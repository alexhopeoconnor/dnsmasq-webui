using DnsmasqWebUI.Models.Dhcp;
using DnsmasqWebUI.Models.Dnsmasq;
using DnsmasqWebUI.Models.Dnsmasq.EffectiveConfig;

namespace DnsmasqWebUI.Infrastructure.Client.Abstractions;

/// <summary>Typed client for GET/PUT api/dhcp/hosts.</summary>
public interface IDhcpHostsClient
{
    /// <summary>Gets DHCP host entries from GET api/dhcp/hosts.</summary>
    Task<IReadOnlyList<DhcpHostEntry>> GetDhcpHostsAsync(CancellationToken ct = default);

    /// <summary>Writes DHCP host entries and triggers reload via PUT api/dhcp/hosts.</summary>
    Task<SaveWithReloadResult> SaveDhcpHostsAsync(IReadOnlyList<DhcpHostEntry> entries, CancellationToken ct = default);
}
