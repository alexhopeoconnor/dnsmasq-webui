namespace DnsmasqWebUI.Models.EffectiveConfig;

/// <summary>
/// Source file for each field in <see cref="EffectiveDnsmasqConfig"/>.
/// Same property names as config: use <c>sources.Port</c> for <c>config.Port</c>, <c>sources.AddnHostsPaths[i].Source</c> for <c>config.AddnHostsPaths[i]</c>, etc.
/// Enables the UI to show exactly where each value came from and whether it is readonly.
/// </summary>
/// <remarks>
/// <para><b>Single-value and flags:</b> Property is <see cref="ConfigValueSource"/>?. Readonly = <c>source?.IsReadOnly == true</c>. Tooltip = <see cref="ConfigValueSource.GetReadOnlyTooltip"/>.</para>
/// <para><b>Multi-value:</b> Property is <c>IReadOnlyList&lt;ValueWithSource&gt;</c> or <c>PathWithSource</c> for addn-hosts. Use <c>sources.AddnHostsPaths[i].Source</c>. Same readonly/tooltip from that Source.</para>
/// </remarks>
public record EffectiveConfigSources(
    // --- Hosts ---
    ConfigValueSource? NoHosts,
    IReadOnlyList<PathWithSource> AddnHostsPaths,

    // --- Multi-value (ARG_DUP): source per value ---
    IReadOnlyList<ValueWithSource> ServerLocalValues,
    IReadOnlyList<ValueWithSource> AddressValues,
    IReadOnlyList<ValueWithSource> Interfaces,
    IReadOnlyList<ValueWithSource> ListenAddresses,
    IReadOnlyList<ValueWithSource> ExceptInterfaces,
    IReadOnlyList<ValueWithSource> DhcpRanges,
    IReadOnlyList<ValueWithSource> DhcpHostLines,
    IReadOnlyList<ValueWithSource> DhcpOptionLines,
    IReadOnlyList<ValueWithSource> ResolvFiles,

    // --- Flags (set if any occurrence; source = first file that set it, so we know if readonly) ---
    ConfigValueSource? ExpandHosts,
    ConfigValueSource? BogusPriv,
    ConfigValueSource? StrictOrder,
    ConfigValueSource? NoResolv,
    ConfigValueSource? DomainNeeded,
    ConfigValueSource? NoPoll,
    ConfigValueSource? BindInterfaces,
    ConfigValueSource? NoNegcache,
    ConfigValueSource? DhcpAuthoritative,
    ConfigValueSource? LeasefileRo,

    // --- Single-value (last occurrence wins; source = file that set the last value) ---
    ConfigValueSource? DhcpLeaseFilePath,
    ConfigValueSource? CacheSize,
    ConfigValueSource? Port,
    ConfigValueSource? LocalTtl,
    ConfigValueSource? PidFilePath,
    ConfigValueSource? User,
    ConfigValueSource? Group,
    ConfigValueSource? LogFacility,
    ConfigValueSource? DhcpLeaseMax,
    ConfigValueSource? NegTtl,
    ConfigValueSource? MaxTtl,
    ConfigValueSource? MaxCacheTtl,
    ConfigValueSource? MinCacheTtl,
    ConfigValueSource? DhcpTtl
);
