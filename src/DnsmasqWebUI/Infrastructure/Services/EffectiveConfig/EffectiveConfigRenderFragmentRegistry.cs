using Microsoft.AspNetCore.Components;
using DnsmasqWebUI.Infrastructure.Helpers.Config;
using DnsmasqWebUI.Models.Dnsmasq;
using DnsmasqWebUI.Models.Dnsmasq.EffectiveConfig;
using DnsmasqWebUI.Models.Dnsmasq.EffectiveConfig.Abstractions;
using DnsmasqWebUI.Infrastructure.Services.EffectiveConfig.Abstractions;
using DnsmasqWebUI.Infrastructure.Services.EffectiveConfig.Validation;
using DnsmasqWebUI.Components.EffectiveConfig.CustomDisplays;

namespace DnsmasqWebUI.Infrastructure.Services.EffectiveConfig;

/// <summary>
/// Maps (sectionId, optionName) to custom effective-config value display component types and descriptor factories.
/// Builds RenderFragments that render those components with the descriptor.
/// </summary>
public class EffectiveConfigRenderFragmentRegistry : IEffectiveConfigRenderFragmentRegistry
{
    private static readonly IMultiValueEditBehavior DefaultMultiBehavior = new DefaultMultiValueEditBehavior();

    private readonly Dictionary<(string SectionId, string OptionName), Type> _displayComponents = new();
    private readonly Dictionary<(string SectionId, string OptionName), EffectiveConfigDescriptorFactory> _descriptorFactories = new();
    private readonly Dictionary<(string SectionId, string OptionName), Type> _multiDisplayComponents = new();
    private readonly Dictionary<(string SectionId, string OptionName), EffectiveConfigMultiDescriptorFactory> _multiDescriptorFactories = new();

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
        RegisterInteger(EffectiveConfigFieldBuilder.SectionResolver, DnsmasqConfKeys.DnsForwardMax, min: 1);
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
        RegisterFlag(EffectiveConfigFieldBuilder.SectionProcess, DnsmasqConfKeys.Conntrack);
        RegisterFlag(EffectiveConfigFieldBuilder.SectionDnssec, DnsmasqConfKeys.ProxyDnssec);
        RegisterFlag(EffectiveConfigFieldBuilder.SectionResolver, DnsmasqConfKeys.ConnmarkAllowlistEnable);
        RegisterFlag(EffectiveConfigFieldBuilder.SectionResolver, DnsmasqConfKeys.NoRoundRobin);
        RegisterFlag(EffectiveConfigFieldBuilder.SectionDnssec, DnsmasqConfKeys.DnssecNoTimecheck);
        RegisterFlag(EffectiveConfigFieldBuilder.SectionDnssec, DnsmasqConfKeys.DnssecDebug);
        RegisterFlag(EffectiveConfigFieldBuilder.SectionDhcp, DnsmasqConfKeys.Leasequery);
        RegisterFlag(EffectiveConfigFieldBuilder.SectionDhcp, DnsmasqConfKeys.DhcpGenerateNames);
        RegisterFlag(EffectiveConfigFieldBuilder.SectionDhcp, DnsmasqConfKeys.DhcpBroadcast);
        RegisterFlag(EffectiveConfigFieldBuilder.SectionDhcp, DnsmasqConfKeys.DhcpSequentialIp);
        RegisterFlag(EffectiveConfigFieldBuilder.SectionDhcp, DnsmasqConfKeys.DhcpIgnoreClid);
        RegisterFlag(EffectiveConfigFieldBuilder.SectionDhcp, DnsmasqConfKeys.BootpDynamic);
        RegisterFlag(EffectiveConfigFieldBuilder.SectionDhcp, DnsmasqConfKeys.NoPing);
        RegisterFlag(EffectiveConfigFieldBuilder.SectionDhcp, DnsmasqConfKeys.ScriptArp);
        RegisterFlag(EffectiveConfigFieldBuilder.SectionDhcp, DnsmasqConfKeys.ScriptOnRenewal);
        RegisterFlag(EffectiveConfigFieldBuilder.SectionDhcp, DnsmasqConfKeys.DhcpNoOverride);
        RegisterFlag(EffectiveConfigFieldBuilder.SectionProcess, DnsmasqConfKeys.QuietDhcp);
        RegisterFlag(EffectiveConfigFieldBuilder.SectionProcess, DnsmasqConfKeys.QuietDhcp6);
        RegisterFlag(EffectiveConfigFieldBuilder.SectionProcess, DnsmasqConfKeys.QuietRa);
        RegisterFlag(EffectiveConfigFieldBuilder.SectionProcess, DnsmasqConfKeys.QuietTftp);
        RegisterFlag(EffectiveConfigFieldBuilder.SectionCache, DnsmasqConfKeys.StripMac);
        RegisterFlag(EffectiveConfigFieldBuilder.SectionCache, DnsmasqConfKeys.StripSubnet);

