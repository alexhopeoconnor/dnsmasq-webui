using Microsoft.AspNetCore.Components;
using DnsmasqWebUI.Models.Dnsmasq;
using DnsmasqWebUI.Models.Dnsmasq.EffectiveConfig;

namespace DnsmasqWebUI.Infrastructure.Services.Abstractions;

/// <summary>
/// Lookup for custom display fragments per effective-config field (section + option).
/// When non-null, the fragment renders the value portion of the row; label and source badge stay shared.
/// Registered as singleton via assembly scanning (<see cref="IApplicationSingleton"/>).
/// </summary>
public interface IEffectiveConfigRenderFragmentRegistry : IApplicationSingleton
{
    /// <summary>
    /// Returns a fragment that renders the custom value UI for this field, or null to use default rendering.
    /// </summary>
    RenderFragment<EffectiveConfigFieldDescriptor>? GetDisplayFragment(string sectionId, string optionName);
}
