using Microsoft.AspNetCore.Components;
using DnsmasqWebUI.Infrastructure.Services.EffectiveConfig.Editing;
using DnsmasqWebUI.Infrastructure.Services.EffectiveConfig.Editing.Abstractions;
using DnsmasqWebUI.Infrastructure.Services.EffectiveConfig.Metadata;
using DnsmasqWebUI.Infrastructure.Services.EffectiveConfig.Rendering.Abstractions;
using DnsmasqWebUI.Infrastructure.Services.EffectiveConfig.Validation;
using DnsmasqWebUI.Infrastructure.Services.EffectiveConfig.Validation.Abstractions;
using DnsmasqWebUI.Models.Dnsmasq;
using DnsmasqWebUI.Models.Dnsmasq.EffectiveConfig;
using DnsmasqWebUI.Models.Dnsmasq.EffectiveConfig.Abstractions;
using DnsmasqWebUI.Components.EffectiveConfig.CustomDisplays;

namespace DnsmasqWebUI.Infrastructure.Services.EffectiveConfig.Rendering;

/// <summary>
/// Maps (sectionId, optionName) to custom effective-config value display component types and descriptor factories.
/// Builds RenderFragments that render those components with the descriptor.
/// </summary>
public class EffectiveConfigRenderFragmentRegistry : IEffectiveConfigRenderFragmentRegistry
{
    private static readonly IMultiValueEditBehavior DefaultMultiBehavior = new DefaultMultiValueEditBehavior();

    private readonly IOptionSemanticValidator _semanticValidator;
    private readonly Dictionary<(string SectionId, string OptionName), Type> _displayComponents = new();
    private readonly Dictionary<(string SectionId, string OptionName), EffectiveConfigDescriptorFactory> _descriptorFactories = new();
    private readonly Dictionary<(string SectionId, string OptionName), Type> _multiDisplayComponents = new();
    private readonly Dictionary<(string SectionId, string OptionName), EffectiveConfigMultiDescriptorFactory> _multiDescriptorFactories = new();

    public EffectiveConfigRenderFragmentRegistry(IOptionSemanticValidator semanticValidator)
    {
        _semanticValidator = semanticValidator;
        RegisterAll();
    }

    private void RegisterAll()
    {
        RegisterCustomSingles();
        RegisterIntegerDescriptors();
        RegisterFlagDisplays();
        RegisterKeyOnlyOrValueDisplays();
        RegisterPathValidatedSingles();
        RegisterMultiValueDescriptors();
    }

    private void RegisterCustomSingles()
    {
        RegisterComponent(EffectiveConfigSections.SectionResolver, DnsmasqConfKeys.Port, typeof(PortValueDisplay));
        RegisterComponent(EffectiveConfigSections.SectionResolver, DnsmasqConfKeys.Do0x20Encode, typeof(Do0x20EncodeDisplay));
        RegisterComponent(EffectiveConfigSections.SectionResolver, DnsmasqConfKeys.LogQueries, typeof(LogQueriesDisplay));
        RegisterComponent(EffectiveConfigSections.SectionProcess, DnsmasqConfKeys.LocalService, typeof(LocalServiceDisplay));
    }

    private void RegisterIntegerDescriptors()
    {
        RegisterInteger(EffectiveConfigSections.SectionResolver, DnsmasqConfKeys.AuthTtl, unit: "seconds");
        RegisterInteger(EffectiveConfigSections.SectionResolver, DnsmasqConfKeys.EdnsPacketMax, max: 65535);
        RegisterInteger(EffectiveConfigSections.SectionResolver, DnsmasqConfKeys.QueryPort, max: 65535, defaultValue: 0);
        RegisterInteger(EffectiveConfigSections.SectionResolver, DnsmasqConfKeys.PortLimit, max: 65535);
        RegisterInteger(EffectiveConfigSections.SectionResolver, DnsmasqConfKeys.MinPort, min: 1, max: 65535);
        RegisterInteger(EffectiveConfigSections.SectionResolver, DnsmasqConfKeys.MaxPort, min: 1, max: 65535);
        RegisterInteger(EffectiveConfigSections.SectionResolver, DnsmasqConfKeys.DnsForwardMax, min: 1);
        RegisterInteger(EffectiveConfigSections.SectionCache, DnsmasqConfKeys.CacheSize, min: 0);
        RegisterInteger(EffectiveConfigSections.SectionCache, DnsmasqConfKeys.LocalTtl, unit: "seconds");
        RegisterInteger(EffectiveConfigSections.SectionCache, DnsmasqConfKeys.NegTtl, unit: "seconds");
        RegisterInteger(EffectiveConfigSections.SectionCache, DnsmasqConfKeys.MaxTtl, unit: "seconds");
        RegisterInteger(EffectiveConfigSections.SectionCache, DnsmasqConfKeys.MaxCacheTtl, unit: "seconds");
        RegisterInteger(EffectiveConfigSections.SectionCache, DnsmasqConfKeys.MinCacheTtl, unit: "seconds");
        RegisterInteger(EffectiveConfigSections.SectionDhcp, DnsmasqConfKeys.DhcpLeaseMax, min: 0);
        RegisterInteger(EffectiveConfigSections.SectionDhcp, DnsmasqConfKeys.DhcpTtl);
    }

