namespace DnsmasqWebUI.Models.Dnsmasq.EffectiveConfig;

/// <summary>
/// Effective dnsmasq config after reading all config files (main + conf-file + conf-dir)
/// in the order returned by <see cref="DnsmasqConfIncludeParser.GetIncludedPathsWithSource"/>.
/// Represents the final values dnsmasq uses: single-value (last wins), flags (set if any),
/// and multi-value (all occurrences in order). Source per value is in <see cref="EffectiveConfigSources"/>
/// from <see cref="IDnsmasqConfigSetService.GetEffectiveConfigWithSources"/> (file path, IsManaged).
/// Non-managed source → readonly in UI; flags set in a non-managed file cannot be unset from the UI.
///
/// Single-value (ARG_ONE): last occurrence wins; can be overridden by writing to the managed file.
/// Flags: set if any file contains the option (key-only line).
/// Multi-value (ARG_DUP): addn-hosts, server/local, address, interface, listen-address, except-interface,
/// dhcp-range, dhcp-host, dhcp-option, resolv-file — all values in order; each has source in EffectiveConfigSources.
/// </summary>
public record EffectiveDnsmasqConfig(
    // --- Hosts (already used by Hosts UI) ---
    bool NoHosts,
    IReadOnlyList<string> AddnHostsPaths,
    string? HostsdirPath,

    // --- Multi-value (ARG_DUP): all occurrences in order ---
    IReadOnlyList<string> ServerLocalValues,
    IReadOnlyList<string> RevServerValues,
    IReadOnlyList<string> AddressValues,
    IReadOnlyList<string> Interfaces,
    IReadOnlyList<string> ListenAddresses,
    IReadOnlyList<string> ExceptInterfaces,
    IReadOnlyList<string> DhcpRanges,
    IReadOnlyList<string> DhcpHostLines,
    IReadOnlyList<string> DhcpOptionLines,
    IReadOnlyList<string> DhcpMatchValues,
    IReadOnlyList<string> DhcpBootValues,
    IReadOnlyList<string> DhcpIgnoreValues,
    IReadOnlyList<string> DhcpVendorclassValues,
    IReadOnlyList<string> DhcpUserclassValues,
    IReadOnlyList<string> RaParamValues,
    IReadOnlyList<string> SlaacValues,
    IReadOnlyList<string> PxeServiceValues,
    IReadOnlyList<string> TrustAnchorValues,
    IReadOnlyList<string> ResolvFiles,
    IReadOnlyList<string> RebindDomainOkValues,
    IReadOnlyList<string> BogusNxdomainValues,
    IReadOnlyList<string> IgnoreAddressValues,
    IReadOnlyList<string> AliasValues,
    IReadOnlyList<string> FilterRrValues,
    IReadOnlyList<string> CacheRrValues,
    IReadOnlyList<string> AuthServerValues,
    IReadOnlyList<string> NoDhcpInterfaceValues,
    IReadOnlyList<string> NoDhcpv4InterfaceValues,
    IReadOnlyList<string> NoDhcpv6InterfaceValues,
    IReadOnlyList<string> DomainValues,
    IReadOnlyList<string> CnameValues,
    IReadOnlyList<string> MxHostValues,
    IReadOnlyList<string> SrvValues,
    IReadOnlyList<string> PtrRecordValues,
    IReadOnlyList<string> TxtRecordValues,
    IReadOnlyList<string> NaptrRecordValues,
    IReadOnlyList<string> HostRecordValues,
    IReadOnlyList<string> DynamicHostValues,
    IReadOnlyList<string> InterfaceNameValues,
    IReadOnlyList<string> DhcpOptionForceLines,
    IReadOnlyList<string> IpsetValues,
    IReadOnlyList<string> NftsetValues,
    IReadOnlyList<string> DhcpMacValues,
    IReadOnlyList<string> DhcpNameMatchValues,
    IReadOnlyList<string> DhcpIgnoreNamesValues,

    // --- Boolean flags (set if any file contains the option) ---
    bool ExpandHosts,
    bool BogusPriv,
    bool StrictOrder,
    bool AllServers,
    bool NoResolv,
    bool DomainNeeded,
    bool NoPoll,
    bool BindInterfaces,
    bool BindDynamic,
    bool NoNegcache,
    bool DnsLoopDetect,
    bool StopDnsRebind,
    bool RebindLocalhostOk,
    bool ClearOnReload,
    bool Filterwin2k,
    bool FilterA,
    bool FilterAaaa,
    bool LocaliseQueries,
    bool LogDebug,
    bool DhcpAuthoritative,
    bool LeasefileRo,
    bool EnableTftp,
    bool TftpSecure,
    bool TftpNoFail,
    bool TftpNoBlocksize,
    bool Dnssec,
    bool DnssecCheckUnsigned,
    bool ReadEthers,
    bool DhcpRapidCommit,
    bool Localmx,
    bool Selfmx,
    bool EnableRa,
    bool LogDhcp,

    // --- Single-value options (last occurrence wins; null = not set, dnsmasq default) ---
    string? DhcpLeaseFilePath,
    int? CacheSize,
    int? Port,
    int? LocalTtl,
    string? PidFilePath,
    string? User,
    string? Group,
    string? LogFacility,
    string? LogQueries,
    int? AuthTtl,
    int? EdnsPacketMax,
    int? QueryPort,
    int? PortLimit,
    int? MinPort,
    int? MaxPort,
    string? LogAsync,
    string? LocalService,
    int? DhcpLeaseMax,
    int? NegTtl,
    int? MaxTtl,
    int? MaxCacheTtl,
    int? MinCacheTtl,
    int? DhcpTtl,
    string? TftpRootPath,
    string? PxePrompt,
    string? EnableDbus,
    string? EnableUbus,
    string? FastDnsRetry,
    string? DhcpScriptPath,
    string? MxTarget
)
{
    /// <summary>
    /// Returns the log file path when <see cref="LogFacility"/> is a file path (e.g. starts with / or contains /).
    /// Returns null when LogFacility is not set, is a syslog facility name (e.g. local0), or is not a file path.
    /// Used for full log download in the UI.
    /// </summary>
    public static string? GetLogsPath(EffectiveDnsmasqConfig? config)
    {
        var logFacility = config?.LogFacility?.Trim();
        if (string.IsNullOrEmpty(logFacility)) return null;
        if (logFacility.StartsWith('/') || logFacility.Contains('/'))
            return logFacility;
        return null;
    }
}
