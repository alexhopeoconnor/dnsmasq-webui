using DnsmasqWebUI.Models.Dnsmasq;
using DnsmasqWebUI.Infrastructure.Services.EffectiveConfig.Validation;

namespace DnsmasqWebUI.Models.Dnsmasq.EffectiveConfig;

/// <summary>
/// Descriptor for multi-value options with option-specific edit behavior and validation.
/// Behavior and validator travel with the descriptor, same as integer metadata in <see cref="EffectiveIntegerConfigFieldDescriptor"/>.
/// </summary>
public record EffectiveMultiValueConfigFieldDescriptor(
    string SectionId,
    string OptionName,
    DnsmasqServiceStatus? Status,
    Func<DnsmasqServiceStatus?, IReadOnlyList<ValueWithSource>?>? ResolveItems,
    IMultiValueEditBehavior Behavior,
    IMultiValueOptionValidator? Validator = null
) : EffectiveConfigFieldDescriptor(
    SectionId,
    OptionName,
    true,
    Status,
    null,
    null,
    ResolveItems);
