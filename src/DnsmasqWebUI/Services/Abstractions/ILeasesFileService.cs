using DnsmasqWebUI.Models.Dhcp;

namespace DnsmasqWebUI.Services.Abstractions;

public interface ILeasesFileService : IApplicationScopedService
{
    Task<IReadOnlyList<LeaseEntry>> ReadAsync(CancellationToken ct = default);
    Task<(bool Available, IReadOnlyList<LeaseEntry>? Entries)> TryReadAsync(CancellationToken ct = default);
}
