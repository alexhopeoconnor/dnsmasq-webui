namespace DnsmasqWebUI.Infrastructure.Helpers.Config;

/// <summary>
/// Literal dnsmasq .conf option names (config keys). Use these everywhere we reference option names
/// so references are compile-time checked and easy to find/rename. Values match dnsmasq long-option
/// names without the leading "--". Case-sensitive per dnsmasq.
/// </summary>
public static class DnsmasqConfKeys
{
    // --- Include (main config discovery) ---
    public const string ConfFile = "conf-file";
    public const string ConfDir = "conf-dir";

    // --- Hosts ---
    public const string NoHosts = "no-hosts";
    public const string AddnHosts = "addn-hosts";

    // --- DHCP lease file (last wins) ---
    public const string DhcpLeasefile = "dhcp-leasefile";
    public const string DhcpLease = "dhcp-lease";

    // --- Multi-value (ARG_DUP) ---
    public const string Server = "server";
    public const string Local = "local";
    public const string RevServer = "rev-server";
    public const string Address = "address";
    public const string Interface = "interface";
    public const string ListenAddress = "listen-address";
    public const string ExceptInterface = "except-interface";
    public const string NoDhcpInterface = "no-dhcp-interface";
    public const string NoDhcpv4Interface = "no-dhcpv4-interface";
    public const string NoDhcpv6Interface = "no-dhcpv6-interface";
    public const string AuthServer = "auth-server";
    public const string DhcpRange = "dhcp-range";
    public const string DhcpHost = "dhcp-host";
    public const string DhcpOption = "dhcp-option";
    public const string ResolvFile = "resolv-file";
    public const string RebindDomainOk = "rebind-domain-ok";
    public const string BogusNxdomain = "bogus-nxdomain";
    public const string IgnoreAddress = "ignore-address";
    public const string Alias = "alias";
    public const string FilterRr = "filter-rr";
    public const string CacheRr = "cache-rr";

    // --- Flags (no value) ---
    public const string ExpandHosts = "expand-hosts";
    public const string BogusPriv = "bogus-priv";
    public const string StrictOrder = "strict-order";
    public const string AllServers = "all-servers";
    public const string NoResolv = "no-resolv";
    public const string DomainNeeded = "domain-needed";
    public const string NoPoll = "no-poll";
    public const string BindInterfaces = "bind-interfaces";
    public const string BindDynamic = "bind-dynamic";
    public const string NoNegcache = "no-negcache";
    public const string DnsLoopDetect = "dns-loop-detect";
    public const string StopDnsRebind = "stop-dns-rebind";
    public const string RebindLocalhostOk = "rebind-localhost-ok";
    public const string ClearOnReload = "clear-on-reload";
    public const string Filterwin2k = "filterwin2k";
    public const string FilterA = "filter-A";
    public const string FilterAaaa = "filter-AAAA";
    public const string LocaliseQueries = "localise-queries";
    public const string LogDebug = "log-debug";
    public const string DhcpAuthoritative = "dhcp-authoritative";
    public const string LeasefileRo = "leasefile-ro";

    // --- Single-value (last wins) ---
    public const string CacheSize = "cache-size";
    public const string Port = "port";
    public const string LocalTtl = "local-ttl";
    public const string AuthTtl = "auth-ttl";
    public const string EdnsPacketMax = "edns-packet-max";
    public const string QueryPort = "query-port";
    public const string PortLimit = "port-limit";
    public const string MinPort = "min-port";
    public const string MaxPort = "max-port";
    public const string LogAsync = "log-async";
    public const string LocalService = "local-service";
    public const string PidFile = "pid-file";
    public const string User = "user";
    public const string Group = "group";
    public const string LogFacility = "log-facility";
    public const string LogQueries = "log-queries";
    public const string DhcpLeaseMax = "dhcp-lease-max";
    public const string NegTtl = "neg-ttl";
    public const string MaxTtl = "max-ttl";
    public const string MaxCacheTtl = "max-cache-ttl";
    public const string MinCacheTtl = "min-cache-ttl";
    public const string DhcpTtl = "dhcp-ttl";

    /// <summary>Keys collected for effective config "server/local" multi-value (order preserved).</summary>
    public static readonly string[] ServerLocalKeys = { Server, Local };
}
