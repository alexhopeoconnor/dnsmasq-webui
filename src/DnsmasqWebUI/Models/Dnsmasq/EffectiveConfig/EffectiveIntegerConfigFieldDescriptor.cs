using DnsmasqWebUI.Models.Dnsmasq;

namespace DnsmasqWebUI.Models.Dnsmasq.EffectiveConfig;

/// <summary>
/// Descriptor for integer single-value options. Extends the base descriptor with display/validation metadata for number inputs.
/// </summary>
public record EffectiveIntegerConfigFieldDescriptor(
    string SectionId,
    string OptionName,
    bool IsMultiValue,
    DnsmasqServiceStatus? Status,
    Func<DnsmasqServiceStatus?, object?>? ResolveValue,
    Func<DnsmasqServiceStatus?, ConfigValueSource?>? ResolveSource,
    Func<DnsmasqServiceStatus?, IReadOnlyList<ValueWithSource>?>? ResolveItems,
    string? ViewSuffix = null,
    string? Unit = null,
    int Min = 0,
    int Max = int.MaxValue,
    int? DefaultValue = null
) : EffectiveConfigFieldDescriptor(SectionId, OptionName, IsMultiValue, Status, ResolveValue, ResolveSource, ResolveItems);
