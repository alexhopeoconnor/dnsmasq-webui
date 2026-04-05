using DnsmasqWebUI.Models.Contracts;
using DnsmasqWebUI.Infrastructure.Services.Registration.Abstractions;
using DnsmasqWebUI.Models.Hosts;

namespace DnsmasqWebUI.Infrastructure.Services.Dnsmasq.Hosts.Abstractions;

/// <summary>
/// Singleton cache for hosts: managed hosts file + read-only (system + addn-hosts + hostsdir). Invalidates on watchers, staleness, or Invalidate.
/// Call <see cref="NotifyWeWroteManagedHosts"/> after the app writes the managed hosts file so the cache updates in place and ignores the next watcher event.
/// </summary>
public interface IHostsCache : IApplicationSingleton
{
    Task<HostsSnapshot> GetSnapshotAsync(CancellationToken ct = default);
    void Invalidate();
    void NotifyWeWroteManagedHosts(IReadOnlyList<HostEntry> entries);

    /// <summary>
    /// Builds unified page rows from the snapshot, preserving source awareness.
    /// Effective-name expansion uses the config snapshot <c>domain=</c> list (including scoped rules)
    /// per row address, matching dnsmasq expand-hosts behavior.
    /// </summary>
    Task<IReadOnlyList<HostsPageRow>> GetUnifiedRowsAsync(
        bool expandHosts,
        bool noHosts,
        string? managedHostsPath,
        CancellationToken ct = default);
}
