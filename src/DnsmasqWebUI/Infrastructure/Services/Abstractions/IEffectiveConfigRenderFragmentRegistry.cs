using Microsoft.AspNetCore.Components;
using DnsmasqWebUI.Models.Dnsmasq;
using DnsmasqWebUI.Models.Dnsmasq.EffectiveConfig;
using DnsmasqWebUI.Models.Dnsmasq.EffectiveConfig.Abstractions;

namespace DnsmasqWebUI.Infrastructure.Services.Abstractions;

/// <summary>
/// Lookup for custom display fragments per effective-config field (section + option).
/// When non-null, the fragment renders the value portion of the row; label and source badge stay shared.
/// Registered as singleton via assembly scanning (<see cref="IApplicationSingleton"/>).
/// </summary>
public interface IEffectiveConfigRenderFragmentRegistry : IApplicationSingleton
{
    /// <summary>
    /// Builds a fragment that renders the custom value component for this field, or null to use default rendering.
    /// </summary>
    RenderFragment<EffectiveConfigFieldDescriptor>? BuildFieldComponentFragment(string sectionId, string optionName);

    /// <summary>
    /// Builds a fragment that renders the custom value component and wires the given callback so the row can receive value changes (for commit-on-blur).
    /// </summary>
    RenderFragment<EffectiveConfigFieldDescriptor>? BuildFieldComponentFragment(string sectionId, string optionName, EventCallback<object?> onValueChanged);

    /// <summary>
    /// Returns a factory that creates the correct descriptor type for this field (e.g. <see cref="EffectiveIntegerConfigFieldDescriptor"/>), or null to use the default.
    /// </summary>
    EffectiveConfigDescriptorFactory? GetDescriptorFactory(string sectionId, string optionName);
}
