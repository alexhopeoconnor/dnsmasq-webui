using DnsmasqWebUI.Models.Config;
using DnsmasqWebUI.Models.Contracts;
using DnsmasqWebUI.Models.Dhcp;
using DnsmasqWebUI.Models.Dnsmasq;
using DnsmasqWebUI.Models.Dnsmasq.EffectiveConfig;

namespace DnsmasqWebUI.Infrastructure.Services.Abstractions;

/// <summary>Reads and writes the managed dnsmasq config file and DHCP hosts entries (the app-managed config and dhcp-host= lines).</summary>
public interface IDnsmasqConfigService : IApplicationScopedService
{
    /// <summary>Reads DHCP host entries from the managed config (dhcp-host= lines).</summary>
    Task<IReadOnlyList<DhcpHostEntry>> ReadDhcpHostsAsync(CancellationToken ct = default);

    /// <summary>Writes DHCP host entries to the managed config file (replaces dhcp-host= lines).</summary>
    Task WriteDhcpHostsAsync(IReadOnlyList<DhcpHostEntry> entries, CancellationToken ct = default);

    /// <summary>Reads the full managed config file as structured lines plus the effective addn-hosts path in file (for display).</summary>
    Task<ManagedConfigContent> ReadManagedConfigAsync(CancellationToken ct = default);

    /// <summary>Writes the full managed config file from the given lines (round-trip from ReadManagedConfigAsync).</summary>
    Task WriteManagedConfigAsync(IReadOnlyList<DnsmasqConfLine> lines, CancellationToken ct = default);
}
