using DnsmasqWebUI.Models.Contracts;
using DnsmasqWebUI.Infrastructure.Services.Registration.Abstractions;
using DnsmasqWebUI.Models.Hosts;

namespace DnsmasqWebUI.Infrastructure.Services.Dnsmasq.Hosts.Abstractions;

/// <summary>
/// Singleton cache for managed hosts and all resolved system/addn-hosts/hostsdir sources. Invalidates on
/// source-file and source-directory watchers, source-shape changes, staleness, or <see cref="Invalidate"/>.
/// Call <see cref="NotifyWeWroteManagedHosts"/> after the app writes the managed hosts file so the cache
/// updates in place and ignores its own watcher event.
/// </summary>
public interface IHostsCache : IApplicationSingleton
{
    Task<HostsSnapshot> GetSnapshotAsync(CancellationToken ct = default);

    /// <summary>Returns source health and load metadata, including empty/missing configured sources.</summary>
    Task<IReadOnlyList<HostSource>> GetSourcesAsync(CancellationToken ct = default);

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

    /// <summary>
    /// Builds unified rows from the authoritative effective config. Prefer this overload for new callers;
    /// the legacy overload remains only for browser-client compatibility.
    /// </summary>
    Task<IReadOnlyList<HostsPageRow>> GetUnifiedRowsAsync(CancellationToken ct = default);
}
