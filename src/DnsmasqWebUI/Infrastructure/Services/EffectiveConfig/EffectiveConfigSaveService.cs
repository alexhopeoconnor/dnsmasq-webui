using System.Linq;
using DnsmasqWebUI.Infrastructure.Services.EffectiveConfig.Metadata;
using DnsmasqWebUI.Infrastructure.Services.Dnsmasq.Config.Abstractions;
using DnsmasqWebUI.Infrastructure.Services.Dnsmasq.Hosts.Abstractions;
using DnsmasqWebUI.Infrastructure.Services.Dnsmasq.Reload.Abstractions;
using DnsmasqWebUI.Infrastructure.Services.Dnsmasq.Validation.Abstractions;
using DnsmasqWebUI.Infrastructure.Services.Dnsmasq.Version.Abstractions;
using DnsmasqWebUI.Infrastructure.Services.EffectiveConfig.Abstractions;
using DnsmasqWebUI.Models.Dnsmasq.EffectiveConfig;
using Microsoft.Extensions.Logging;

namespace DnsmasqWebUI.Infrastructure.Services.EffectiveConfig;

public sealed class EffectiveConfigSaveService : IEffectiveConfigSaveService
{
    // EffectiveConfigSaveService is scoped to a Blazor circuit. The lock must therefore be
    // process-wide so two browser circuits cannot interleave writes, validation, and reload.
    private static readonly SemaphoreSlim SaveLock = new(1, 1);

    private readonly IDnsmasqConfigSetService _configSetService;
    private readonly IDnsmasqConfigService _configService;
    private readonly IConfigSetCache _configSetCache;
    private readonly IHostsFileService _hostsFileService;
    private readonly IHostsCache? _hostsCache;
    private readonly IConfigValidationService _validationService;
    private readonly IEffectiveConfigSemanticValidationService _semanticValidationService;
    private readonly IReloadService _reloadService;
    private readonly IDnsmasqVersionService _versionService;
    private readonly ILogger<EffectiveConfigSaveService> _logger;

