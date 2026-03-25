namespace DnsmasqWebUI.Models.Hosts;

/// <summary>
/// Source kind for a hosts row. Used to distinguish editable managed rows from read-only sources.
/// </summary>
public enum HostsRowSourceKind
{
    /// <summary>Managed hosts file (editable by the app).</summary>
    Managed,
    /// <summary>System hosts file (e.g. /etc/hosts).</summary>
    SystemHosts,
    /// <summary>Additional hosts file (addn-hosts=).</summary>
    AddnHosts,
    /// <summary>Hosts directory (hostsdir=).</summary>
    Hostsdir
}

/// <summary>
/// Unified row model for the Hosts page. Preserves source awareness while allowing a single rich table.
/// </summary>
public sealed record HostsPageRow(
    string Id,
    HostsRowSourceKind SourceKind,
    string SourcePath,
    bool IsEditable,
    bool IsActive,
    string? InactiveReason,
    string Address,
    IReadOnlyList<string> Names,
    IReadOnlyList<string> EffectiveNames,
    bool IsComment,
    int LineNumber);
