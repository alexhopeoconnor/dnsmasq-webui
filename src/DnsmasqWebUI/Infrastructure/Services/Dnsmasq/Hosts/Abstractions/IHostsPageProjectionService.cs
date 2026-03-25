using DnsmasqWebUI.Infrastructure.Services.Registration.Abstractions;
using DnsmasqWebUI.Models.Hosts;

namespace DnsmasqWebUI.Infrastructure.Services.Dnsmasq.Hosts.Abstractions;

/// <summary>
/// Projects flat <see cref="HostsPageRow"/> into grouped, filtered, sorted Hosts page groups.
/// </summary>
public interface IHostsPageProjectionService : IApplicationSingleton
{
    IReadOnlyList<HostsPageGroup> BuildGroups(IReadOnlyList<HostsPageRow> rows, HostsPageQueryState query);
}
