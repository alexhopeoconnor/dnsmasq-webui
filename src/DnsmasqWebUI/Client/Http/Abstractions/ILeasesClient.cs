using DnsmasqWebUI.Models.Dhcp;

namespace DnsmasqWebUI.Client.Http.Abstractions;

/// <summary>Typed client for GET api/leases.</summary>
public interface ILeasesClient
{
    Task<LeasesResult> GetLeasesAsync(CancellationToken ct = default);
}
