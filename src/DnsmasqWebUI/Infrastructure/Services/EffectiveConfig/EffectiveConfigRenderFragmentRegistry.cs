using Microsoft.AspNetCore.Components;
using DnsmasqWebUI.Infrastructure.Helpers.Config;
using DnsmasqWebUI.Models.Dnsmasq;
using DnsmasqWebUI.Models.Dnsmasq.EffectiveConfig;
using DnsmasqWebUI.Models.Dnsmasq.EffectiveConfig.Abstractions;
using DnsmasqWebUI.Infrastructure.Services.EffectiveConfig.Abstractions;
using DnsmasqWebUI.Components.Dnsmasq.EffectiveConfig.CustomDisplays;

namespace DnsmasqWebUI.Infrastructure.Services.EffectiveConfig;

/// <summary>
/// Maps (sectionId, optionName) to custom effective-config value display component types and descriptor factories.
/// Builds RenderFragments that render those components with the descriptor.
/// </summary>
public class EffectiveConfigRenderFragmentRegistry : IEffectiveConfigRenderFragmentRegistry
{
    private readonly Dictionary<(string SectionId, string OptionName), Type> _displayComponents = new();
    private readonly Dictionary<(string SectionId, string OptionName), EffectiveConfigDescriptorFactory> _descriptorFactories = new();

    public EffectiveConfigRenderFragmentRegistry()
    {
        // Port: single-field display (e.g. "53 (default DNS port)").
        RegisterComponent(EffectiveConfigFieldBuilder.SectionResolver, DnsmasqConfKeys.Port, typeof(PortValueDisplay));

        // Integer single-value options: component type + factory that creates EffectiveIntegerConfigFieldDescriptor.
        RegisterInteger(EffectiveConfigFieldBuilder.SectionResolver, DnsmasqConfKeys.AuthTtl, unit: "seconds");
        RegisterInteger(EffectiveConfigFieldBuilder.SectionResolver, DnsmasqConfKeys.EdnsPacketMax, max: 65535);
        RegisterInteger(EffectiveConfigFieldBuilder.SectionResolver, DnsmasqConfKeys.QueryPort, max: 65535, defaultValue: 0);
        RegisterInteger(EffectiveConfigFieldBuilder.SectionResolver, DnsmasqConfKeys.PortLimit, max: 65535);
        RegisterInteger(EffectiveConfigFieldBuilder.SectionResolver, DnsmasqConfKeys.MinPort, min: 1, max: 65535);
        RegisterInteger(EffectiveConfigFieldBuilder.SectionResolver, DnsmasqConfKeys.MaxPort, min: 1, max: 65535);
        RegisterInteger(EffectiveConfigFieldBuilder.SectionCache, DnsmasqConfKeys.CacheSize, min: 0);
        RegisterInteger(EffectiveConfigFieldBuilder.SectionCache, DnsmasqConfKeys.LocalTtl, unit: "seconds");
        RegisterInteger(EffectiveConfigFieldBuilder.SectionCache, DnsmasqConfKeys.NegTtl, unit: "seconds");
        RegisterInteger(EffectiveConfigFieldBuilder.SectionCache, DnsmasqConfKeys.MaxTtl, unit: "seconds");
        RegisterInteger(EffectiveConfigFieldBuilder.SectionCache, DnsmasqConfKeys.MaxCacheTtl, unit: "seconds");
        RegisterInteger(EffectiveConfigFieldBuilder.SectionCache, DnsmasqConfKeys.MinCacheTtl, unit: "seconds");
        RegisterInteger(EffectiveConfigFieldBuilder.SectionDhcp, DnsmasqConfKeys.DhcpLeaseMax, min: 0);
        RegisterInteger(EffectiveConfigFieldBuilder.SectionDhcp, DnsmasqConfKeys.DhcpTtl);

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
        RegisterFlag(EffectiveConfigFieldBuilder.SectionHosts, DnsmasqConfKeys.ReadEthers);
        RegisterFlag(EffectiveConfigFieldBuilder.SectionDhcp, DnsmasqConfKeys.DhcpRapidCommit);
        RegisterFlag(EffectiveConfigFieldBuilder.SectionDhcp, DnsmasqConfKeys.LeasefileRo);
        RegisterFlag(EffectiveConfigFieldBuilder.SectionTftpPxe, DnsmasqConfKeys.EnableTftp);
        RegisterFlag(EffectiveConfigFieldBuilder.SectionTftpPxe, DnsmasqConfKeys.TftpSecure);
        RegisterFlag(EffectiveConfigFieldBuilder.SectionTftpPxe, DnsmasqConfKeys.TftpNoFail);
        RegisterFlag(EffectiveConfigFieldBuilder.SectionTftpPxe, DnsmasqConfKeys.TftpNoBlocksize);
        RegisterFlag(EffectiveConfigFieldBuilder.SectionDnsRecords, DnsmasqConfKeys.Localmx);
        RegisterFlag(EffectiveConfigFieldBuilder.SectionDnsRecords, DnsmasqConfKeys.Selfmx);
        RegisterFlag(EffectiveConfigFieldBuilder.SectionDnssec, DnsmasqConfKeys.Dnssec);
        RegisterFlag(EffectiveConfigFieldBuilder.SectionDnssec, DnsmasqConfKeys.DnssecCheckUnsigned);
        RegisterFlag(EffectiveConfigFieldBuilder.SectionProcess, DnsmasqConfKeys.EnableRa);
        RegisterFlag(EffectiveConfigFieldBuilder.SectionProcess, DnsmasqConfKeys.LogDhcp);
        RegisterFlag(EffectiveConfigFieldBuilder.SectionProcess, DnsmasqConfKeys.KeepInForeground);
        RegisterFlag(EffectiveConfigFieldBuilder.SectionProcess, DnsmasqConfKeys.NoDaemon);
        RegisterFlag(EffectiveConfigFieldBuilder.SectionDnssec, DnsmasqConfKeys.ProxyDnssec);

        // log-queries: dropdown (Off / On / extra / proto / auth).
        RegisterComponent(EffectiveConfigFieldBuilder.SectionResolver, DnsmasqConfKeys.LogQueries, typeof(LogQueriesDisplay));

        // local-service: dropdown (not set / net / host).
        RegisterComponent(EffectiveConfigFieldBuilder.SectionProcess, DnsmasqConfKeys.LocalService, typeof(LocalServiceDisplay));
    }

