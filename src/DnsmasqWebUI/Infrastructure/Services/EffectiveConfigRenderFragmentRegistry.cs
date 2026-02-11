using Microsoft.AspNetCore.Components;
using DnsmasqWebUI.Infrastructure.Helpers.Config;
using DnsmasqWebUI.Models.Dnsmasq;
using DnsmasqWebUI.Models.Dnsmasq.EffectiveConfig;
using DnsmasqWebUI.Infrastructure.Services.Abstractions;
using DnsmasqWebUI.Components.Dnsmasq.EffectiveConfig.CustomDisplays;

namespace DnsmasqWebUI.Infrastructure.Services;

/// <summary>
/// Maps (sectionId, optionName) to custom effective-config value display component types and builds
/// RenderFragments that render those components with the descriptor.
/// </summary>
public class EffectiveConfigRenderFragmentRegistry : IEffectiveConfigRenderFragmentRegistry
{
    private readonly Dictionary<(string SectionId, string OptionName), Type> _displayComponents = new();

    public EffectiveConfigRenderFragmentRegistry()
    {
        // Port: single-field display (e.g. "53 (default DNS port)").
        _displayComponents[(EffectiveConfigFieldBuilder.SectionResolver, DnsmasqConfKeys.Port)] = typeof(PortValueDisplay);

        // Boolean flags: shared display ("Enabled" / "Disabled") for all flag options.
        RegisterFlag(EffectiveConfigFieldBuilder.SectionHosts, DnsmasqConfKeys.NoHosts);
        RegisterFlag(EffectiveConfigFieldBuilder.SectionResolver, DnsmasqConfKeys.ExpandHosts);
        RegisterFlag(EffectiveConfigFieldBuilder.SectionResolver, DnsmasqConfKeys.BogusPriv);
        RegisterFlag(EffectiveConfigFieldBuilder.SectionResolver, DnsmasqConfKeys.StrictOrder);
        RegisterFlag(EffectiveConfigFieldBuilder.SectionResolver, DnsmasqConfKeys.AllServers);
        RegisterFlag(EffectiveConfigFieldBuilder.SectionResolver, DnsmasqConfKeys.NoResolv);
        RegisterFlag(EffectiveConfigFieldBuilder.SectionResolver, DnsmasqConfKeys.DomainNeeded);
        RegisterFlag(EffectiveConfigFieldBuilder.SectionCache, DnsmasqConfKeys.NoNegcache);
        RegisterFlag(EffectiveConfigFieldBuilder.SectionProcess, DnsmasqConfKeys.NoPoll);
        RegisterFlag(EffectiveConfigFieldBuilder.SectionProcess, DnsmasqConfKeys.BindInterfaces);
        RegisterFlag(EffectiveConfigFieldBuilder.SectionProcess, DnsmasqConfKeys.BindDynamic);
        RegisterFlag(EffectiveConfigFieldBuilder.SectionProcess, DnsmasqConfKeys.LogDebug);
        RegisterFlag(EffectiveConfigFieldBuilder.SectionResolver, DnsmasqConfKeys.DnsLoopDetect);
        RegisterFlag(EffectiveConfigFieldBuilder.SectionResolver, DnsmasqConfKeys.StopDnsRebind);
        RegisterFlag(EffectiveConfigFieldBuilder.SectionResolver, DnsmasqConfKeys.RebindLocalhostOk);
        RegisterFlag(EffectiveConfigFieldBuilder.SectionResolver, DnsmasqConfKeys.ClearOnReload);
        RegisterFlag(EffectiveConfigFieldBuilder.SectionResolver, DnsmasqConfKeys.Filterwin2k);
        RegisterFlag(EffectiveConfigFieldBuilder.SectionResolver, DnsmasqConfKeys.FilterA);
        RegisterFlag(EffectiveConfigFieldBuilder.SectionResolver, DnsmasqConfKeys.FilterAaaa);
        RegisterFlag(EffectiveConfigFieldBuilder.SectionResolver, DnsmasqConfKeys.LocaliseQueries);
        RegisterFlag(EffectiveConfigFieldBuilder.SectionDhcp, DnsmasqConfKeys.DhcpAuthoritative);
        RegisterFlag(EffectiveConfigFieldBuilder.SectionDhcp, DnsmasqConfKeys.LeasefileRo);
    }

    private void RegisterFlag(string sectionId, string optionName)
    {
        _displayComponents[(sectionId, optionName)] = typeof(FlagValueDisplay);
    }

    /// <inheritdoc />
    public RenderFragment<EffectiveConfigFieldDescriptor>? GetDisplayFragment(string sectionId, string optionName)
    {
        if (!_displayComponents.TryGetValue((sectionId, optionName), out var componentType))
            return null;

        return descriptor => builder =>
        {
            builder.OpenComponent(0, componentType);
            builder.AddAttribute(1, "Descriptor", descriptor);
            builder.CloseComponent();
        };
    }
}
