using DnsmasqWebUI.Models.Dhcp;
using DnsmasqWebUI.Models.Dnsmasq;
using DnsmasqWebUI.Models.Dnsmasq.EffectiveConfig;

namespace DnsmasqWebUI.Models.Contracts;

/// <summary>Immutable snapshot from the config set cache: config set, effective config, sources, managed file content, and DHCP host entries (one read per refresh).</summary>
public record ConfigSetSnapshot(
    DnsmasqConfigSet Set,
    EffectiveDnsmasqConfig Config,
    EffectiveConfigSources Sources,
    ManagedConfigContent ManagedContent,
    IReadOnlyList<DhcpHostEntry> DhcpHostEntries
);
