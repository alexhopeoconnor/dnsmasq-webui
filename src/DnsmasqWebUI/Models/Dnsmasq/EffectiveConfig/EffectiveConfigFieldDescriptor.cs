using DnsmasqWebUI.Models.Dnsmasq;

namespace DnsmasqWebUI.Models.Dnsmasq.EffectiveConfig;

/// <summary>
/// Glue for rendering one effective-config field: metadata (section, option name) plus delegates that resolve value, source, and items from Status at render time.
/// </summary>
public record EffectiveConfigFieldDescriptor(
    string SectionId,
    string OptionName,
    bool IsMultiValue,
    DnsmasqServiceStatus? Status,
    Func<DnsmasqServiceStatus?, object?>? ResolveValue,
    Func<DnsmasqServiceStatus?, ConfigValueSource?>? ResolveSource,
    Func<DnsmasqServiceStatus?, IReadOnlyList<ValueWithSource>?>? ResolveItems
)
{
    public object? GetValue() => ResolveValue?.Invoke(Status);
    public ConfigValueSource? GetSource() => ResolveSource?.Invoke(Status);
    public IReadOnlyList<ValueWithSource>? GetItems() => ResolveItems?.Invoke(Status);
}