    private void RegisterFlagDisplays()
    {
        RegisterFlags(EffectiveConfigSections.SectionHosts, DnsmasqConfKeys.NoHosts, DnsmasqConfKeys.ExpandHosts);
        RegisterFlags(
            EffectiveConfigSections.SectionResolver,
            DnsmasqConfKeys.BogusPriv,
            DnsmasqConfKeys.StrictOrder,
            DnsmasqConfKeys.AllServers,
            DnsmasqConfKeys.NoResolv,
            DnsmasqConfKeys.DomainNeeded,
            DnsmasqConfKeys.DnsLoopDetect,
            DnsmasqConfKeys.StopDnsRebind,
            DnsmasqConfKeys.RebindLocalhostOk,
            DnsmasqConfKeys.ClearOnReload,
            DnsmasqConfKeys.Filterwin2k,
            DnsmasqConfKeys.FilterA,
            DnsmasqConfKeys.FilterAaaa,
            DnsmasqConfKeys.LocaliseQueries,
            DnsmasqConfKeys.NoRoundRobin);
        RegisterFlags(
            EffectiveConfigSections.SectionDhcp,
            DnsmasqConfKeys.EnableRa,
            DnsmasqConfKeys.DhcpAuthoritative,
            DnsmasqConfKeys.ReadEthers,
            DnsmasqConfKeys.DhcpRapidCommit,
            DnsmasqConfKeys.LeasefileRo,
            DnsmasqConfKeys.DhcpSequentialIp,
            DnsmasqConfKeys.DhcpIgnoreClid,
            DnsmasqConfKeys.NoPing,
            DnsmasqConfKeys.ScriptArp,
            DnsmasqConfKeys.ScriptOnRenewal,
            DnsmasqConfKeys.DhcpNoOverride);
        RegisterFlags(
            EffectiveConfigSections.SectionTftpPxe,
            DnsmasqConfKeys.EnableTftp,
            DnsmasqConfKeys.TftpSecure,
            DnsmasqConfKeys.TftpNoFail,
            DnsmasqConfKeys.TftpNoBlocksize);
        RegisterFlags(EffectiveConfigSections.SectionDnsRecords, DnsmasqConfKeys.Localmx, DnsmasqConfKeys.Selfmx);
        RegisterFlags(
            EffectiveConfigSections.SectionDnssec,
            DnsmasqConfKeys.Dnssec,
            DnsmasqConfKeys.ProxyDnssec,
            DnsmasqConfKeys.DnssecNoTimecheck,
            DnsmasqConfKeys.DnssecDebug);
        RegisterFlags(
            EffectiveConfigSections.SectionCache,
            DnsmasqConfKeys.NoNegcache,
            DnsmasqConfKeys.StripMac,
            DnsmasqConfKeys.StripSubnet);
        RegisterFlags(
            EffectiveConfigSections.SectionProcess,
            DnsmasqConfKeys.NoPoll,
            DnsmasqConfKeys.BindInterfaces,
            DnsmasqConfKeys.BindDynamic,
            DnsmasqConfKeys.LogDebug,
            DnsmasqConfKeys.LogDhcp,
            DnsmasqConfKeys.KeepInForeground,
            DnsmasqConfKeys.NoDaemon,
            DnsmasqConfKeys.Conntrack,
            DnsmasqConfKeys.QuietDhcp,
            DnsmasqConfKeys.QuietDhcp6,
            DnsmasqConfKeys.QuietRa,
            DnsmasqConfKeys.QuietTftp);
    }

