using DnsmasqWebUI.Models;

namespace DnsmasqWebUI.Http.Clients;

/// <summary>Typed client for GET/PUT api/dhcp/hosts.</summary>
public interface IDhcpHostsClient
{
    Task<IReadOnlyList<DhcpHostEntry>> GetDhcpHostsAsync(CancellationToken ct = default);
    Task<SaveWithReloadResult> SaveDhcpHostsAsync(IReadOnlyList<DhcpHostEntry> entries, CancellationToken ct = default);
}
