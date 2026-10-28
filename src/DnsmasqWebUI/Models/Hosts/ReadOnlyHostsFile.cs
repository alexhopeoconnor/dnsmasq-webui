namespace DnsmasqWebUI.Models.Hosts;

/// <summary>
/// Path and parsed entries for a non-managed hosts source.
/// Metadata is retained even when a configured source is inactive, missing, or unreadable so callers do
/// not mistake an absent row for a source dnsmasq did not know about.
/// </summary>
/// <param name="Path">Absolute path of the hosts file (e.g. /etc/hosts or an addn-hosts path).</param>
/// <param name="Entries">Parsed entries from the file (IP, names, comments, passthrough).</param>
/// <param name="SourceKind">How dnsmasq reaches the file.</param>
/// <param name="IsActive">Whether dnsmasq currently uses this source.</param>
/// <param name="InactiveReason">Why the source is inactive, when applicable.</param>
/// <param name="LoadOrder">Best available load order for the source.</param>
/// <param name="ConfiguredPath">Configured path that produced this file. Differs from <paramref name="Path"/> for directory members.</param>
/// <param name="Diagnostic">Missing/unreadable source diagnostic, when available.</param>
public record ReadOnlyHostsFile(
    string Path,
    IReadOnlyList<HostEntry> Entries,
    HostsRowSourceKind SourceKind = HostsRowSourceKind.AddnHosts,
    bool IsActive = true,
    string? InactiveReason = null,
    int LoadOrder = 0,
    string? ConfiguredPath = null,
    string? Diagnostic = null);
