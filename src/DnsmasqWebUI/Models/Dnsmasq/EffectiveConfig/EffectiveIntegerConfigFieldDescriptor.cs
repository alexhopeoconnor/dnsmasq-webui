using DnsmasqWebUI.Models.Dnsmasq;

namespace DnsmasqWebUI.Models.Dnsmasq.EffectiveConfig;

/// <summary>
/// Descriptor for integer single-value options. Extends the base descriptor with display/validation metadata for number inputs.
/// </summary>
public record EffectiveIntegerConfigFieldDescriptor(
    string SectionId,
    string OptionName,
    DnsmasqServiceStatus? Status,
    Func<DnsmasqServiceStatus?, object?>? ResolveValue,
    Func<DnsmasqServiceStatus?, ConfigValueSource?>? ResolveSource,
    Func<DnsmasqServiceStatus?, IReadOnlyList<ValueWithSource>?>? ResolveItems,
    string? ViewSuffix = null,
    string? Unit = null,
    int Min = 0,
    int Max = int.MaxValue,
    int? DefaultValue = null
) : EffectiveConfigFieldDescriptor(
    SectionId,
    OptionName,
    false,
    Status,
    ResolveValue,
    ResolveSource,
    ResolveItems,
    value =>
    {
        if (value is null) return null;
        if (value is int n && (n < Min || n > Max))
            return $"Value must be between {Min} and {Max}.";
        if (value is int)
            return null;
        return $"Value must be a whole number between {Min} and {Max}.";
    });