    private void RegisterKeyOnlyOrValueDisplays()
    {
        RegisterSemanticSingleComponent(
            EffectiveConfigSections.SectionCache,
            typeof(KeyOnlyOrValueDisplay),
            DnsmasqConfKeys.UseStaleCache,
            DnsmasqConfKeys.AddMac,
            DnsmasqConfKeys.AddSubnet,
            DnsmasqConfKeys.Umbrella);
        RegisterSemanticSingleComponent(
            EffectiveConfigSections.SectionResolver,
            typeof(KeyOnlyOrValueDisplay),
            DnsmasqConfKeys.ConnmarkAllowlistEnable);
        RegisterSemanticSingleComponent(
            EffectiveConfigSections.SectionDnssec,
            typeof(KeyOnlyOrValueDisplay),
            DnsmasqConfKeys.DnssecCheckUnsigned);
        RegisterComponents(
            EffectiveConfigSections.SectionDhcp,
            typeof(KeyOnlyOrValueDisplay),
            DnsmasqConfKeys.DhcpGenerateNames,
            DnsmasqConfKeys.DhcpBroadcast,
            DnsmasqConfKeys.BootpDynamic);
    }

    private void RegisterPathValidatedSingles()
    {
        RegisterSemanticSingles(
            EffectiveConfigSections.SectionHosts,
            DnsmasqConfKeys.Hostsdir);
        RegisterSemanticSingles(
            EffectiveConfigSections.SectionDhcp,
            DnsmasqConfKeys.DhcpLeasefile,
            DnsmasqConfKeys.DhcpScript,
            DnsmasqConfKeys.DhcpLuascript);
        RegisterSemanticSingles(
            EffectiveConfigSections.SectionTftpPxe,
            DnsmasqConfKeys.TftpRoot);
        RegisterSemanticSingles(
            EffectiveConfigSections.SectionDnssec,
            DnsmasqConfKeys.DnssecTimestamp);
        RegisterSemanticSingles(
            EffectiveConfigSections.SectionProcess,
            DnsmasqConfKeys.Dumpfile,
            DnsmasqConfKeys.PidFile);
    }

    private void RegisterMultiValueDescriptors()
    {
        RegisterSemanticMultiDescriptor(
            EffectiveConfigSections.SectionResolver,
            DnsmasqConfKeys.Server,
            behavior: new DistinctMultiValueEditBehavior());
        RegisterSemanticMultiDescriptor(EffectiveConfigSections.SectionProcess, DnsmasqConfKeys.ListenAddress);

        RegisterSemanticMultis(
            EffectiveConfigSections.SectionHosts,
            DnsmasqConfKeys.AddnHosts,
            DnsmasqConfKeys.Domain);
        RegisterSemanticMultis(
            EffectiveConfigSections.SectionResolver,
            DnsmasqConfKeys.Local,
            DnsmasqConfKeys.ResolvFile,
            DnsmasqConfKeys.RevServer,
            DnsmasqConfKeys.Address,
            DnsmasqConfKeys.RebindDomainOk,
            DnsmasqConfKeys.BogusNxdomain,
            DnsmasqConfKeys.IgnoreAddress,
            DnsmasqConfKeys.Alias,
            DnsmasqConfKeys.Ipset,
            DnsmasqConfKeys.Nftset,
            DnsmasqConfKeys.ConnmarkAllowlist);
        RegisterSemanticMultis(
            EffectiveConfigSections.SectionDhcp,
            DnsmasqConfKeys.NoDhcpInterface,
            DnsmasqConfKeys.NoDhcpv4Interface,
            DnsmasqConfKeys.NoDhcpv6Interface,
            DnsmasqConfKeys.DhcpRange,
            DnsmasqConfKeys.DhcpHost,
            DnsmasqConfKeys.DhcpOption,
            DnsmasqConfKeys.DhcpOptionForce,
            DnsmasqConfKeys.DhcpMatch,
            DnsmasqConfKeys.DhcpMac,
            DnsmasqConfKeys.DhcpIgnoreNames,
            DnsmasqConfKeys.DhcpNameMatch,
            DnsmasqConfKeys.DhcpHostsfile,
            DnsmasqConfKeys.DhcpOptsfile,
            DnsmasqConfKeys.DhcpHostsdir,
            DnsmasqConfKeys.DhcpOptsdir,
            DnsmasqConfKeys.Leasequery,
            DnsmasqConfKeys.DhcpRelay,
            DnsmasqConfKeys.DhcpProxy,
            DnsmasqConfKeys.RaParam,
            DnsmasqConfKeys.TagIf,
            DnsmasqConfKeys.BridgeInterface,
            DnsmasqConfKeys.SharedNetwork,
            DnsmasqConfKeys.DhcpBoot,
            DnsmasqConfKeys.DhcpIgnore,
            DnsmasqConfKeys.DhcpVendorclass,
            DnsmasqConfKeys.DhcpUserclass,
            DnsmasqConfKeys.Slaac);

        RegisterSemanticMultis(
            EffectiveConfigSections.SectionResolver,
            DnsmasqConfKeys.FilterRr);
        RegisterSemanticMultis(
            EffectiveConfigSections.SectionDnsRecords,
            DnsmasqConfKeys.Cname,
            DnsmasqConfKeys.MxHost,
            DnsmasqConfKeys.PtrRecord,
            DnsmasqConfKeys.InterfaceName,
            DnsmasqConfKeys.CaaRecord,
            DnsmasqConfKeys.Srv,
            DnsmasqConfKeys.NaptrRecord,
            DnsmasqConfKeys.TxtRecord,
            DnsmasqConfKeys.HostRecord,
            DnsmasqConfKeys.DynamicHost,
            DnsmasqConfKeys.DnsRr,
            DnsmasqConfKeys.SynthDomain,
            DnsmasqConfKeys.AuthZone,
            DnsmasqConfKeys.AuthSoa,
            DnsmasqConfKeys.AuthSecServers,
            DnsmasqConfKeys.AuthPeer);
        RegisterSemanticMultis(
            EffectiveConfigSections.SectionDhcp,
            DnsmasqConfKeys.DhcpCircuitid,
            DnsmasqConfKeys.DhcpRemoteid,
            DnsmasqConfKeys.DhcpSubscrid);
        RegisterSemanticMultis(
            EffectiveConfigSections.SectionTftpPxe,
            DnsmasqConfKeys.PxeService);
        RegisterSemanticMultis(
            EffectiveConfigSections.SectionTftpPxe,
            DnsmasqConfKeys.DhcpOptionPxe);
        RegisterSemanticMultis(EffectiveConfigSections.SectionDnssec, DnsmasqConfKeys.TrustAnchor);
        RegisterSemanticMultis(EffectiveConfigSections.SectionCache, DnsmasqConfKeys.CacheRr);
        RegisterSemanticMultis(
            EffectiveConfigSections.SectionProcess,
            DnsmasqConfKeys.Interface,
            DnsmasqConfKeys.ExceptInterface,
            DnsmasqConfKeys.AuthServer);
    }

