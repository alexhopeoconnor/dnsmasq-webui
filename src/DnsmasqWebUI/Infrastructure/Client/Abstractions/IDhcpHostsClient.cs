using DnsmasqWebUI.Models.Dhcp;
using DnsmasqWebUI.Models.Dnsmasq;
using DnsmasqWebUI.Models.Dnsmasq.EffectiveConfig;

namespace DnsmasqWebUI.Infrastructure.Client.Abstractions;

/// <summary>Typed client for GET api/dhcp/hosts. Save goes through effective-config save flow.</summary>
public interface IDhcpHostsClient
{
    /// <summary>Gets DHCP host entries from GET api/dhcp/hosts.</summary>
    Task<IReadOnlyList<DhcpHostEntry>> GetDhcpHostsAsync(CancellationToken ct = default);
}
