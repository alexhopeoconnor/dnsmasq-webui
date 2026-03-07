using DnsmasqWebUI.Models.Dnsmasq;
using DnsmasqWebUI.Models.Dnsmasq.EffectiveConfig;
using DnsmasqWebUI.Infrastructure.Services.EffectiveConfig.Abstractions;

namespace DnsmasqWebUI.Infrastructure.Helpers.Config;

/// <summary>
/// Extension methods for building lists of effective-config field descriptors. Keeps the field builder readable.
/// </summary>
public static class EffectiveConfigFieldDescriptorListExtensions
{
    /// <summary>
    /// Adds a field descriptor; single vs multi is determined by <see cref="EffectiveConfigOptionKindMap"/> for the given option name.
    /// </summary>
    public static void AddDescriptor(
        this List<EffectiveConfigFieldDescriptor> list,
        IEffectiveConfigRenderFragmentRegistry? registry,
        string optionName,
        DnsmasqServiceStatus? status,
        Func<DnsmasqServiceStatus?, object?>? getValue,
        Func<DnsmasqServiceStatus?, ConfigValueSource?>? getSource,
        Func<DnsmasqServiceStatus?, IReadOnlyList<ValueWithSource>?>? getItems)
    {
        if (EffectiveConfigOptionKindMap.GetKind(optionName) == EffectiveConfigFieldKind.Multi)
            list.AddMultiDescriptor(registry, optionName, status, getItems);
        else
            list.AddSingleDescriptor(registry, optionName, status, getValue, getSource, getItems);
    }

    /// <summary>
    /// Adds a single-value field descriptor. Uses the registry's factory when registered (e.g. integer options), otherwise a plain descriptor.
    /// </summary>
    public static void AddSingleDescriptor(
        this List<EffectiveConfigFieldDescriptor> list,
        IEffectiveConfigRenderFragmentRegistry? registry,
        string optionName,
        DnsmasqServiceStatus? status,
        Func<DnsmasqServiceStatus?, object?>? getValue,
        Func<DnsmasqServiceStatus?, ConfigValueSource?>? getSource,
        Func<DnsmasqServiceStatus?, IReadOnlyList<ValueWithSource>?>? getItems)
    {
        list.Add(EffectiveConfigFieldBuilder.GetSingleDescriptor(registry, optionName, status, getValue, getSource, getItems));
    }

    /// <summary>
    /// Adds a multi-value field descriptor. Every multi option must be registered in the registry; uses the registry's multi descriptor factory.
    /// </summary>
    public static void AddMultiDescriptor(
        this List<EffectiveConfigFieldDescriptor> list,
        IEffectiveConfigRenderFragmentRegistry? registry,
        string optionName,
        DnsmasqServiceStatus? status,
        Func<DnsmasqServiceStatus?, IReadOnlyList<ValueWithSource>?>? getItems)
    {
        var sectionId = EffectiveConfigSections.GetSectionId(optionName);
        var factory = registry?.GetMultiDescriptorFactory(sectionId, optionName)
            ?? throw new InvalidOperationException($"No multi descriptor factory registered for option '{optionName}' (section '{sectionId}'). Register all multi options via RegisterMultiDescriptor.");
        list.Add(factory(sectionId, optionName, status, getItems));
    }
}
