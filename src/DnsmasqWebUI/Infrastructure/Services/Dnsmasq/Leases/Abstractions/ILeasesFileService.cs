using DnsmasqWebUI.Infrastructure.Services.Registration.Abstractions;
using DnsmasqWebUI.Models.Dhcp;

namespace DnsmasqWebUI.Infrastructure.Services.Dnsmasq.Leases.Abstractions;

/// <summary>Reads the dnsmasq DHCP leases file (path from effective config).</summary>
public interface ILeasesFileService : IApplicationScopedService
{
    /// <summary>Reads lease entries from the leases file. Throws if file is not configured or not readable.</summary>
    Task<IReadOnlyList<LeaseEntry>> ReadAsync(CancellationToken ct = default);

    /// <summary>Attempts to read the leases file. Returns (false, null) when not configured or not readable; (true, entries) otherwise.</summary>
    Task<(bool Available, IReadOnlyList<LeaseEntry>? Entries)> TryReadAsync(CancellationToken ct = default);
}
