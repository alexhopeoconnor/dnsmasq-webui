using DnsmasqWebUI.Models;

namespace DnsmasqWebUI.Http.Clients;

/// <summary>Typed client for GET api/leases.</summary>
public interface ILeasesClient
{
    Task<LeasesResult> GetLeasesAsync(CancellationToken ct = default);
}
