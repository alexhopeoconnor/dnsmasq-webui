namespace DnsmasqWebUI.Models;

/// <summary>
/// Kind of dnsmasq .conf option. Each kind has a dedicated parser and backing model
/// so we parse once into structured data instead of re-scanning files for single fields.
/// See testdata/dnsmasq.conf.example and man 8 dnsmasq.
/// </summary>
public enum DnsmasqOptionKind
{
    /// <summary>Unknown or not-yet-typed option; store as key=value.</summary>
    Raw,

    // --- Include (main config only) ---
    /// <summary>conf-file=path → <see cref="ConfFileOption"/></summary>
    ConfFile,
    /// <summary>conf-dir=path[,suffix] → <see cref="ConfDirOption"/></summary>
    ConfDir,

    // --- Path-valued (single or cumulative) ---
    /// <summary>addn-hosts=path (cumulative) → <see cref="AddnHostsOption"/></summary>
    AddnHosts,
    /// <summary>dhcp-leasefile= or dhcp-lease-file= (last wins) → <see cref="DhcpLeaseFileOption"/></summary>
    DhcpLeaseFile,
    /// <summary>resolv-file=, dhcp-hostsfile=, etc. → path model</summary>
    Path,

    // --- Simple string ---
    /// <summary>domain=value → <see cref="DomainOption"/></summary>
    Domain,
    /// <summary>user=, group=, port=, etc. → RawOption</summary>
    String,
    /// <summary>Flag (no value): domain-needed, bogus-priv, no-hosts, expand-hosts, etc.</summary>
    Flag,

    // --- DHCP (structured) ---
    /// <summary>dhcp-range=... → <see cref="DhcpRangeOption"/> (complex)</summary>
    DhcpRange,
    /// <summary>dhcp-host=... → <see cref="DhcpHostEntry"/> (already have parser)</summary>
    DhcpHost,
    /// <summary>dhcp-option=... (complex)</summary>
    DhcpOption,

    // --- DNS (structured) ---
    /// <summary>server=/domain/ip or server=ip → server model</summary>
    Server,
    /// <summary>local=/domain/</summary>
    Local,
    /// <summary>address=/domain/ip</summary>
    Address,
}
