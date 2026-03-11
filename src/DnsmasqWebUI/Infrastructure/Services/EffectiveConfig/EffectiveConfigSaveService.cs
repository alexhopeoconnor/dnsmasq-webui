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
    private readonly IDnsmasqConfigSetService _configSetService;
    private readonly IDnsmasqConfigService _configService;
    private readonly IConfigSetCache _configSetCache;
    private readonly IHostsFileService _hostsFileService;
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
        ILogger<EffectiveConfigSaveService> logger)
    {
        _configSetService = configSetService;
        _configService = configService;
        _configSetCache = configSetCache;
        _hostsFileService = hostsFileService;
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

        var set = await _configSetService.GetConfigSetAsync(ct);
        var plan = BuildWritePlan(changes, set.ManagedFilePath ?? "");

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

        var backups = CreateBackups(plan.Targets);

        try
        {
            if (plan.OptionChanges.Count > 0)
                await _configService.ApplyEffectiveConfigChangesAsync(optionWriteChanges, ct);
            if (plan.ManagedHostsChange != null)
            {
                await _hostsFileService.WriteAsync(plan.ManagedHostsChange.NewEntries, ct);
                _logger.LogInformation("Managed hosts file written, {Count} entries", plan.ManagedHostsChange.NewEntries.Count);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to write effective config or managed hosts");
            // Roll back any partial write so we never leave one target overwritten and the other unchanged.
            foreach (var backup in backups)
            {
                if (File.Exists(backup.BackupPath))
                {
                    var dir = Path.GetDirectoryName(backup.TargetPath);
                    if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                        Directory.CreateDirectory(dir);
                    File.Copy(backup.BackupPath, backup.TargetPath, overwrite: true);
                    _logger.LogInformation("Rolled back partial write: {BackupPath} -> {TargetPath}", backup.BackupPath, backup.TargetPath);
                }
            }
            if (backups.Count > 0)
                _configSetCache.Invalidate();
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
                UserMessage: ex.Message);
        }

        var validateResult = await _validationService.ValidateAsync(ct);
        if (!validateResult.Success)
        {
            _logger.LogWarning("Changes saved but validation failed: exit {ExitCode}, stderr: {Stderr}", validateResult.ExitCode, validateResult.StdErr);
            return new EffectiveConfigSaveResult(
                backups,
                Saved: true,
                Validated: false,
                ValidationExitCode: validateResult.ExitCode,
                ValidationStdOut: validateResult.StdOut,
                ValidationStdErr: validateResult.StdErr,
                Restarted: false,
                RestartExitCode: -1,
                RestartStdOut: null,
                RestartStdErr: null,
                ErrorCode: EffectiveConfigSaveResult.ErrorCodes.ValidateFailed,
                UserMessage: "Saved, but validation failed. Restart was not attempted.");
        }

        var restartResult = await _reloadService.ReloadAsync(ct);
        if (!restartResult.Success)
            _logger.LogWarning("Config saved but restart command failed: exit {ExitCode}, stderr: {Stderr}", restartResult.ExitCode, restartResult.StdErr);

        return new EffectiveConfigSaveResult(
            backups,
            Saved: true,
            Validated: true,
            ValidationExitCode: 0,
            ValidationStdOut: validateResult.StdOut,
            ValidationStdErr: validateResult.StdErr,
            Restarted: restartResult.Success,
            RestartExitCode: restartResult.ExitCode,
            RestartStdOut: restartResult.StdOut,
            RestartStdErr: restartResult.StdErr,
            ErrorCode: restartResult.Success ? null : EffectiveConfigSaveResult.ErrorCodes.RestartFailed,
            UserMessage: restartResult.Success
                ? "Saved, validated, and dnsmasq restarted."
                : "Saved and validated, but the restart command failed.");
    }

    /// <inheritdoc />
    public async Task<EffectiveConfigRestoreResult> RestoreAsync(
        IReadOnlyList<DnsmasqManagedBackup> backups,
        CancellationToken ct = default)
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

        foreach (var backup in backups)
        {
            var dir = Path.GetDirectoryName(backup.TargetPath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);
            File.Copy(backup.BackupPath, backup.TargetPath, overwrite: true);
            _logger.LogInformation("Restored {Kind} from backup: {BackupPath} -> {TargetPath}", backup.Kind, backup.BackupPath, backup.TargetPath);
        }

        _configSetCache.Invalidate();

        var restartResult = await _reloadService.ReloadAsync(ct);
        if (!restartResult.Success)
            _logger.LogWarning("Restore completed but restart command failed: exit {ExitCode}, stderr: {Stderr}", restartResult.ExitCode, restartResult.StdErr);

        return new EffectiveConfigRestoreResult(
            Restored: true,
            Restarted: restartResult.Success,
            RestartExitCode: restartResult.ExitCode,
            RestartStdErr: restartResult.StdErr,
            UserMessage: restartResult.Success
                ? "Backups restored and dnsmasq restarted."
                : "Backups restored, but the restart command still failed.");
    }

    private static EffectiveConfigSaveResult Fail(
        IReadOnlyList<DnsmasqManagedBackup> backups,
        string errorCode,
        string userMessage) =>
        new(backups, false, false, -1, null, null, false, -1, null, null, errorCode, userMessage);

    private static DnsmasqSaveWritePlan BuildWritePlan(
        IReadOnlyList<PendingDnsmasqChange> changes,
        string managedConfigPath)
    {
        var optionChanges = changes.OfType<PendingOptionChange>().ToList();
        var hostsChange = changes.OfType<PendingManagedHostsChange>().FirstOrDefault();
        var targets = new List<DnsmasqManagedWriteTarget>();
        if (optionChanges.Count > 0 && !string.IsNullOrWhiteSpace(managedConfigPath))
            targets.Add(new DnsmasqManagedWriteTarget(DnsmasqManagedTargetKind.ManagedConfig, managedConfigPath));
        if (hostsChange != null)
            targets.Add(new DnsmasqManagedWriteTarget(DnsmasqManagedTargetKind.ManagedHosts, hostsChange.ManagedHostsFilePath));
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
            var backupPath = $"{target.TargetPath}.bak.{timestamp}";
            var dir = Path.GetDirectoryName(backupPath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);
            File.Copy(target.TargetPath, backupPath, overwrite: false);
            list.Add(new DnsmasqManagedBackup(target.Kind, target.TargetPath, backupPath));
        }
        return list;
    }
}
