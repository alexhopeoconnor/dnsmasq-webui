using DnsmasqWebUI.Models.Config;
using DnsmasqWebUI.Models.Contracts;
using DnsmasqWebUI.Models.Dhcp;
using DnsmasqWebUI.Models.Dnsmasq;
using DnsmasqWebUI.Models.Dnsmasq.EffectiveConfig;

namespace DnsmasqWebUI.Infrastructure.Services.Abstractions;

public interface IDnsmasqConfigService : IApplicationScopedService
{
    Task<IReadOnlyList<DhcpHostEntry>> ReadDhcpHostsAsync(CancellationToken ct = default);
    Task WriteDhcpHostsAsync(IReadOnlyList<DhcpHostEntry> entries, CancellationToken ct = default);
    Task<ManagedConfigContent> ReadManagedConfigAsync(CancellationToken ct = default);
    Task WriteManagedConfigAsync(IReadOnlyList<DnsmasqConfLine> lines, CancellationToken ct = default);
}
