namespace DnsmasqWebUI.Models.Hosts;

/// <summary>
/// A logical group of <see cref="HostsPageRow"/> for the Hosts page (by source file or by active/inactive).
/// </summary>
public sealed record HostsPageGroup(
    string Key,
    string Title,
    string? Subtitle,
    HostsRowSourceKind? SourceKind,
    bool GroupContainsEditableRows,
    bool IsActive,
    string? InactiveReason,
    int VisibleRowCount,
    IReadOnlyList<HostsPageRow> Rows);
