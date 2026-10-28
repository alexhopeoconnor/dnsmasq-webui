using DnsmasqWebUI.Infrastructure.Services.Registration.Abstractions;
using DnsmasqWebUI.Models.Contracts;

namespace DnsmasqWebUI.Infrastructure.Services.Dnsmasq.Config.Abstractions;

/// <summary>
/// Singleton cache for the config set (main + conf-dir + managed file). Invalidates on source-file watchers,
/// source metadata and include-topology changes, staleness, or manual <see cref="Invalidate"/>.
/// After the app writes the managed config file, call <see cref="NotifyWeWroteManagedConfig"/> so the next
/// snapshot refresh is intentional rather than dependent on watcher delivery.
/// </summary>
public interface IConfigSetCache : IApplicationSingleton
{
    /// <summary>Returns the current snapshot (config set, effective config, sources, managed content), refreshing from disk if dirty or stale.</summary>
    Task<ConfigSetSnapshot> GetSnapshotAsync(CancellationToken ct = default);

    /// <summary>Forces the next <see cref="GetSnapshotAsync"/> to re-read all config files.</summary>
    void Invalidate();

    /// <summary>Call after the app writes the managed config file. Marks the snapshot stale and ignores the next watcher event for that file for a short window.</summary>
    void NotifyWeWroteManagedConfig(ManagedConfigContent newContent);
}
