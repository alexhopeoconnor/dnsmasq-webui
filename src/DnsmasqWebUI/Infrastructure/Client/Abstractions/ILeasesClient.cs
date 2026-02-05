using DnsmasqWebUI.Models.Dhcp;

namespace DnsmasqWebUI.Infrastructure.Client.Abstractions;

/// <summary>Typed client for GET api/leases.</summary>
public interface ILeasesClient
{
    /// <summary>Gets DHCP lease entries from GET api/leases. Optionally forces a cache refresh on the server.</summary>
    /// <param name="forceRefresh">When true, invalidates the server cache so the next read is from disk (e.g. after manual Refresh).</param>
    Task<LeasesResult> GetLeasesAsync(bool forceRefresh = false, CancellationToken ct = default);
}
