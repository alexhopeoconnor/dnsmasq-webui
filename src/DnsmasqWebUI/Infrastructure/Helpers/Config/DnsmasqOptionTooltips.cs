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

        // --- DHCP ---
        [DnsmasqConfKeys.DhcpAuthoritative] = "DHCP server is authoritative; required for DHCP to work.",
        [DnsmasqConfKeys.LeasefileRo] = "Do not create or truncate the DHCP lease file.",
        [DnsmasqConfKeys.DhcpLeasefile] = "Path to the DHCP lease file.",
        [DnsmasqConfKeys.DhcpLeaseMax] = "Maximum number of DHCP leases to hold.",
        [DnsmasqConfKeys.DhcpTtl] = "TTL for DHCP names in the DNS cache.",
        [DnsmasqConfKeys.DhcpRange] = "DHCP address range(s) and lease time. Can repeat.",
        [DnsmasqConfKeys.DhcpHost] = "DHCP host entries (reservations by MAC or client-id). Can repeat.",
        [DnsmasqConfKeys.DhcpOption] = "DHCP option lines sent to clients. Can repeat.",

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
    }.ToFrozenDictionary();

    /// <summary>Returns a short tooltip for the option, or null if none is defined.</summary>
    public static string? Get(string optionName)
    {
        return Tooltips.TryGetValue(optionName, out var tip) ? tip : null;
    }
}
