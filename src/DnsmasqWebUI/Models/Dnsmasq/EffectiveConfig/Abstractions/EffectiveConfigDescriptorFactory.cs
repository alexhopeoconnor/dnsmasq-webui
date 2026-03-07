using DnsmasqWebUI.Models.Dnsmasq;
using DnsmasqWebUI.Models.Dnsmasq.EffectiveConfig;

namespace DnsmasqWebUI.Models.Dnsmasq.EffectiveConfig.Abstractions;

/// <summary>
/// Creates an <see cref="IEffectiveConfigFieldDescriptor"/> for a single-value field given section, option, status, and resolve delegates.
/// Used by the registry so the builder can request the correct concrete descriptor type per registration (e.g. integer options).
/// </summary>
public delegate IEffectiveConfigFieldDescriptor EffectiveConfigDescriptorFactory(
    string sectionId,
    string optionName,
    DnsmasqServiceStatus? status,
    Func<DnsmasqServiceStatus?, object?>? getValue,
    Func<DnsmasqServiceStatus?, ConfigValueSource?>? getSource,
    Func<DnsmasqServiceStatus?, IReadOnlyList<ValueWithSource>?>? getItems);

/// <summary>
/// Creates an <see cref="EffectiveMultiValueConfigFieldDescriptor"/> for a multi-value field (section, option, status, getItems only).
/// Used by the registry so the builder can create multi descriptors with behavior/validator.
/// </summary>
public delegate EffectiveMultiValueConfigFieldDescriptor EffectiveConfigMultiDescriptorFactory(
    string sectionId,
    string optionName,
    DnsmasqServiceStatus? status,
    Func<DnsmasqServiceStatus?, IReadOnlyList<ValueWithSource>?>? getItems);
