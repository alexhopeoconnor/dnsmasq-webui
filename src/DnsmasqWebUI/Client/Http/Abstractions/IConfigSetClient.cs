using DnsmasqWebUI.Models;

namespace DnsmasqWebUI.Client.Http.Abstractions;

/// <summary>Typed client for GET api/config/set.</summary>
public interface IConfigSetClient
{
    Task<DnsmasqConfigSet> GetConfigSetAsync(CancellationToken ct = default);
}
