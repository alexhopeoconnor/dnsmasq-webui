namespace DnsmasqWebUI.Models.Filters;

public sealed record FilterPolicyGroup(
    FilterPolicyCategory Category,
    string Title,
    IReadOnlyList<FilterPolicyRow> Rows);
