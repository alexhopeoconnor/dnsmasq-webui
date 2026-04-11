using DnsmasqWebUI.Models.Dnsmasq.EffectiveConfig;

namespace DnsmasqWebUI.Models.Filters;

public sealed record FilterPolicyRow(
    string Id,
    FilterPolicyCategory Category,
    FilterPolicyKind Kind,
    string Title,
    string Summary,
    string RawValue,
    bool IsEditable,
    bool IsActive,
    ConfigValueSource? Source,
    IReadOnlyDictionary<string, string> Facets);
