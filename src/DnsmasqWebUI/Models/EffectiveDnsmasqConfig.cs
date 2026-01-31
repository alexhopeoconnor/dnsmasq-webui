namespace DnsmasqWebUI.Models;

/// <summary>
/// Effective dnsmasq config after reading all config files (main + conf-file + conf-dir).
/// Represents the final values that dnsmasq uses. Only includes options that are
/// single-value (last wins) or boolean flags, so they can be overridden by writing
/// to our managed file. Multi-value options (e.g. addn-hosts, server=) are either
/// listed read-only or omitted from this model.
/// Based on dnsmasq option.c: ARG_ONE = last occurrence wins; flag options = set if any.
/// </summary>
public record EffectiveDnsmasqConfig(
    // --- Hosts (already used by Hosts UI) ---
    bool NoHosts,
    IReadOnlyList<string> AddnHostsPaths,

    // --- Boolean flags (set if any file contains the option) ---
    bool ExpandHosts,
    bool BogusPriv,
    bool StrictOrder,
    bool NoResolv,
    bool DomainNeeded,
    bool NoPoll,
    bool BindInterfaces,
    bool NoNegcache,
    bool DhcpAuthoritative,
    bool LeasefileRo,

    // --- Single-value options (last occurrence wins; null = not set, dnsmasq default) ---
    string? DhcpLeaseFilePath,
    int? CacheSize,
    int? Port,
    int? LocalTtl,
    string? PidFilePath,
    string? User,
    string? Group,
    string? LogFacility,
    int? DhcpLeaseMax,
    int? NegTtl,
    int? MaxTtl,
    int? MaxCacheTtl,
    int? MinCacheTtl,
    int? DhcpTtl
);
