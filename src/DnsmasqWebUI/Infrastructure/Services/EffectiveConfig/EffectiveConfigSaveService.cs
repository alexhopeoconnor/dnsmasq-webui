using DnsmasqWebUI.Infrastructure.Services.Dnsmasq.Config.Abstractions;
using DnsmasqWebUI.Infrastructure.Services.Dnsmasq.Reload.Abstractions;
using DnsmasqWebUI.Infrastructure.Services.EffectiveConfig.Abstractions;
using DnsmasqWebUI.Models.Dnsmasq.EffectiveConfig;
using Microsoft.Extensions.Logging;

namespace DnsmasqWebUI.Infrastructure.Services.EffectiveConfig;

public sealed class EffectiveConfigSaveService : IEffectiveConfigSaveService
{
    private readonly IDnsmasqConfigSetService _configSetService;
    private readonly IDnsmasqConfigService _configService;
    private readonly IReloadService _reloadService;
    private readonly ILogger<EffectiveConfigSaveService> _logger;

    public EffectiveConfigSaveService(
        IDnsmasqConfigSetService configSetService,
        IDnsmasqConfigService configService,
        IReloadService reloadService,
        ILogger<EffectiveConfigSaveService> logger)
    {
        _configSetService = configSetService;
        _configService = configService;
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
                false, null, false, false, -1, null, null,
                "missing_managed_path", "Managed config path is not configured.");
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
                ex.Message,
                "write_failed",
                "Failed to write config.");
        }

        var reload = await _reloadService.ReloadAsync(ct);
        var backupCreated = File.Exists(backupPath);

        if (!reload.Success)
            _logger.LogWarning("Config saved but reload failed: exit {ExitCode}, stderr: {Stderr}", reload.ExitCode, reload.StdErr);

        return new EffectiveConfigSaveResult(
            BackupCreated: backupCreated,
            BackupPath: backupCreated ? backupPath : null,
            Saved: true,
            Reloaded: reload.Success,
            ReloadExitCode: reload.ExitCode,
            ReloadStdOut: reload.StdOut,
            ReloadStdErr: reload.StdErr,
            ErrorCode: reload.Success ? null : "reload_failed",
            UserMessage: reload.Success ? "Saved and reloaded." : "Saved, but reload failed.");
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

        var reload = await _reloadService.ReloadAsync(ct);

        if (!reload.Success)
            _logger.LogWarning("Restore completed but reload failed: exit {ExitCode}, stderr: {Stderr}", reload.ExitCode, reload.StdErr);

        return new EffectiveConfigRestoreResult(
            Restored: true,
            Reloaded: reload.Success,
            ReloadExitCode: reload.ExitCode,
            ReloadStdErr: reload.StdErr,
            UserMessage: reload.Success
                ? "Backup restored and dnsmasq reloaded."
                : "Backup restored, but reload still failed.");
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
