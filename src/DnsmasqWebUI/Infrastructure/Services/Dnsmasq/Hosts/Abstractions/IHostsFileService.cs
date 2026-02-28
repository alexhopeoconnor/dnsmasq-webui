using DnsmasqWebUI.Infrastructure.Services.Registration.Abstractions;
using DnsmasqWebUI.Models.Hosts;

namespace DnsmasqWebUI.Infrastructure.Services.Dnsmasq.Hosts.Abstractions;

/// <summary>Reads and writes the app-managed hosts file (the single hosts file the app edits).</summary>
public interface IHostsFileService : IApplicationScopedService
{
    /// <summary>Reads all entries from the managed hosts file (including comments and passthrough lines).</summary>
    Task<IReadOnlyList<HostEntry>> ReadAsync(CancellationToken ct = default);

    /// <summary>Writes the given entries to the managed hosts file (replaces file content).</summary>
    Task WriteAsync(IReadOnlyList<HostEntry> entries, CancellationToken ct = default);
}
