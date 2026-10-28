namespace DnsmasqWebUI.Models.Hosts;

/// <summary>
/// One hosts source resolved from the effective dnsmasq configuration.
/// A configured directory is represented as a directory source and each discovered member is represented
/// separately, so callers can distinguish an empty directory from a missing or unreadable one.
/// </summary>
/// <param name="SourceKind">How dnsmasq reaches this source.</param>
/// <param name="Path">Absolute path of this source or directory member.</param>
/// <param name="ConfiguredPath">The configured <c>addn-hosts</c> or <c>hostsdir</c> path. Equals <paramref name="Path"/> for direct file sources.</param>
/// <param name="IsDirectory">Whether this item is a configured directory rather than a file member.</param>
/// <param name="IsActive">Whether dnsmasq will use this source under the current effective configuration.</param>
/// <param name="InactiveReason">Why a configured source is inactive, when applicable.</param>
/// <param name="LoadOrder">Best available source order for display and diagnostics.</param>
/// <param name="Diagnostic">Missing/unreadable source diagnostic, when available.</param>
/// <param name="EntryCount">Number of parsed entries for file sources. Directory sources have zero.</param>
public sealed record HostSource(
    HostsRowSourceKind SourceKind,
    string Path,
    string ConfiguredPath,
    bool IsDirectory,
    bool IsActive,
    string? InactiveReason,
    int LoadOrder,
    string? Diagnostic,
    int EntryCount);