    public EffectiveConfigSaveService(
        IDnsmasqConfigSetService configSetService,
        IDnsmasqConfigService configService,
        IConfigSetCache configSetCache,
        IHostsFileService hostsFileService,
        IConfigValidationService validationService,
        IEffectiveConfigSemanticValidationService semanticValidationService,
        IReloadService reloadService,
        IDnsmasqVersionService versionService,
        ILogger<EffectiveConfigSaveService> logger,
        IHostsCache? hostsCache = null)
    {
        _configSetService = configSetService;
        _configService = configService;
        _configSetCache = configSetCache;
        _hostsFileService = hostsFileService;
        _hostsCache = hostsCache;
        _validationService = validationService;
        _semanticValidationService = semanticValidationService;
        _reloadService = reloadService;
        _versionService = versionService;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<EffectiveConfigSaveResult> SaveAsync(
        IReadOnlyList<PendingDnsmasqChange> changes,
        CancellationToken ct = default)
    {
        if (changes.Count == 0)
            return EffectiveConfigSaveResult.NoChanges();

        await SaveLock.WaitAsync(ct);
        try
        {
            return await SaveCoreAsync(changes, ct);
        }
        finally
        {
            SaveLock.Release();
        }
    }

    private async Task<EffectiveConfigSaveResult> SaveCoreAsync(
        IReadOnlyList<PendingDnsmasqChange> changes,
        CancellationToken ct)
    {

        var version = await _versionService.GetVersionInfoAsync(ct);
        if (!version.ProbeSucceeded)
        {
            return Fail(Array.Empty<DnsmasqManagedBackup>(), EffectiveConfigSaveResult.ErrorCodes.VersionProbeFailed,
                string.IsNullOrWhiteSpace(version.Error)
                    ? "Cannot save: dnsmasq version could not be determined."
                    : $"Cannot save: dnsmasq version could not be determined. {version.Error}");
        }
        if (!version.IsSupported)
        {
            return Fail(Array.Empty<DnsmasqManagedBackup>(), EffectiveConfigSaveResult.ErrorCodes.UnsupportedVersion,
                $"Installed dnsmasq {version.InstalledVersion} is below required {version.MinimumVersion}.");
        }

        // Save must compare against freshly read source files rather than the status snapshot a
        // browser may have loaded minutes ago. File watchers are best effort, so force the cache
        // to rebuild before deriving the managed paths and conflict baseline.
        _configSetCache.Invalidate();
        var set = await _configSetService.GetConfigSetAsync(ct);
        var plan = BuildWritePlan(changes, set.ManagedFilePath ?? "", set.ManagedHostsFilePath);

        if (plan.OptionChanges.Count > 0 && string.IsNullOrWhiteSpace(set.ManagedFilePath))
        {
            _logger.LogWarning("Save skipped: managed config path is not configured");
            return Fail(Array.Empty<DnsmasqManagedBackup>(), EffectiveConfigSaveResult.ErrorCodes.MissingManagedPath,
                "Managed config path is not configured.");
        }

        var optionWriteChanges = plan.OptionChanges
            .Select(c => new PendingEffectiveConfigChange(c.SectionId, c.OptionName, c.OldValue, c.NewValue, c.CurrentSourceFilePath))
            .ToList();

        var unsupported = optionWriteChanges
            .Where(c => !EffectiveConfigFeatureRequirements.IsSupportedByCapabilities(c.OptionName, version.Capabilities))
            .Select(c => c.OptionName)
            .Distinct(StringComparer.Ordinal)
            .ToList();
        if (unsupported.Count > 0)
        {
            var firstReason = EffectiveConfigFeatureRequirements.GetRequiredFeature(unsupported[0]) is { } f
                ? EffectiveConfigFeatureRequirements.GetUnsupportedReason(f)
                : "This option isn't available with your current dnsmasq build.";
            var optionList = string.Join(", ", unsupported);
            _logger.LogWarning("Save rejected: unsupported options in pending changes: {Options}", optionList);
            var userMessage = unsupported.Count == 1
                ? firstReason
                : $"Some of the options you changed aren't supported by your dnsmasq build. Options not saved: {optionList}.";
            return Fail(Array.Empty<DnsmasqManagedBackup>(), EffectiveConfigSaveResult.ErrorCodes.UnsupportedCapabilities, userMessage);
        }

        var semanticIssues = _semanticValidationService.Validate(optionWriteChanges);
        if (semanticIssues.Any(i => i.Severity == FieldIssueSeverity.Error))
        {
            var errorIssues = semanticIssues.Where(i => i.Severity == FieldIssueSeverity.Error).ToList();
            var messages = errorIssues.Select(i => i.Message).Distinct().Take(5).ToList();
            var userMessage = messages.Count == 1
                ? messages[0]
                : "Some values are invalid. Fix validation errors before saving. " + string.Join("; ", messages);
            var failedFields = errorIssues.Select(i => i.FieldKey).Distinct().ToList();
            _logger.LogWarning(
                "Save blocked: semantic validation failed for {ErrorCount} error(s) on field(s) {FailedFields}. First message: {FirstMessage}",
                errorIssues.Count, failedFields, messages.FirstOrDefault());
            return Fail(Array.Empty<DnsmasqManagedBackup>(), EffectiveConfigSaveResult.ErrorCodes.SemanticValidationFailed, userMessage);
        }

        var optionFreshness = EffectiveConfigSaveConflictDetector.CheckOptions(set, plan.OptionChanges);
        if (!optionFreshness.IsCurrent)
            return FailForConflict(optionFreshness);

        if (plan.ManagedHostsChange != null)
        {
            var hostsFreshness = EffectiveConfigSaveConflictDetector.CheckManagedHosts(plan.ManagedHostsChange);
            if (!hostsFreshness.IsCurrent)
                return FailForConflict(hostsFreshness);
        }

        // dnsmasq does not re-read .conf includes on SIGHUP. A managed option write therefore
        // must have a real restart available before we create backups or modify disk. Hosts-only
        // edits can use ReloadCommand; when that is absent, a configured restart is also safe.
        var applyWithRestart = plan.OptionChanges.Count > 0 || !_reloadService.ReloadCommandConfigured;
        if (plan.OptionChanges.Count > 0 && !_reloadService.RestartCommandConfigured)
        {
            const string message =
                "Cannot save dnsmasq configuration changes: Dnsmasq:RestartCommand must be configured " +
                "because dnsmasq must restart to re-read managed .conf files. ReloadCommand/SIGHUP cannot apply these changes.";
            _logger.LogWarning(message);
            return Fail(Array.Empty<DnsmasqManagedBackup>(), EffectiveConfigSaveResult.ErrorCodes.RestartCommandRequired, message);
        }

        if (plan.OptionChanges.Count == 0 && plan.ManagedHostsChange != null &&
            !_reloadService.ReloadCommandConfigured && !_reloadService.RestartCommandConfigured)
        {
            const string message =
                "Cannot save managed Hosts changes: configure Dnsmasq:ReloadCommand, or Dnsmasq:RestartCommand, " +
                "so dnsmasq can apply the updated hosts data.";
            _logger.LogWarning(message);
            return Fail(Array.Empty<DnsmasqManagedBackup>(), EffectiveConfigSaveResult.ErrorCodes.ApplyCommandRequired, message);
        }

        // Keep enough information to restore a target which did not exist before this save.
        // Backups alone cannot represent that state because there is no file to copy.
        var originalTargets = plan.Targets
            .Select(target => new OriginalTargetState(target, File.Exists(target.TargetPath)))
            .ToList();
        var backups = CreateBackups(originalTargets.Where(x => x.Existed).Select(x => x.Target).ToList());

        try
        {
            if (plan.OptionChanges.Count > 0)
                await _configService.ApplyEffectiveConfigChangesAsync(optionWriteChanges, ct);
            if (plan.ManagedHostsChange != null)
            {
                await _hostsFileService.WriteAsync(plan.ManagedHostsChange.NewEntries, ct);
                _logger.LogInformation("Managed hosts file written, {Count} entries", plan.ManagedHostsChange.NewEntries.Count);
            }
            // Config changes can enable/disable or retarget read-only host sources even when
            // the managed hosts entries themselves did not change.
            _hostsCache?.Invalidate();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to write effective config or managed hosts");
            var rollback = TryRestoreOriginalTargets(originalTargets, backups);
            _configSetCache.Invalidate();
            _hostsCache?.Invalidate();
            return new EffectiveConfigSaveResult(
                backups,
                Saved: false,
                Validated: false,
                ValidationExitCode: -1,
                ValidationStdOut: null,
                ValidationStdErr: null,
                Restarted: false,
                RestartExitCode: -1,
                RestartStdOut: null,
                RestartStdErr: null,
                ErrorCode: EffectiveConfigSaveResult.ErrorCodes.WriteFailed,
                UserMessage: rollback.Success
                    ? $"Save failed and partial changes were restored automatically. {ex.Message}"
                    : $"Save failed and automatic rollback also failed: {rollback.Error} Original error: {ex.Message}")
            {
                RolledBack = rollback.Success
            };
        }

        var validateResult = await _validationService.ValidateAsync(ct);
        if (!validateResult.Success)
        {
            _logger.LogWarning("Changes saved but validation failed: exit {ExitCode}, stderr: {Stderr}", validateResult.ExitCode, validateResult.StdErr);
            var rollback = TryRestoreOriginalTargets(originalTargets, backups);
            _configSetCache.Invalidate();
            _hostsCache?.Invalidate();
            return new EffectiveConfigSaveResult(
                backups,
                Saved: !rollback.Success,
                Validated: false,
                ValidationExitCode: validateResult.ExitCode,
                ValidationStdOut: validateResult.StdOut,
                ValidationStdErr: validateResult.StdErr,
                Restarted: false,
                RestartExitCode: -1,
                RestartStdOut: null,
                RestartStdErr: null,
                ErrorCode: EffectiveConfigSaveResult.ErrorCodes.ValidateFailed,
                UserMessage: rollback.Success
                    ? "Validation failed. Changes were restored automatically; your draft is still available to correct."
                    : $"Saved, but validation failed and automatic rollback also failed: {rollback.Error}")
            {
                RolledBack = rollback.Success
            };
        }

        var applyResult = await ApplyDnsmasqAsync(applyWithRestart, ct);
        if (!applyResult.Success)
        {
            var operation = applyWithRestart ? "restart" : "reload";
            _logger.LogWarning(
                "Changes were saved but dnsmasq {Operation} command failed: exit {ExitCode}, stderr: {Stderr}",
                operation,
                applyResult.ExitCode,
                applyResult.StdErr);
            var rollback = TryRestoreOriginalTargets(originalTargets, backups);
            _configSetCache.Invalidate();
            _hostsCache?.Invalidate();

            ReloadResult? rollbackApply = null;
            if (rollback.Success)
                rollbackApply = await ApplyDnsmasqAsync(applyWithRestart, ct);

            return new EffectiveConfigSaveResult(
                backups,
                Saved: !rollback.Success,
                Validated: true,
                ValidationExitCode: 0,
                ValidationStdOut: validateResult.StdOut,
                ValidationStdErr: validateResult.StdErr,
                Restarted: false,
                RestartExitCode: applyResult.ExitCode,
                RestartStdOut: applyResult.StdOut,
                RestartStdErr: AppendRollbackStatus(applyResult.StdErr, rollback, rollbackApply, applyWithRestart),
                ErrorCode: applyWithRestart
                    ? EffectiveConfigSaveResult.ErrorCodes.RestartFailed
                    : EffectiveConfigSaveResult.ErrorCodes.ReloadFailed,
                UserMessage: rollback.Success
                    ? $"Dnsmasq {operation} failed. Changes were restored automatically; your draft is still available to correct."
                    : $"Saved and validated, but dnsmasq {operation} failed and automatic rollback also failed: {rollback.Error}")
            {
                RolledBack = rollback.Success
            };
        }

        return new EffectiveConfigSaveResult(
            backups,
            Saved: true,
            Validated: true,
            ValidationExitCode: 0,
            ValidationStdOut: validateResult.StdOut,
            ValidationStdErr: validateResult.StdErr,
            Restarted: applyWithRestart,
            RestartExitCode: applyResult.ExitCode,
            RestartStdOut: applyResult.StdOut,
            RestartStdErr: applyResult.StdErr,
            ErrorCode: null,
            UserMessage: applyWithRestart
                ? "Saved, validated, and dnsmasq restarted."
                : "Saved, validated, and dnsmasq reloaded.")
        {
            Reloaded = !applyWithRestart
        };
    }

    /// <inheritdoc />
    public async Task<EffectiveConfigRestoreResult> RestoreAsync(
        IReadOnlyList<DnsmasqManagedBackup> backups,
        CancellationToken ct = default)
    {
        await SaveLock.WaitAsync(ct);
        try
        {
            return await RestoreCoreAsync(backups, ct);
        }
        finally
        {
            SaveLock.Release();
        }
    }

    private async Task<EffectiveConfigRestoreResult> RestoreCoreAsync(
        IReadOnlyList<DnsmasqManagedBackup> backups,
        CancellationToken ct)
    {
        if (backups == null || backups.Count == 0)
        {
            _logger.LogWarning("Restore skipped: no backups provided");
            return new EffectiveConfigRestoreResult(
                false, false, -1, null, "No backups available.");
        }

        foreach (var backup in backups)
        {
            if (!File.Exists(backup.BackupPath))
            {
                _logger.LogWarning("Restore skipped: backup file not found: {Path}", backup.BackupPath);
                return new EffectiveConfigRestoreResult(
                    false, false, -1, null, $"Backup file not found: {backup.BackupPath}");
            }
        }

        // A managed .conf backup changes dnsmasq configuration, so it has the same full-restart
        // requirement as a normal option save. A hosts-only backup may use the lightweight reload.
        var restoreIncludesConfig = backups.Any(b => b.Kind == DnsmasqManagedTargetKind.ManagedConfig);
        if (restoreIncludesConfig && !_reloadService.RestartCommandConfigured)
        {
            const string message =
                "Cannot restore managed dnsmasq configuration: Dnsmasq:RestartCommand must be configured " +
                "because restored .conf files require a full restart.";
            _logger.LogWarning(message);
            return new EffectiveConfigRestoreResult(false, false, -1, message, message);
        }

        if (!restoreIncludesConfig && !_reloadService.ReloadCommandConfigured && !_reloadService.RestartCommandConfigured)
        {
            const string message =
                "Cannot restore managed Hosts data: configure Dnsmasq:ReloadCommand or Dnsmasq:RestartCommand " +
                "so dnsmasq can apply the restored hosts data.";
            _logger.LogWarning(message);
            return new EffectiveConfigRestoreResult(false, false, -1, message, message);
        }

        var restoreWithRestart = restoreIncludesConfig || !_reloadService.ReloadCommandConfigured;

        foreach (var backup in backups)
        {
            var dir = Path.GetDirectoryName(backup.TargetPath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);
            File.Copy(backup.BackupPath, backup.TargetPath, overwrite: true);
            _logger.LogInformation("Restored {Kind} from backup: {BackupPath} -> {TargetPath}", backup.Kind, backup.BackupPath, backup.TargetPath);
        }

        _configSetCache.Invalidate();
        _hostsCache?.Invalidate();

        var applyResult = await ApplyDnsmasqAsync(restoreWithRestart, ct);
        var restoreOperation = restoreWithRestart ? "restart" : "reload";
        if (!applyResult.Success)
            _logger.LogWarning(
                "Restore completed but dnsmasq {Operation} command failed: exit {ExitCode}, stderr: {Stderr}",
                restoreOperation,
                applyResult.ExitCode,
                applyResult.StdErr);

        return new EffectiveConfigRestoreResult(
            Restored: true,
            Restarted: restoreWithRestart && applyResult.Success,
            RestartExitCode: applyResult.ExitCode,
            RestartStdErr: applyResult.StdErr,
            UserMessage: applyResult.Success
                ? restoreWithRestart
                    ? "Backups restored and dnsmasq restarted."
                    : "Backups restored and dnsmasq reloaded."
                : $"Backups restored, but the dnsmasq {restoreOperation} command failed.")
        {
            Reloaded = !restoreWithRestart && applyResult.Success
        };
    }

    private static EffectiveConfigSaveResult Fail(
        IReadOnlyList<DnsmasqManagedBackup> backups,
        string errorCode,
        string userMessage) =>
        new(backups, false, false, -1, null, null, false, -1, null, null, errorCode, userMessage);

    private static EffectiveConfigSaveResult FailForConflict(EffectiveConfigSaveConflictCheck check)
    {
        if (!string.IsNullOrWhiteSpace(check.VerificationFailure))
        {
            return Fail(
                Array.Empty<DnsmasqManagedBackup>(),
                EffectiveConfigSaveResult.ErrorCodes.StaleDraftConflict,
                $"{check.VerificationFailure} Refresh the page and review the current settings before saving.");
        }

        var fields = check.ConflictingFields;
        var subject = fields.Count switch
        {
            0 => "The configuration",
            1 when string.Equals(fields[0], "managed hosts", StringComparison.Ordinal) => "The managed Hosts file",
            1 => $"The current value of {fields[0]}",
            _ => $"The current values of {string.Join(", ", fields.Take(5))}",
        };
        return Fail(
            Array.Empty<DnsmasqManagedBackup>(),
            EffectiveConfigSaveResult.ErrorCodes.StaleDraftConflict,
            $"{subject} changed outside this draft. Refresh the page, review the current settings, then reapply your changes.");
    }

    private static DnsmasqSaveWritePlan BuildWritePlan(
        IReadOnlyList<PendingDnsmasqChange> changes,
        string managedConfigPath,
        string? managedHostsPath)
    {
        var optionChanges = changes.OfType<PendingOptionChange>().ToList();
        var hostsChange = changes.OfType<PendingManagedHostsChange>().FirstOrDefault();
        var targets = new List<DnsmasqManagedWriteTarget>();
        if (optionChanges.Count > 0 && !string.IsNullOrWhiteSpace(managedConfigPath))
        {
            targets.Add(new DnsmasqManagedWriteTarget(DnsmasqManagedTargetKind.ManagedConfig, managedConfigPath));
            // The managed config writer keeps an addn-hosts invariant and may create this
            // file. Track it in the transaction even when its entries were not edited.
            if (!string.IsNullOrWhiteSpace(managedHostsPath))
                targets.Add(new DnsmasqManagedWriteTarget(DnsmasqManagedTargetKind.ManagedHosts, managedHostsPath));
        }
        if (hostsChange != null)
        {
            if (!targets.Any(t => string.Equals(t.TargetPath, hostsChange.ManagedHostsFilePath, StringComparison.Ordinal)))
                targets.Add(new DnsmasqManagedWriteTarget(DnsmasqManagedTargetKind.ManagedHosts, hostsChange.ManagedHostsFilePath));
        }
        return new DnsmasqSaveWritePlan(optionChanges, hostsChange, targets);
    }

    private static IReadOnlyList<DnsmasqManagedBackup> CreateBackups(IReadOnlyList<DnsmasqManagedWriteTarget> targets)
    {
        var list = new List<DnsmasqManagedBackup>();
        var timestamp = DateTime.UtcNow.ToString("yyyyMMdd-HHmmss");
        foreach (var target in targets)
        {
            if (!File.Exists(target.TargetPath))
                continue;
            var backupPath = $"{target.TargetPath}.bak.{timestamp}.{Guid.NewGuid():N}";
            var dir = Path.GetDirectoryName(backupPath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);
            File.Copy(target.TargetPath, backupPath, overwrite: false);
            list.Add(new DnsmasqManagedBackup(target.Kind, target.TargetPath, backupPath));
        }
        return list;
    }

    private Task<ReloadResult> ApplyDnsmasqAsync(bool restart, CancellationToken ct) =>
        restart ? _reloadService.RestartAsync(ct) : _reloadService.ReloadAsync(ct);

    /// <summary>
    /// Restores all targets to exactly the state observed before a save. Targets created by
    /// the failed save are removed; targets that existed are copied back from their backup.
    /// </summary>
    private (bool Success, string? Error) TryRestoreOriginalTargets(
        IReadOnlyList<OriginalTargetState> originalTargets,
        IReadOnlyList<DnsmasqManagedBackup> backups)
    {
        try
        {
            var backupsByTarget = backups.ToDictionary(x => x.TargetPath, StringComparer.Ordinal);
            foreach (var original in originalTargets)
            {
                if (!original.Existed)
                {
                    if (File.Exists(original.Target.TargetPath))
                    {
                        File.Delete(original.Target.TargetPath);
                        _logger.LogInformation("Rolled back newly-created {Kind}: {TargetPath}", original.Target.Kind, original.Target.TargetPath);
                    }
                    continue;
                }

                if (!backupsByTarget.TryGetValue(original.Target.TargetPath, out var backup) || !File.Exists(backup.BackupPath))
                    return (false, $"Backup is unavailable for {original.Target.TargetPath}.");

                var directory = Path.GetDirectoryName(original.Target.TargetPath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                    Directory.CreateDirectory(directory);
                File.Copy(backup.BackupPath, original.Target.TargetPath, overwrite: true);
                _logger.LogInformation("Rolled back {Kind}: {BackupPath} -> {TargetPath}", original.Target.Kind, backup.BackupPath, original.Target.TargetPath);
            }
            return (true, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Automatic rollback failed");
            return (false, ex.Message);
        }
    }

    private static string? AppendRollbackStatus(
        string? originalError,
        (bool Success, string? Error) rollback,
        ReloadResult? rollbackApply,
        bool rollbackWithRestart)
    {
        var messages = new List<string>();
        if (!string.IsNullOrWhiteSpace(originalError))
            messages.Add(originalError.Trim());
        if (!rollback.Success)
            messages.Add($"Automatic rollback failed: {rollback.Error}");
        else if (rollbackApply is { Success: false })
            messages.Add($"Files were restored, but dnsmasq could not {GetApplyOperation(rollbackWithRestart)} the known-good state: {rollbackApply.StdErr}");
        else if (rollback.Success)
            messages.Add("Files were restored automatically.");
        return messages.Count == 0 ? null : string.Join(Environment.NewLine, messages);
    }

    private static string GetApplyOperation(bool restart) => restart ? "restart" : "reload";

    private sealed record OriginalTargetState(DnsmasqManagedWriteTarget Target, bool Existed);
}
