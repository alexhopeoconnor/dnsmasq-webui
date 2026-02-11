using System.Collections.Frozen;

namespace DnsmasqWebUI.Infrastructure.Helpers.Config;

/// <summary>
/// Short explainer tooltips for dnsmasq option names. Shown on hover over the field label.
/// One entry per effective-config key for consistency.
/// </summary>
public static class DnsmasqOptionTooltips
{
    /// <summary>Display label used in EffectiveConfigFieldBuilder for server/local multi-value.</summary>
    public const string ServerLocalLabel = "server / local";

    private static readonly FrozenDictionary<string, string> Tooltips = new Dictionary<string, string>(StringComparer.Ordinal)
    {
        // --- Hosts ---
        [DnsmasqConfKeys.NoHosts] = "Do not read /etc/hosts; only use addn-hosts and DHCP/hosts.",
        [DnsmasqConfKeys.AddnHosts] = "Additional hosts files (one per line).",
        [DnsmasqConfKeys.Hostsdir] = "Directory of hosts files; new/changed files are read automatically.",
        [DnsmasqConfKeys.ReadEthers] = "Read /etc/ethers for MAC→IP mappings (like dhcp-host).",

        // --- Resolver / DNS ---
        [DnsmasqConfKeys.ExpandHosts] = "Expand simple names in /etc/hosts with the local domain.",
        [DnsmasqConfKeys.BogusPriv] = "Do not forward reverse lookups for private IP ranges.",
        [DnsmasqConfKeys.StrictOrder] = "Query servers in the order specified; do not try the next on failure.",
        [DnsmasqConfKeys.AllServers] = "Send each query to all upstream servers; use the first reply.",
        [DnsmasqConfKeys.NoResolv] = "Do not read /etc/resolv.conf; use only server= and resolv-file=.",
        [DnsmasqConfKeys.DomainNeeded] = "Never forward plain names (without a domain) to upstream.",
        [DnsmasqConfKeys.Port] = "Listen on this port for DNS queries (default 53).",
        [DnsmasqConfKeys.LogQueries] = "Log DNS queries. Optional value: extra, proto, or auth.",
        [ServerLocalLabel] = "Upstream DNS servers (server=) and local-only domains (local=). Order is preserved.",
        [DnsmasqConfKeys.RevServer] = "Reverse DNS server (e.g. rev-server=1.2.3.0/24,192.168.0.1). Can repeat.",
        [DnsmasqConfKeys.Address] = "Map a domain or hostname to an IP (e.g. for ad blocking or local names). Can repeat.",
        [DnsmasqConfKeys.ResolvFile] = "File(s) to read upstream server addresses from (e.g. from DHCP). Can repeat.",
        [DnsmasqConfKeys.AuthTtl] = "TTL for answers from the authoritative server.",
        [DnsmasqConfKeys.EdnsPacketMax] = "Largest EDNS.0 UDP packet size (default 1232).",
        [DnsmasqConfKeys.QueryPort] = "Source port for outbound DNS queries (0 = single OS-allocated port).",
        [DnsmasqConfKeys.PortLimit] = "Number of source ports for outbound queries.",
        [DnsmasqConfKeys.MinPort] = "Minimum source port for outbound DNS queries.",
        [DnsmasqConfKeys.MaxPort] = "Maximum source port for outbound DNS queries.",
        [DnsmasqConfKeys.DnsLoopDetect] = "Detect DNS forwarding loops and disable bad upstream servers.",
        [DnsmasqConfKeys.StopDnsRebind] = "Reject private-range addresses from upstream (anti-rebind).",
        [DnsmasqConfKeys.RebindLocalhostOk] = "Allow 127.0.0.0/8 and ::1 in upstream replies.",
        [DnsmasqConfKeys.ClearOnReload] = "Clear DNS cache when resolv.conf or servers are reloaded.",
        [DnsmasqConfKeys.Filterwin2k] = "Filter Windows LDAP/SOA/SRV queries that trigger dial-on-demand.",
        [DnsmasqConfKeys.FilterA] = "Remove A (IPv4) records from answers.",
        [DnsmasqConfKeys.FilterAaaa] = "Remove AAAA (IPv6) records from answers.",
        [DnsmasqConfKeys.LocaliseQueries] = "Return interface-specific answers from hosts/DHCP.",
        [DnsmasqConfKeys.FastDnsRetry] = "DNS retry parameters: initial delay (ms) and optional continue time (ms).",
        [DnsmasqConfKeys.RebindDomainOk] = "Domains exempt from rebind checks. Can repeat.",
        [DnsmasqConfKeys.BogusNxdomain] = "Transform replies containing these IPs to NXDOMAIN. Can repeat.",
        [DnsmasqConfKeys.IgnoreAddress] = "Ignore A/AAAA replies containing these IPs. Can repeat.",
        [DnsmasqConfKeys.Alias] = "Rewrite IPs in upstream replies (DNS doctoring). Can repeat.",
        [DnsmasqConfKeys.FilterRr] = "Remove these RR types from answers. Can repeat.",
        [DnsmasqConfKeys.CacheRr] = "Cache these RR types in addition to default. Can repeat.",
        [DnsmasqConfKeys.AuthServer] = "Authoritative DNS mode (domain, interface/IP). Can repeat.",
        [DnsmasqConfKeys.NoDhcpInterface] = "No DHCP/TFTP/RA on these interfaces; DNS only. Can repeat.",
        [DnsmasqConfKeys.NoDhcpv4Interface] = "No DHCPv4 on these interfaces. Can repeat.",
        [DnsmasqConfKeys.NoDhcpv6Interface] = "No DHCPv6/RA on these interfaces. Can repeat.",
        [DnsmasqConfKeys.Ipset] = "Add resolved IPs to ipset (domain, set names). Can repeat.",
        [DnsmasqConfKeys.Nftset] = "Add resolved IPs to nftables sets. Can repeat.",

        // --- DNS records ---
        [DnsmasqConfKeys.Domain] = "Local domain(s); optional IP. Can repeat (e.g. domain=home,192.168.1.1).",
        [DnsmasqConfKeys.Cname] = "CNAME records (alias, target). Can repeat.",
        [DnsmasqConfKeys.MxHost] = "MX records (domain, host, priority). Can repeat.",
        [DnsmasqConfKeys.MxTarget] = "Default target for MX records (used with localmx).",
        [DnsmasqConfKeys.Localmx] = "Serve MX for local machines using mx-target.",
        [DnsmasqConfKeys.Selfmx] = "Serve MX pointing to self for local machines.",
        [DnsmasqConfKeys.Srv] = "SRV records. Can repeat.",
        [DnsmasqConfKeys.PtrRecord] = "PTR records for reverse DNS. Can repeat.",
        [DnsmasqConfKeys.TxtRecord] = "TXT records. Can repeat.",
        [DnsmasqConfKeys.NaptrRecord] = "NAPTR records. Can repeat.",
        [DnsmasqConfKeys.HostRecord] = "A/AAAA host records (name, IP). Can repeat.",
        [DnsmasqConfKeys.DynamicHost] = "Dynamic host entries. Can repeat.",
        [DnsmasqConfKeys.InterfaceName] = "Name per interface (for localise-queries). Can repeat.",

        // --- DHCP ---
        [DnsmasqConfKeys.DhcpAuthoritative] = "DHCP server is authoritative; required for DHCP to work.",
        [DnsmasqConfKeys.DhcpRapidCommit] = "DHCPv4 Rapid Commit (RFC 4039); use only if single server or all commit.",
        [DnsmasqConfKeys.DhcpScript] = "Script run on lease add/delete (add|del, MAC, IP, hostname).",
        [DnsmasqConfKeys.LeasefileRo] = "Do not create or truncate the DHCP lease file.",
        [DnsmasqConfKeys.DhcpLeasefile] = "Path to the DHCP lease file.",
        [DnsmasqConfKeys.DhcpLeaseMax] = "Maximum number of DHCP leases to hold.",
        [DnsmasqConfKeys.DhcpTtl] = "TTL for DHCP names in the DNS cache.",
        [DnsmasqConfKeys.DhcpRange] = "DHCP address range(s) and lease time. Can repeat.",
        [DnsmasqConfKeys.DhcpHost] = "DHCP host entries (reservations by MAC or client-id). Can repeat.",
        [DnsmasqConfKeys.DhcpOption] = "DHCP option lines sent to clients. Can repeat.",
        [DnsmasqConfKeys.DhcpOptionForce] = "Like dhcp-option but force send even if not in parameter request list. Can repeat.",
        [DnsmasqConfKeys.DhcpMatch] = "Match DHCP clients (e.g. set tag from option). Can repeat.",
        [DnsmasqConfKeys.DhcpMac] = "Set tag when client MAC matches pattern. Can repeat.",
        [DnsmasqConfKeys.DhcpNameMatch] = "Set tag when client name matches. Can repeat.",
        [DnsmasqConfKeys.DhcpIgnoreNames] = "Ignore client names when tag set. Can repeat.",
        [DnsmasqConfKeys.DhcpBoot] = "Boot file and server for PXE. Can repeat.",
        [DnsmasqConfKeys.DhcpIgnore] = "Ignore DHCP clients (e.g. by tag). Can repeat.",
        [DnsmasqConfKeys.DhcpVendorclass] = "Match by DHCP vendor class. Can repeat.",
        [DnsmasqConfKeys.DhcpUserclass] = "Match by DHCP user class. Can repeat.",
        [DnsmasqConfKeys.RaParam] = "Router advertisement parameters (DHCPv6/RA). Can repeat.",
        [DnsmasqConfKeys.Slaac] = "SLAAC host part for DHCPv6/RA. Can repeat.",

        // --- TFTP / PXE ---
        [DnsmasqConfKeys.EnableTftp] = "Enable the built-in TFTP server.",
        [DnsmasqConfKeys.TftpSecure] = "Only allow TFTP access from DHCP-assigned clients.",
        [DnsmasqConfKeys.TftpNoFail] = "Do not abort if tftp-root is missing.",
        [DnsmasqConfKeys.TftpNoBlocksize] = "Do not negotiate TFTP blocksize (for broken clients).",
        [DnsmasqConfKeys.TftpRoot] = "Root directory for TFTP files.",
        [DnsmasqConfKeys.PxePrompt] = "PXE boot prompt (e.g. timeout,0 to disable).",
        [DnsmasqConfKeys.PxeService] = "PXE service definitions. Can repeat.",

        // --- DNSSEC ---
        [DnsmasqConfKeys.Dnssec] = "Validate DNSSEC on replies from upstream.",
        [DnsmasqConfKeys.DnssecCheckUnsigned] = "Treat unsigned zones as bogus.",
        [DnsmasqConfKeys.TrustAnchor] = "DNSSEC trust anchors. Can repeat.",

        // --- Cache ---
        [DnsmasqConfKeys.CacheSize] = "Maximum number of DNS cache entries (0 to disable).",
        [DnsmasqConfKeys.LocalTtl] = "TTL for entries from /etc/hosts and DHCP.",
        [DnsmasqConfKeys.NoNegcache] = "Do not cache negative (NXDOMAIN) responses.",
        [DnsmasqConfKeys.NegTtl] = "TTL for negative (cached miss) responses.",
        [DnsmasqConfKeys.MaxTtl] = "Maximum TTL for cached responses (cap on upstream TTL).",
        [DnsmasqConfKeys.MaxCacheTtl] = "Maximum TTL for positive cache entries.",
        [DnsmasqConfKeys.MinCacheTtl] = "Minimum TTL for positive cache entries.",

        // --- Process & networking ---
        [DnsmasqConfKeys.NoPoll] = "Do not poll /etc/resolv.conf for changes.",
        [DnsmasqConfKeys.BindInterfaces] = "Only bind to interfaces in use; do not bind to wildcard.",
        [DnsmasqConfKeys.Interface] = "Only use these interfaces for DHCP/DNS. Can repeat.",
        [DnsmasqConfKeys.ListenAddress] = "Listen on these addresses for DNS/DHCP. Can repeat.",
        [DnsmasqConfKeys.ExceptInterface] = "Do not use these interfaces. Can repeat.",
        [DnsmasqConfKeys.PidFile] = "Write process ID to this file.",
        [DnsmasqConfKeys.User] = "Run as this user after startup (requires root).",
        [DnsmasqConfKeys.Group] = "Run as this group after startup (requires root).",
        [DnsmasqConfKeys.LogFacility] = "Syslog facility for dnsmasq logs (e.g. local0, daemon).",
        [DnsmasqConfKeys.LogDebug] = "Extra debug logging.",
        [DnsmasqConfKeys.LogAsync] = "Asynchronous logging; optional queue size.",
        [DnsmasqConfKeys.BindDynamic] = "Bind per-interface; auto-add new interfaces (Linux).",
        [DnsmasqConfKeys.LocalService] = "Restrict to local network or localhost (net|host).",
        [DnsmasqConfKeys.EnableDbus] = "Enable DBus interface (optional service name).",
        [DnsmasqConfKeys.EnableUbus] = "Enable UBus interface (optional service name).",
        [DnsmasqConfKeys.EnableRa] = "Enable router advertisements for DHCPv6 subnets.",
        [DnsmasqConfKeys.LogDhcp] = "Log DHCP transactions (leases, options).",
    }.ToFrozenDictionary();

    /// <summary>Returns a short tooltip for the option, or null if none is defined.</summary>
    public static string? Get(string optionName)
    {
        return Tooltips.TryGetValue(optionName, out var tip) ? tip : null;
    }
}
