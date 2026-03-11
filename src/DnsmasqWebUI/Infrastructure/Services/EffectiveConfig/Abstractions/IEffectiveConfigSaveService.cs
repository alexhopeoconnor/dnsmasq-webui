using DnsmasqWebUI.Infrastructure.Services.Registration.Abstractions;
using DnsmasqWebUI.Models.Dnsmasq.EffectiveConfig;

namespace DnsmasqWebUI.Infrastructure.Services.EffectiveConfig.Abstractions;

/// <summary>Orchestrates effective-config save: backup managed config and/or managed hosts, write, validate, reload; and restore from backups. Keeps IDnsmasqConfigService focused on read/write mechanics.</summary>
public interface IEffectiveConfigSaveService : IApplicationScopedService
{
    /// <summary>Creates timestamped backups for each target that exists, applies option and managed-hosts changes, then validates and reloads. Returns structured result for UI including backups for restore.</summary>
    Task<EffectiveConfigSaveResult> SaveAsync(
        IReadOnlyList<PendingDnsmasqChange> changes,
        CancellationToken ct = default);

    /// <summary>Overwrites each target file with its backup, then runs reload. Returns structured result for UI.</summary>
    Task<EffectiveConfigRestoreResult> RestoreAsync(IReadOnlyList<DnsmasqManagedBackup> backups, CancellationToken ct = default);
}
