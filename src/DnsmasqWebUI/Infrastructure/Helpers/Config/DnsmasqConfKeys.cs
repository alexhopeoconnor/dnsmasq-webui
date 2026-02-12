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
    public const string Hostsdir = "hostsdir";
    public const string ReadEthers = "read-ethers";

    // --- DHCP lease file (last wins) ---
    public const string DhcpLeasefile = "dhcp-leasefile";
    public const string DhcpLease = "dhcp-lease";

    // --- DHCP include files/dirs (multi-value paths; lines still come from conf only, we just show paths) ---
    public const string DhcpHostsfile = "dhcp-hostsfile";
    public const string DhcpOptsfile = "dhcp-optsfile";
    public const string DhcpHostsdir = "dhcp-hostsdir";

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
    public const string DhcpOptionForce = "dhcp-option-force";
    public const string DhcpMatch = "dhcp-match";
    public const string DhcpMac = "dhcp-mac";
    public const string DhcpNameMatch = "dhcp-name-match";
    public const string DhcpIgnoreNames = "dhcp-ignore-names";
    public const string DhcpBoot = "dhcp-boot";
    public const string DhcpIgnore = "dhcp-ignore";
    public const string DhcpVendorclass = "dhcp-vendorclass";
    public const string DhcpUserclass = "dhcp-userclass";
    public const string RaParam = "ra-param";
    public const string Slaac = "slaac";
    public const string PxeService = "pxe-service";
    public const string TrustAnchor = "trust-anchor";
    public const string ResolvFile = "resolv-file";
    public const string RebindDomainOk = "rebind-domain-ok";
    public const string BogusNxdomain = "bogus-nxdomain";
    public const string IgnoreAddress = "ignore-address";
    public const string Alias = "alias";
    public const string FilterRr = "filter-rr";
    public const string CacheRr = "cache-rr";
    public const string Ipset = "ipset";
    public const string Nftset = "nftset";
    // --- DNS records (authoritative / local) ---
    public const string Domain = "domain";
    public const string Cname = "cname";
    public const string MxHost = "mx-host";
    public const string MxTarget = "mx-target";
    public const string Srv = "srv-host";
    public const string PtrRecord = "ptr-record";
    public const string TxtRecord = "txt-record";
    public const string NaptrRecord = "naptr-record";
    public const string HostRecord = "host-record";
    public const string DynamicHost = "dynamic-host";
    public const string InterfaceName = "interface-name";

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
    public const string DhcpRapidCommit = "dhcp-rapid-commit";
    public const string LeasefileRo = "leasefile-ro";
    public const string Localmx = "localmx";
    public const string Selfmx = "selfmx";
    public const string EnableRa = "enable-ra";
    public const string LogDhcp = "log-dhcp";
    public const string EnableTftp = "enable-tftp";
    public const string TftpSecure = "tftp-secure";
    public const string TftpNoFail = "tftp-no-fail";
    public const string TftpNoBlocksize = "tftp-no-blocksize";
    public const string Dnssec = "dnssec";
    public const string DnssecCheckUnsigned = "dnssec-check-unsigned";
    public const string ProxyDnssec = "proxy-dnssec";

    // --- Process / daemon behaviour (flags) ---
    public const string KeepInForeground = "keep-in-foreground";
    public const string NoDaemon = "no-daemon";

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
    public const string TftpRoot = "tftp-root";
    public const string PxePrompt = "pxe-prompt";
    public const string EnableDbus = "enable-dbus";
    public const string EnableUbus = "enable-ubus";
    public const string FastDnsRetry = "fast-dns-retry";
    public const string DhcpScript = "dhcp-script";

    // --- Niche / platform (Linux conntrack mark for UBus/query filtering) ---
    public const string Conntrack = "conntrack";

    /// <summary>Keys collected for effective config "server/local" multi-value (order preserved).</summary>
    public static readonly string[] ServerLocalKeys = { Server, Local };
}
