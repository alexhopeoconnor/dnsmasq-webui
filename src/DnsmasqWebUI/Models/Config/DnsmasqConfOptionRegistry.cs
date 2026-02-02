namespace DnsmasqWebUI.Models.Config;

/// <summary>
/// Registry of dnsmasq .conf option names to their <see cref="DnsmasqOptionKind"/>.
/// Used by <see cref="DnsmasqConfDirectiveParser"/> to dispatch each line to the correct backing model.
/// Add entries as we add parsers for more options.
/// </summary>
public static class DnsmasqConfOptionRegistry
{
    /// <summary>Option name (config key) to kind. Keys are lowercase; config file uses same as long option without "--".</summary>
    public static IReadOnlyDictionary<string, DnsmasqOptionKind> OptionKindByKey { get; } =
        new Dictionary<string, DnsmasqOptionKind>(StringComparer.OrdinalIgnoreCase)
        {
            // Include
            { "conf-file", DnsmasqOptionKind.ConfFile },
            { "conf-dir", DnsmasqOptionKind.ConfDir },
            // Path-valued
            { "addn-hosts", DnsmasqOptionKind.AddnHosts },
            { "dhcp-leasefile", DnsmasqOptionKind.DhcpLeaseFile },
            { "dhcp-lease", DnsmasqOptionKind.DhcpLeaseFile },
            { "resolv-file", DnsmasqOptionKind.Path },
            { "dhcp-hostsfile", DnsmasqOptionKind.Path },
            { "dhcp-optsfile", DnsmasqOptionKind.Path },
            { "pid-file", DnsmasqOptionKind.Path },
            { "hostsdir", DnsmasqOptionKind.Path },
            { "dhcp-script", DnsmasqOptionKind.Path },
            { "dhcp-lua-script", DnsmasqOptionKind.Path },
            { "read-ethers", DnsmasqOptionKind.Path },
            // Simple string / domain
            { "domain", DnsmasqOptionKind.Domain },
            { "interface", DnsmasqOptionKind.String },
            { "listen-address", DnsmasqOptionKind.String },
            { "port", DnsmasqOptionKind.String },
            { "user", DnsmasqOptionKind.String },
            { "group", DnsmasqOptionKind.String },
            { "cache-size", DnsmasqOptionKind.String },
            { "local-ttl", DnsmasqOptionKind.String },
            { "log-facility", DnsmasqOptionKind.String },
            { "dhcp-ignore", DnsmasqOptionKind.String },
            { "min-port", DnsmasqOptionKind.String },
            { "max-port", DnsmasqOptionKind.String },
            { "query-port", DnsmasqOptionKind.String },
            { "edns-packet-max", DnsmasqOptionKind.String },
            // DHCP structured
            { "dhcp-range", DnsmasqOptionKind.DhcpRange },
            { "dhcp-host", DnsmasqOptionKind.DhcpHost },
            { "dhcp-option", DnsmasqOptionKind.DhcpOption },
            // DNS structured
            { "server", DnsmasqOptionKind.Server },
            { "local", DnsmasqOptionKind.Local },
            { "address", DnsmasqOptionKind.Address },
        };

    /// <summary>Well-known flag options (no value).</summary>
    public static IReadOnlySet<string> FlagOptions { get; } =
        new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "domain-needed", "bogus-priv", "no-hosts", "expand-hosts", "strict-order",
            "no-resolv", "no-poll", "bind-interfaces", "no-negcache", "log-queries", "log-dhcp",
            "all-servers", "leasefile-ro", "dhcp-authoritative",
            "quiet-dhcp", "quiet-dhcp6", "quiet-ra", "dhcp-broadcast", "dhcp-sequential-ip",
            "enable-tftp", "self-resolve", "conntrack",
        };

    /// <summary>Resolve option key (e.g. "addn-hosts") to kind; returns Raw if unknown.</summary>
    public static DnsmasqOptionKind GetKind(string key)
    {
        if (string.IsNullOrWhiteSpace(key)) return DnsmasqOptionKind.Raw;
        var k = key.Trim();
        if (FlagOptions.Contains(k)) return DnsmasqOptionKind.Flag;
        return OptionKindByKey.TryGetValue(k, out var kind) ? kind : DnsmasqOptionKind.Raw;
    }
}