    private void RegisterComponent(string sectionId, string optionName, Type componentType)
    {
        _displayComponents[(sectionId, optionName)] = componentType;
    }

    private void RegisterFlag(string sectionId, string optionName)
    {
        _displayComponents[(sectionId, optionName)] = typeof(FlagValueDisplay);
    }

    private void RegisterInteger(string sectionId, string optionName,
        string? viewSuffix = null, string? unit = null, int min = 0, int max = int.MaxValue, int? defaultValue = null)
    {
        _displayComponents[(sectionId, optionName)] = typeof(IntegerValueDisplay);
        _descriptorFactories[(sectionId, optionName)] = (sectionId, optionName, status, getValue, getSource, getItems) =>
            new EffectiveIntegerConfigFieldDescriptor(sectionId, optionName, false, status, getValue, getSource, getItems, viewSuffix, unit, min, max, defaultValue);
    }

    /// <inheritdoc />
    public EffectiveConfigDescriptorFactory? GetDescriptorFactory(string sectionId, string optionName)
    {
        return _descriptorFactories.TryGetValue((sectionId, optionName), out var factory) ? factory : null;
    }

    /// <inheritdoc />
    public RenderFragment<EffectiveConfigFieldDescriptor>? BuildFieldComponentFragment(string sectionId, string optionName)
    {
        return BuildFieldComponentFragment(sectionId, optionName, default);
    }

    /// <inheritdoc />
    public RenderFragment<EffectiveConfigFieldDescriptor>? BuildFieldComponentFragment(string sectionId, string optionName, EventCallback<object?> onValueChanged)
    {
        if (!_displayComponents.TryGetValue((sectionId, optionName), out var componentType))
            return null;

        var hasCallback = onValueChanged.HasDelegate;

        return descriptor => builder =>
        {
            builder.OpenComponent(0, componentType);
            builder.AddAttribute(1, "Descriptor", descriptor);
            if (hasCallback)
                builder.AddAttribute(2, "ValueChanged", onValueChanged);
            builder.CloseComponent();
        };
    }
}
