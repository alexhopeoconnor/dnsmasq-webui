using DnsmasqWebUI.Models.Dhcp;
using DnsmasqWebUI.Models.Dnsmasq;

namespace DnsmasqWebUI.Models.Dhcp.Ui;

/// <summary>Source classification for dhcp-host and related DHCP rows.</summary>
public enum DhcpSourceKind
{
    Managed,
    MainConfig,
    DhcpHostsFile,
    DhcpHostsDir,
    OtherIncluded
}

public sealed record DhcpPageQueryState(
    string Search,
    string? SourcePathFilter,
    DhcpHostSortMode Sort,
    bool Descending);

public enum DhcpHostSortMode
{
    LineNumber,
    Mac,
    Address,
    Name
}

/// <summary>Per-row conflict hints for static host table and lease integration.</summary>
public sealed record DhcpHostConflictInfo(
    bool DuplicateMac,
    bool DuplicateAddress,
    bool LeaseAddressMismatch,
    bool LeaseMacMismatch);

/// <summary>Unified dhcp-host row for the DHCP page (source-aware).</summary>
public sealed record DhcpHostPageRow(
    int EffectiveIndex,
    string ValueString,
    string RowKey,
    DhcpSourceKind SourceKind,
    string SourcePath,
    bool IsEditable,
    bool IsActive,
    DhcpHostEntry Entry,
    DhcpHostConflictInfo? Conflict,
    LeaseEntry? LinkedLease,
    bool MatchesManagedStatic);

/// <summary>Grouped static hosts by source file.</summary>
public sealed record DhcpHostPageGroup(
    string Key,
    string Title,
    string? Subtitle,
    DhcpSourceKind SourceKind,
    bool IsSourceEditable,
    bool IsActive,
    int VisibleRowCount,
    IReadOnlyList<DhcpHostPageRow> Rows);

/// <summary>Read-only summary of external include paths for DHCP.</summary>
public sealed record DhcpExternalSourcesViewModel(
    bool ReadEthers,
    IReadOnlyList<string> DhcpHostsfilePaths,
    IReadOnlyList<string> DhcpHostsdirPaths,
    IReadOnlyList<string> DhcpOptsfilePaths,
    IReadOnlyList<string> DhcpOptsdirPaths);

/// <summary>Lease row with linkage to static reservations.</summary>
public sealed record DhcpLeaseRowViewModel(
    LeaseEntry Lease,
    bool HasManagedStaticByMac,
    bool HasAnyStaticByMac,
    bool AddressMatchesStatic,
    bool InIpv4Range,
    string? RangeContext);

/// <summary>Ordered classification rule for policy UI.</summary>
public sealed record DhcpClassificationRuleRow(
    int Order,
    string OptionKey,
    string DisplayLabel,
    string RawValue,
    string? SourcePath,
    bool IsManaged);
