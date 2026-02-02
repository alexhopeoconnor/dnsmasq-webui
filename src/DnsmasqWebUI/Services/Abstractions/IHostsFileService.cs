using DnsmasqWebUI.Models.Hosts;

namespace DnsmasqWebUI.Services.Abstractions;

public interface IHostsFileService : IApplicationScopedService
{
    Task<IReadOnlyList<HostEntry>> ReadAsync(CancellationToken ct = default);
    Task WriteAsync(IReadOnlyList<HostEntry> entries, CancellationToken ct = default);
}