    private void RegisterComponents(string sectionId, Type componentType, params string[] optionNames)
    {
        foreach (var optionName in optionNames)
            RegisterComponent(sectionId, optionName, componentType);
    }

    private void RegisterFlags(string sectionId, params string[] optionNames)
    {
        foreach (var optionName in optionNames)
            RegisterFlag(sectionId, optionName);
    }

    private void RegisterSemanticSingles(string sectionId, params string[] optionNames)
    {
        foreach (var optionName in optionNames)
            RegisterSemanticSingleValidator(sectionId, optionName);
    }

    private void RegisterSemanticSingleComponent(string sectionId, Type componentType, params string[] optionNames)
    {
        foreach (var optionName in optionNames)
        {
            RegisterComponent(sectionId, optionName, componentType);
            RegisterSemanticSingleValidator(sectionId, optionName);
        }
    }

    private void RegisterMultis(string sectionId, params string[] optionNames)
    {
        foreach (var optionName in optionNames)
            RegisterMultiDescriptor(sectionId, optionName);
    }

    private void RegisterSemanticMultis(string sectionId, params string[] optionNames)
    {
        foreach (var optionName in optionNames)
            RegisterSemanticMultiDescriptor(sectionId, optionName);
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

    private void RegisterSemanticSingleValidator(string sectionId, string optionName)
    {
        var semantics = EffectiveConfigSpecialOptionSemantics.TryGetSemantics(optionName);
        if (semantics is null)
            return;

        RegisterValidatedSingle(
            sectionId,
            optionName,
            value => _semanticValidator.ValidateSingle(optionName, value, semantics.Validation));
    }

    private void RegisterSemanticMultiDescriptor(string sectionId, string optionName, IMultiValueEditBehavior? behavior = null)
    {
        var semantics = EffectiveConfigSpecialOptionSemantics.TryGetSemantics(optionName);
        IMultiValueOptionValidator? validator = semantics is null
            ? null
            : new DelegateMultiValueOptionValidator(v => _semanticValidator.ValidateMultiItem(optionName, v, semantics.Validation));
        RegisterMultiDescriptor(sectionId, optionName, behavior, validator);
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
