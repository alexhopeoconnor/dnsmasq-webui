using DnsmasqWebUI.Models.Dhcp;
using DnsmasqWebUI.Models.Dnsmasq;
using DnsmasqWebUI.Models.Dnsmasq.EffectiveConfig;

namespace DnsmasqWebUI.Models.Contracts;

/// <summary>Immutable snapshot from the config set cache: config set, effective config, sources, managed file content, and DHCP host entries (one read per refresh).</summary>
/// <param name="Set">Ordered config set (main + conf-file/conf-dir) and managed file paths.</param>
/// <param name="Config">Effective dnsmasq config built from all files.</param>
/// <param name="Sources">Source per field (file path, readonly) for UI tooltips.</param>
/// <param name="ManagedContent">Parsed lines and effective addn-hosts path of the managed config file.</param>
/// <param name="DhcpHostEntries">DHCP host entries read from the managed file.</param>
public record ConfigSetSnapshot(
    DnsmasqConfigSet Set,
    EffectiveDnsmasqConfig Config,
    EffectiveConfigSources Sources,
    ManagedConfigContent ManagedContent,
    IReadOnlyList<DhcpHostEntry> DhcpHostEntries
);
