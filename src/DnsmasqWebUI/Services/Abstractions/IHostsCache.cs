using DnsmasqWebUI.Models.Hosts;

namespace DnsmasqWebUI.Services.Abstractions;

/// <summary>
/// Singleton cache for hosts: managed hosts file + read-only (system + addn-hosts). Invalidates on watchers, staleness, or Invalidate.
/// Call <see cref="NotifyWeWroteManagedHosts"/> after the app writes the managed hosts file so the cache updates in place and ignores the next watcher event.
/// </summary>
public interface IHostsCache : IApplicationSingleton
{
    Task<HostsSnapshot> GetSnapshotAsync(CancellationToken ct = default);
    void Invalidate();
    void NotifyWeWroteManagedHosts(IReadOnlyList<HostEntry> entries);
}
