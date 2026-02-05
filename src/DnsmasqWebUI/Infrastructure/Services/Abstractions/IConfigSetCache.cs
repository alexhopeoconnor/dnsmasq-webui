using DnsmasqWebUI.Models.Contracts;

namespace DnsmasqWebUI.Infrastructure.Services.Abstractions;

/// <summary>
/// Singleton cache for the config set (main + conf-dir + managed file). Invalidates on file watchers, staleness, or manual Invalidate.
/// After the app writes the managed config file, call <see cref="NotifyWeWroteManagedConfig"/> so the cache updates in place instead of treating the write as an external change.
/// </summary>
public interface IConfigSetCache : IApplicationSingleton
{
    /// <summary>Returns the current snapshot (config set, effective config, sources, managed content), refreshing from disk if dirty or stale.</summary>
    Task<ConfigSetSnapshot> GetSnapshotAsync(CancellationToken ct = default);

    /// <summary>Forces the next <see cref="GetSnapshotAsync"/> to re-read all config files.</summary>
    void Invalidate();

    /// <summary>Call after the app writes the managed config file. Updates the cached managed content in place and ignores the next watcher event for that file for a short window.</summary>
    void NotifyWeWroteManagedConfig(ManagedConfigContent newContent);
}
