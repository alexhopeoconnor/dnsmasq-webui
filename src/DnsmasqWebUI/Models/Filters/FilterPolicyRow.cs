using DnsmasqWebUI.Models.Dnsmasq.EffectiveConfig;

namespace DnsmasqWebUI.Models.Filters;

public sealed record FilterPolicyRow(
    string Id,
    string OccurrenceId,
    FilterPolicyCategory Category,
    FilterPolicyKind Kind,
    string Title,
    string Summary,
    string RawValue,
    bool IsEditable,
    bool IsActive,
    ConfigValueSource? Source,
    string? SourcePath,
    string? SourceLabel,
    bool IsDraftOnly,
    IReadOnlyDictionary<string, string> Facets);
