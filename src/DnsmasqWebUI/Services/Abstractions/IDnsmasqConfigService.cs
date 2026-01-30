using DnsmasqWebUI.Models;

namespace DnsmasqWebUI.Services.Abstractions;

public interface IDnsmasqConfigService : IApplicationScopedService
{
    Task<IReadOnlyList<DhcpHostEntry>> ReadDhcpHostsAsync(CancellationToken ct = default);
    Task WriteDhcpHostsAsync(IReadOnlyList<DhcpHostEntry> entries, CancellationToken ct = default);
}
