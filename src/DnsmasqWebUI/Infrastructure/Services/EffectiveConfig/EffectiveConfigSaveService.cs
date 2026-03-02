using DnsmasqWebUI.Infrastructure.Services.Dnsmasq.Config.Abstractions;
using DnsmasqWebUI.Infrastructure.Services.Dnsmasq.Reload.Abstractions;
using DnsmasqWebUI.Infrastructure.Services.Dnsmasq.Validation.Abstractions;
using DnsmasqWebUI.Infrastructure.Services.EffectiveConfig.Abstractions;
using DnsmasqWebUI.Models.Dnsmasq.EffectiveConfig;
using Microsoft.Extensions.Logging;

namespace DnsmasqWebUI.Infrastructure.Services.EffectiveConfig;

public sealed class EffectiveConfigSaveService : IEffectiveConfigSaveService
{
    private readonly IDnsmasqConfigSetService _configSetService;
    private readonly IDnsmasqConfigService _configService;
    private readonly IConfigValidationService _validationService;
    private readonly IReloadService _reloadService;
    private readonly ILogger<EffectiveConfigSaveService> _logger;

    public EffectiveConfigSaveService(
        IDnsmasqConfigSetService configSetService,
        IDnsmasqConfigService configService,
        IConfigValidationService validationService,
        IReloadService reloadService,
        ILogger<EffectiveConfigSaveService> logger)
    {
        _configSetService = configSetService;
        _configService = configService;
        _validationService = validationService;
        _reloadService = reloadService;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<EffectiveConfigSaveResult> SaveAsync(
        IReadOnlyList<PendingEffectiveConfigChange> changes,
        CancellationToken ct = default)
    {
        if (changes.Count == 0)
            return EffectiveConfigSaveResult.NoChanges();

        var set = await _configSetService.GetConfigSetAsync(ct);
        if (string.IsNullOrWhiteSpace(set.ManagedFilePath))
        {
            _logger.LogWarning("Save skipped: managed config path is not configured");
            return new EffectiveConfigSaveResult(
                false, null, false, false, -1, null, null, false, -1, null, null,
                EffectiveConfigSaveResult.ErrorCodes.MissingManagedPath, "Managed config path is not configured.");
        }

        var managedPath = set.ManagedFilePath!;
        var backupPath = BuildBackupPath(managedPath);

        try
        {
            CreateBackupIfSourceExists(managedPath, backupPath);
            await _configService.ApplyEffectiveConfigChangesAsync(changes, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to write effective config");
            var backupExists = File.Exists(backupPath);
            return new EffectiveConfigSaveResult(
                backupExists,
                backupExists ? backupPath : null,
                false,
                false,
                -1,
                null,
                null,
                false,
                -1,
                null,
                ex.Message,
                EffectiveConfigSaveResult.ErrorCodes.WriteFailed,
                "Failed to write config.");
        }

        var backupCreated = File.Exists(backupPath);
        var validateResult = await _validationService.ValidateAsync(ct);

        if (!validateResult.Success)
        {
            _logger.LogWarning("Config saved but validation failed: exit {ExitCode}, stderr: {Stderr}", validateResult.ExitCode, validateResult.StdErr);
            return new EffectiveConfigSaveResult(
                BackupCreated: backupCreated,
                BackupPath: backupCreated ? backupPath : null,
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
            BackupCreated: backupCreated,
            BackupPath: backupCreated ? backupPath : null,
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
    public async Task<EffectiveConfigRestoreResult> RestoreAsync(string backupPath, CancellationToken ct = default)
    {
        var set = await _configSetService.GetConfigSetAsync(ct);
        if (string.IsNullOrWhiteSpace(set.ManagedFilePath))
        {
            _logger.LogWarning("Restore skipped: managed config path is not configured");
            return new EffectiveConfigRestoreResult(
                false, false, -1, null, "Managed config path is not configured.");
        }

        if (!File.Exists(backupPath))
        {
            _logger.LogWarning("Restore skipped: backup file not found: {Path}", backupPath);
            return new EffectiveConfigRestoreResult(
                false, false, -1, null, "Backup file not found.");
        }

        var managedPath = set.ManagedFilePath!;
        File.Copy(backupPath, managedPath, overwrite: true);
        _logger.LogInformation("Restored managed config from backup: {BackupPath}", backupPath);

        var restartResult = await _reloadService.ReloadAsync(ct);

        if (!restartResult.Success)
            _logger.LogWarning("Restore completed but restart command failed: exit {ExitCode}, stderr: {Stderr}", restartResult.ExitCode, restartResult.StdErr);

        return new EffectiveConfigRestoreResult(
            Restored: true,
            Restarted: restartResult.Success,
            RestartExitCode: restartResult.ExitCode,
            RestartStdErr: restartResult.StdErr,
            UserMessage: restartResult.Success
                ? "Backup restored and dnsmasq restarted."
                : "Backup restored, but the restart command still failed.");
    }

    private static string BuildBackupPath(string managedPath) =>
        $"{managedPath}.bak.{DateTime.UtcNow:yyyyMMdd-HHmmss}";

    private static void CreateBackupIfSourceExists(string managedPath, string backupPath)
    {
        if (!File.Exists(managedPath))
            return;

        var dir = Path.GetDirectoryName(backupPath);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            Directory.CreateDirectory(dir);

        File.Copy(managedPath, backupPath, overwrite: false);
    }
}
