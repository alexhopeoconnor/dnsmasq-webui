using DnsmasqWebUI.Models.Dnsmasq.EffectiveConfig;

namespace DnsmasqWebUI.Models.Dnsmasq.EffectiveConfig.Abstractions;

/// <summary>
/// Contract for one effective-config field: metadata (section, option name) and resolution of value, source, and items at render time.
/// </summary>
public interface IEffectiveConfigFieldDescriptor
{
    string SectionId { get; }
    string OptionName { get; }
    bool IsMultiValue { get; }
    object? GetValue();
    ConfigValueSource? GetSource();
    IReadOnlyList<ValueWithSource>? GetItems();
}
