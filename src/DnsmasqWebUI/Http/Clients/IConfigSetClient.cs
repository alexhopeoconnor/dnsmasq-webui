using DnsmasqWebUI.Models;

namespace DnsmasqWebUI.Http.Clients;

/// <summary>Typed client for GET api/config/set.</summary>
public interface IConfigSetClient
{
    Task<DnsmasqConfigSet> GetConfigSetAsync(CancellationToken ct = default);
}
