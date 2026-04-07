namespace DnsmasqWebUI.Models.Hosts;

/// <summary>How rows are grouped on the Hosts page.</summary>
public enum HostsGroupingMode
{
    Source,
    Activity
}

/// <summary>Sort key for rows within each group.</summary>
public enum HostsSortMode
{
    Address,
    Name,
    EffectiveName,
    LineNumber
}

/// <summary>Filter and sort state for <see cref="IHostsPageProjectionService"/>.</summary>
public sealed record HostsPageQueryState(
    string Search,
    HostsGroupingMode Grouping,
    HostsSortMode Sort,
    bool Descending,
    HostsRowSourceKind? SourceKindFilter,
    /// <summary>When set, only rows whose <see cref="HostsPageRow.SourcePath"/> matches (ordinal-ignore-case).</summary>
    string? SourcePathFilter,
    bool? EditableFilter,
    bool? ActiveFilter);
