using DnsmasqWebUI.Models.Dhcp;

namespace DnsmasqWebUI.Infrastructure.Services.Abstractions;

/// <summary>
/// Singleton cache for the leases file; invalidates when the file changes (e.g. via file watcher).
/// </summary>
public interface ILeasesCache : IApplicationSingleton
{
    /// <summary>Forces the next <see cref="GetOrRefreshAsync"/> to re-read the file. Use for manual Refresh; no need to recreate the file watcher.</summary>
    void Invalidate();

    Task<(bool Available, IReadOnlyList<LeaseEntry>? Entries)> GetOrRefreshAsync(CancellationToken ct = default);
}
