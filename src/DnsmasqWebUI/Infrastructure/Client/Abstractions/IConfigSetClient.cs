using DnsmasqWebUI.Models.Dnsmasq.EffectiveConfig;

namespace DnsmasqWebUI.Infrastructure.Client.Abstractions;

/// <summary>Typed client for GET api/config/set.</summary>
public interface IConfigSetClient
{
    Task<DnsmasqConfigSet> GetConfigSetAsync(CancellationToken ct = default);
}