        // do-0x20-encode / no-0x20-encode: tri-state dropdown (Default / Enabled / Disabled).
        RegisterComponent(EffectiveConfigFieldBuilder.SectionResolver, DnsmasqConfKeys.Do0x20Encode, typeof(Do0x20EncodeDisplay));

        // Key-only or key=value options: checkbox (On) + optional value input; with semantic validators.
        RegisterComponent(EffectiveConfigFieldBuilder.SectionCache, DnsmasqConfKeys.UseStaleCache, typeof(KeyOnlyOrValueDisplay));
        RegisterValidatedSingle(EffectiveConfigFieldBuilder.SectionCache, DnsmasqConfKeys.UseStaleCache, SpecialOptionValidators.ValidateUseStaleCache);
        RegisterComponent(EffectiveConfigFieldBuilder.SectionCache, DnsmasqConfKeys.AddMac, typeof(KeyOnlyOrValueDisplay));
        RegisterValidatedSingle(EffectiveConfigFieldBuilder.SectionCache, DnsmasqConfKeys.AddMac, SpecialOptionValidators.ValidateAddMac);
        RegisterComponent(EffectiveConfigFieldBuilder.SectionCache, DnsmasqConfKeys.AddSubnet, typeof(KeyOnlyOrValueDisplay));
        RegisterValidatedSingle(EffectiveConfigFieldBuilder.SectionCache, DnsmasqConfKeys.AddSubnet, SpecialOptionValidators.ValidateAddSubnet);
        RegisterComponent(EffectiveConfigFieldBuilder.SectionCache, DnsmasqConfKeys.Umbrella, typeof(KeyOnlyOrValueDisplay));
        RegisterValidatedSingle(EffectiveConfigFieldBuilder.SectionCache, DnsmasqConfKeys.Umbrella, SpecialOptionValidators.ValidateUmbrella);

        // log-queries: dropdown (Off / On / extra / proto / auth).
        RegisterComponent(EffectiveConfigFieldBuilder.SectionResolver, DnsmasqConfKeys.LogQueries, typeof(LogQueriesDisplay));

        // local-service: dropdown (not set / net / host).
        RegisterComponent(EffectiveConfigFieldBuilder.SectionProcess, DnsmasqConfKeys.LocalService, typeof(LocalServiceDisplay));

        // Multi-value: server — descriptor only (behavior + validator); no custom display.
        RegisterMultiDescriptor(
            EffectiveConfigFieldBuilder.SectionResolver,
            DnsmasqConfKeys.Server,
            behavior: new ServerMultiBehavior(),
            validator: new ServerMultiValidator());

        RegisterMultiDescriptor(
            EffectiveConfigFieldBuilder.SectionProcess,
            DnsmasqConfKeys.ListenAddress,
            validator: new ListenAddressMultiValidator());

