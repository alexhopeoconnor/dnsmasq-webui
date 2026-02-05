using DnsmasqWebUI.Models.Dhcp;

namespace DnsmasqWebUI.Infrastructure.Client.Abstractions;

/// <summary>Typed client for GET api/leases.</summary>
public interface ILeasesClient
{
    /// <param name="forceRefresh">When true, invalidates the server cache so the next read is from disk (e.g. after manual Refresh).</param>
    Task<LeasesResult> GetLeasesAsync(bool forceRefresh = false, CancellationToken ct = default);
}
