using DnsmasqWebUI.Models.Dhcp;

namespace DnsmasqWebUI.Services.Abstractions;

/// <summary>
/// Singleton cache for the leases file; invalidates when the file changes (e.g. via file watcher).
/// </summary>
public interface ILeasesCache : IApplicationSingleton
{
    Task<(bool Available, IReadOnlyList<LeaseEntry>? Entries)> GetOrRefreshAsync(CancellationToken ct = default);
}
