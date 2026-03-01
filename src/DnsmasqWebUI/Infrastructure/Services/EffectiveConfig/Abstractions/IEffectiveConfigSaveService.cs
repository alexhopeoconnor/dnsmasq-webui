using DnsmasqWebUI.Infrastructure.Services.Registration.Abstractions;
using DnsmasqWebUI.Models.Dnsmasq.EffectiveConfig;

namespace DnsmasqWebUI.Infrastructure.Services.EffectiveConfig.Abstractions;

/// <summary>Orchestrates effective-config save: backup, write, reload; and restore from backup. Keeps IDnsmasqConfigService focused on read/write mechanics.</summary>
public interface IEffectiveConfigSaveService : IApplicationScopedService
{
    /// <summary>Creates a timestamped backup (if managed file exists), applies changes, then runs reload. Returns structured result for UI.</summary>
    Task<EffectiveConfigSaveResult> SaveAsync(
        IReadOnlyList<PendingEffectiveConfigChange> changes,
        CancellationToken ct = default);

    /// <summary>Overwrites the managed config with the backup file, then runs reload. Returns structured result for UI.</summary>
    Task<EffectiveConfigRestoreResult> RestoreAsync(string backupPath, CancellationToken ct = default);
}
