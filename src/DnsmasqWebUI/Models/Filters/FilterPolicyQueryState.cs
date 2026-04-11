namespace DnsmasqWebUI.Models.Filters;

public sealed record FilterPolicyQueryState(
    string Search,
    FilterPolicyCategory? Category,
    string? SourcePathFilter,
    bool? EditableFilter,
    bool? ActiveFilter);
