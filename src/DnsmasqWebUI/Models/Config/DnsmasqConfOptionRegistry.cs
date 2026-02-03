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
            { DnsmasqConfKeys.ConfFile, DnsmasqOptionKind.ConfFile },
            { DnsmasqConfKeys.ConfDir, DnsmasqOptionKind.ConfDir },
            // Path-valued
            { DnsmasqConfKeys.AddnHosts, DnsmasqOptionKind.AddnHosts },
            { DnsmasqConfKeys.DhcpLeasefile, DnsmasqOptionKind.DhcpLeaseFile },
            { DnsmasqConfKeys.DhcpLease, DnsmasqOptionKind.DhcpLeaseFile },
            { DnsmasqConfKeys.ResolvFile, DnsmasqOptionKind.Path },
            { "dhcp-hostsfile", DnsmasqOptionKind.Path },
            { "dhcp-optsfile", DnsmasqOptionKind.Path },
            { DnsmasqConfKeys.PidFile, DnsmasqOptionKind.Path },
            { "hostsdir", DnsmasqOptionKind.Path },
            { "dhcp-script", DnsmasqOptionKind.Path },
            { "dhcp-lua-script", DnsmasqOptionKind.Path },
            { "read-ethers", DnsmasqOptionKind.Path },
            // Simple string / domain
            { "domain", DnsmasqOptionKind.Domain },
            { DnsmasqConfKeys.Interface, DnsmasqOptionKind.String },
            { DnsmasqConfKeys.ListenAddress, DnsmasqOptionKind.String },
            { DnsmasqConfKeys.Port, DnsmasqOptionKind.String },
            { DnsmasqConfKeys.User, DnsmasqOptionKind.String },
            { DnsmasqConfKeys.Group, DnsmasqOptionKind.String },
            { DnsmasqConfKeys.CacheSize, DnsmasqOptionKind.String },
            { DnsmasqConfKeys.LocalTtl, DnsmasqOptionKind.String },
            { DnsmasqConfKeys.LogFacility, DnsmasqOptionKind.String },
            { "dhcp-ignore", DnsmasqOptionKind.String },
            { "min-port", DnsmasqOptionKind.String },
            { "max-port", DnsmasqOptionKind.String },
            { "query-port", DnsmasqOptionKind.String },
            { "edns-packet-max", DnsmasqOptionKind.String },
            // DHCP structured
            { DnsmasqConfKeys.DhcpRange, DnsmasqOptionKind.DhcpRange },
            { DnsmasqConfKeys.DhcpHost, DnsmasqOptionKind.DhcpHost },
            { DnsmasqConfKeys.DhcpOption, DnsmasqOptionKind.DhcpOption },
            // DNS structured
            { DnsmasqConfKeys.Server, DnsmasqOptionKind.Server },
            { DnsmasqConfKeys.Local, DnsmasqOptionKind.Local },
            { DnsmasqConfKeys.Address, DnsmasqOptionKind.Address },
        };

    /// <summary>Well-known flag options (no value).</summary>
    public static IReadOnlySet<string> FlagOptions { get; } =
        new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            DnsmasqConfKeys.DomainNeeded, DnsmasqConfKeys.BogusPriv, DnsmasqConfKeys.NoHosts, DnsmasqConfKeys.ExpandHosts, DnsmasqConfKeys.StrictOrder,
            DnsmasqConfKeys.NoResolv, DnsmasqConfKeys.NoPoll, DnsmasqConfKeys.BindInterfaces, DnsmasqConfKeys.NoNegcache, "log-queries", "log-dhcp",
            "all-servers", DnsmasqConfKeys.LeasefileRo, DnsmasqConfKeys.DhcpAuthoritative,
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