        RegisterMultiDescriptor(EffectiveConfigFieldBuilder.SectionHosts, DnsmasqConfKeys.AddnHosts);
        RegisterMultiDescriptor(EffectiveConfigFieldBuilder.SectionResolver, DnsmasqConfKeys.Local);
        RegisterMultiDescriptor(EffectiveConfigFieldBuilder.SectionResolver, DnsmasqConfKeys.RevServer);
        RegisterMultiDescriptor(EffectiveConfigFieldBuilder.SectionResolver, DnsmasqConfKeys.Address);
        RegisterMultiDescriptor(EffectiveConfigFieldBuilder.SectionResolver, DnsmasqConfKeys.ResolvFile);
        RegisterMultiDescriptor(EffectiveConfigFieldBuilder.SectionResolver, DnsmasqConfKeys.RebindDomainOk);
        RegisterMultiDescriptor(EffectiveConfigFieldBuilder.SectionResolver, DnsmasqConfKeys.BogusNxdomain);
        RegisterMultiDescriptor(EffectiveConfigFieldBuilder.SectionResolver, DnsmasqConfKeys.IgnoreAddress);
        RegisterMultiDescriptor(EffectiveConfigFieldBuilder.SectionResolver, DnsmasqConfKeys.Alias);
        RegisterMultiDescriptor(EffectiveConfigFieldBuilder.SectionResolver, DnsmasqConfKeys.FilterRr);
        RegisterMultiDescriptor(EffectiveConfigFieldBuilder.SectionResolver, DnsmasqConfKeys.Ipset);
        RegisterMultiDescriptor(EffectiveConfigFieldBuilder.SectionResolver, DnsmasqConfKeys.Nftset);
        RegisterMultiDescriptor(EffectiveConfigFieldBuilder.SectionResolver, DnsmasqConfKeys.ConnmarkAllowlist);
        RegisterMultiDescriptor(EffectiveConfigFieldBuilder.SectionDnsRecords, DnsmasqConfKeys.Domain);
        RegisterMultiDescriptor(EffectiveConfigFieldBuilder.SectionDnsRecords, DnsmasqConfKeys.Cname);
        RegisterMultiDescriptor(EffectiveConfigFieldBuilder.SectionDnsRecords, DnsmasqConfKeys.MxHost);
        RegisterMultiDescriptor(EffectiveConfigFieldBuilder.SectionDnsRecords, DnsmasqConfKeys.Srv);
        RegisterMultiDescriptor(EffectiveConfigFieldBuilder.SectionDnsRecords, DnsmasqConfKeys.PtrRecord);
        RegisterMultiDescriptor(EffectiveConfigFieldBuilder.SectionDnsRecords, DnsmasqConfKeys.TxtRecord);
        RegisterMultiDescriptor(EffectiveConfigFieldBuilder.SectionDnsRecords, DnsmasqConfKeys.NaptrRecord);
        RegisterMultiDescriptor(EffectiveConfigFieldBuilder.SectionDnsRecords, DnsmasqConfKeys.HostRecord);
        RegisterMultiDescriptor(EffectiveConfigFieldBuilder.SectionDnsRecords, DnsmasqConfKeys.DynamicHost);
        RegisterMultiDescriptor(EffectiveConfigFieldBuilder.SectionDnsRecords, DnsmasqConfKeys.InterfaceName);
        RegisterMultiDescriptor(EffectiveConfigFieldBuilder.SectionDnsRecords, DnsmasqConfKeys.CaaRecord);
        RegisterMultiDescriptor(EffectiveConfigFieldBuilder.SectionDnsRecords, DnsmasqConfKeys.DnsRr);
        RegisterMultiDescriptor(EffectiveConfigFieldBuilder.SectionDnsRecords, DnsmasqConfKeys.SynthDomain);
        RegisterMultiDescriptor(EffectiveConfigFieldBuilder.SectionDnsRecords, DnsmasqConfKeys.AuthZone);
        RegisterMultiDescriptor(EffectiveConfigFieldBuilder.SectionDnsRecords, DnsmasqConfKeys.AuthSoa);
        RegisterMultiDescriptor(EffectiveConfigFieldBuilder.SectionDnsRecords, DnsmasqConfKeys.AuthSecServers);
        RegisterMultiDescriptor(EffectiveConfigFieldBuilder.SectionDnsRecords, DnsmasqConfKeys.AuthPeer);
        RegisterMultiDescriptor(EffectiveConfigFieldBuilder.SectionDhcp, DnsmasqConfKeys.DhcpRange);
        RegisterMultiDescriptor(EffectiveConfigFieldBuilder.SectionDhcp, DnsmasqConfKeys.DhcpHost);
        RegisterMultiDescriptor(EffectiveConfigFieldBuilder.SectionDhcp, DnsmasqConfKeys.DhcpOption);
        RegisterMultiDescriptor(EffectiveConfigFieldBuilder.SectionDhcp, DnsmasqConfKeys.DhcpOptionForce);
        RegisterMultiDescriptor(EffectiveConfigFieldBuilder.SectionDhcp, DnsmasqConfKeys.DhcpMatch);
        RegisterMultiDescriptor(EffectiveConfigFieldBuilder.SectionDhcp, DnsmasqConfKeys.DhcpMac);
        RegisterMultiDescriptor(EffectiveConfigFieldBuilder.SectionDhcp, DnsmasqConfKeys.DhcpNameMatch);
        RegisterMultiDescriptor(EffectiveConfigFieldBuilder.SectionDhcp, DnsmasqConfKeys.DhcpIgnoreNames);
        RegisterMultiDescriptor(EffectiveConfigFieldBuilder.SectionDhcp, DnsmasqConfKeys.DhcpHostsfile);
        RegisterMultiDescriptor(EffectiveConfigFieldBuilder.SectionDhcp, DnsmasqConfKeys.DhcpOptsfile);
        RegisterMultiDescriptor(EffectiveConfigFieldBuilder.SectionDhcp, DnsmasqConfKeys.DhcpHostsdir);
        RegisterMultiDescriptor(EffectiveConfigFieldBuilder.SectionDhcp, DnsmasqConfKeys.DhcpRelay);
        RegisterMultiDescriptor(EffectiveConfigFieldBuilder.SectionDhcp, DnsmasqConfKeys.DhcpCircuitid);
        RegisterMultiDescriptor(EffectiveConfigFieldBuilder.SectionDhcp, DnsmasqConfKeys.DhcpRemoteid);
        RegisterMultiDescriptor(EffectiveConfigFieldBuilder.SectionDhcp, DnsmasqConfKeys.DhcpSubscrid);
        RegisterMultiDescriptor(EffectiveConfigFieldBuilder.SectionDhcp, DnsmasqConfKeys.DhcpProxy);
        RegisterMultiDescriptor(EffectiveConfigFieldBuilder.SectionDhcp, DnsmasqConfKeys.TagIf);
        RegisterMultiDescriptor(EffectiveConfigFieldBuilder.SectionDhcp, DnsmasqConfKeys.BridgeInterface);
        RegisterMultiDescriptor(EffectiveConfigFieldBuilder.SectionDhcp, DnsmasqConfKeys.SharedNetwork);
        RegisterMultiDescriptor(EffectiveConfigFieldBuilder.SectionDhcp, DnsmasqConfKeys.DhcpBoot);
        RegisterMultiDescriptor(EffectiveConfigFieldBuilder.SectionDhcp, DnsmasqConfKeys.DhcpIgnore);
        RegisterMultiDescriptor(EffectiveConfigFieldBuilder.SectionDhcp, DnsmasqConfKeys.DhcpVendorclass);
        RegisterMultiDescriptor(EffectiveConfigFieldBuilder.SectionDhcp, DnsmasqConfKeys.DhcpUserclass);
        RegisterMultiDescriptor(EffectiveConfigFieldBuilder.SectionDhcp, DnsmasqConfKeys.RaParam);
        RegisterMultiDescriptor(EffectiveConfigFieldBuilder.SectionDhcp, DnsmasqConfKeys.Slaac);
        RegisterMultiDescriptor(EffectiveConfigFieldBuilder.SectionTftpPxe, DnsmasqConfKeys.PxeService);
        RegisterMultiDescriptor(EffectiveConfigFieldBuilder.SectionTftpPxe, DnsmasqConfKeys.DhcpOptionPxe);
        RegisterMultiDescriptor(EffectiveConfigFieldBuilder.SectionDnssec, DnsmasqConfKeys.TrustAnchor);
        RegisterMultiDescriptor(EffectiveConfigFieldBuilder.SectionCache, DnsmasqConfKeys.CacheRr);
        RegisterMultiDescriptor(EffectiveConfigFieldBuilder.SectionProcess, DnsmasqConfKeys.Interface);
        RegisterMultiDescriptor(EffectiveConfigFieldBuilder.SectionProcess, DnsmasqConfKeys.ExceptInterface);
        RegisterMultiDescriptor(EffectiveConfigFieldBuilder.SectionProcess, DnsmasqConfKeys.AuthServer);
        RegisterMultiDescriptor(EffectiveConfigFieldBuilder.SectionProcess, DnsmasqConfKeys.NoDhcpInterface);
        RegisterMultiDescriptor(EffectiveConfigFieldBuilder.SectionProcess, DnsmasqConfKeys.NoDhcpv4Interface);
        RegisterMultiDescriptor(EffectiveConfigFieldBuilder.SectionProcess, DnsmasqConfKeys.NoDhcpv6Interface);
    }

    private void RegisterMultiDescriptor(
        string sectionId,
        string optionName,
        IMultiValueEditBehavior? behavior = null,
        IMultiValueOptionValidator? validator = null)
    {
        _multiDescriptorFactories[(sectionId, optionName)] =
            (sid, name, status, getItems) => new EffectiveMultiValueConfigFieldDescriptor(
                sid,
                name,
                status,
                getItems,
                behavior ?? DefaultMultiBehavior,
                validator);
    }

    private void RegisterMultiComponent(string sectionId, string optionName, Type componentType)
    {
        _multiDisplayComponents[(sectionId, optionName)] = componentType;
    }

    private void RegisterMulti(
        string sectionId,
        string optionName,
        Type componentType,
        IMultiValueEditBehavior? behavior = null,
        IMultiValueOptionValidator? validator = null)
    {
        RegisterMultiDescriptor(sectionId, optionName, behavior, validator);
        RegisterMultiComponent(sectionId, optionName, componentType);
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
            new EffectiveIntegerConfigFieldDescriptor(sectionId, optionName, status, getValue, getSource, getItems, viewSuffix, unit, min, max, defaultValue);
    }

    private void RegisterValidatedSingle(string sectionId, string optionName, EffectiveConfigSingleValueValidator validator)
    {
        _descriptorFactories[(sectionId, optionName)] =
            (sid, name, status, getValue, getSource, getItems) => new EffectiveConfigFieldDescriptor(
                sid,
                name,
                false,
                status,
                getValue,
                getSource,
                getItems,
                validator);
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

    /// <inheritdoc />
    public RenderFragment<EffectiveConfigFieldDescriptor>? BuildMultiFieldComponentFragment(string sectionId, string optionName, EventCallback<IReadOnlyList<string>> onValuesChanged)
    {
        if (!_multiDisplayComponents.TryGetValue((sectionId, optionName), out var componentType))
            return null;

        return descriptor => builder =>
        {
            builder.OpenComponent(0, componentType);
            builder.AddAttribute(1, "Descriptor", descriptor);
            builder.AddAttribute(2, "OnValuesChanged", onValuesChanged);
            builder.CloseComponent();
        };
    }

    /// <inheritdoc />
    public EffectiveConfigMultiDescriptorFactory? GetMultiDescriptorFactory(string sectionId, string optionName)
    {
        return _multiDescriptorFactories.TryGetValue((sectionId, optionName), out var factory) ? factory : null;
    }
}
