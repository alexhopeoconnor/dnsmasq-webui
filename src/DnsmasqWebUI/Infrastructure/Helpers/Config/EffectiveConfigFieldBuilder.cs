using DnsmasqWebUI.Models.Dnsmasq;
using DnsmasqWebUI.Models.Dnsmasq.EffectiveConfig;
using DnsmasqWebUI.Infrastructure.Services.Abstractions;

namespace DnsmasqWebUI.Infrastructure.Helpers.Config;

/// <summary>
/// Builds field descriptors for viewing and editing. Mapping is done once here via delegates passed to each descriptor.
/// </summary>
public static class EffectiveConfigFieldBuilder
{
    private static EffectiveDnsmasqConfig? Config(DnsmasqServiceStatus? s) => s?.EffectiveConfig;
    private static EffectiveConfigSources? Sources(DnsmasqServiceStatus? s) => s?.EffectiveConfigSources;

    /// <summary>Builds getItems from config/source list selectors; used by AddDescriptor for multi-value fields that have both config and source lists.</summary>
    private static Func<DnsmasqServiceStatus?, IReadOnlyList<ValueWithSource>?> Items(
        Func<EffectiveDnsmasqConfig?, IReadOnlyList<string>?> getValues,
        Func<EffectiveConfigSources?, IReadOnlyList<ValueWithSource>?> getWithSource)
    {
        return s =>
        {
            var values = getValues(Config(s));
            var withSource = getWithSource(Sources(s));
            if (values == null || values.Count == 0) return null;
            return withSource != null && withSource.Count == values.Count
                ? withSource
                : values.Select(v => new ValueWithSource(v, null)).ToList();
        };
    }

    /// <summary>Section IDs for use by registry/views. Prefer EffectiveConfigSections for section list and option→section mapping.</summary>
    public static string SectionHosts => EffectiveConfigSections.SectionHosts;
    public static string SectionResolver => EffectiveConfigSections.SectionResolver;
    public static string SectionDnsRecords => EffectiveConfigSections.SectionDnsRecords;
    public static string SectionDhcp => EffectiveConfigSections.SectionDhcp;
    public static string SectionTftpPxe => EffectiveConfigSections.SectionTftpPxe;
    public static string SectionDnssec => EffectiveConfigSections.SectionDnssec;
    public static string SectionCache => EffectiveConfigSections.SectionCache;
    public static string SectionProcess => EffectiveConfigSections.SectionProcess;

    public static IReadOnlyList<EffectiveConfigFieldDescriptor> BuildFieldDescriptors(DnsmasqServiceStatus? status, IEffectiveConfigRenderFragmentRegistry? registry = null)
    {
        if (status == null || status.EffectiveConfig == null)
            return Array.Empty<EffectiveConfigFieldDescriptor>();

        var list = new List<EffectiveConfigFieldDescriptor>();

        // Hosts
        list.AddDescriptor(registry, DnsmasqConfKeys.NoHosts, status, s => Config(s)?.NoHosts, s => Sources(s)?.NoHosts, null);
        list.AddDescriptor(registry, DnsmasqConfKeys.AddnHosts, status, null, null, s => ToValueWithSourceList(s?.AddnHostsPaths, Sources(s)?.AddnHostsPaths));
        list.AddDescriptor(registry, DnsmasqConfKeys.Hostsdir, status, s => Config(s)?.HostsdirPath, s => Sources(s)?.HostsdirPath, null);
        list.AddDescriptor(registry, DnsmasqConfKeys.ReadEthers, status, s => Config(s)?.ReadEthers, s => Sources(s)?.ReadEthers, null);

        // Resolver / DNS
        list.AddDescriptor(registry, DnsmasqOptionTooltips.ServerLocalLabel, status, null, null, Items(ec => ec?.ServerLocalValues, src => src?.ServerLocalValues));
        list.AddDescriptor(registry, DnsmasqConfKeys.RevServer, status, null, null, Items(ec => ec?.RevServerValues, src => src?.RevServerValues));
        list.AddDescriptor(registry, DnsmasqConfKeys.Address, status, null, null, Items(ec => ec?.AddressValues, src => src?.AddressValues));
        list.AddDescriptor(registry, DnsmasqConfKeys.ResolvFile, status, null, null, Items(ec => ec?.ResolvFiles, src => src?.ResolvFiles));
        list.AddDescriptor(registry, DnsmasqConfKeys.NoResolv, status, s => Config(s)?.NoResolv, s => Sources(s)?.NoResolv, null);
        list.AddDescriptor(registry, DnsmasqConfKeys.DomainNeeded, status, s => Config(s)?.DomainNeeded, s => Sources(s)?.DomainNeeded, null);
        list.AddDescriptor(registry, DnsmasqConfKeys.Port, status, s => Config(s)?.Port, s => Sources(s)?.Port, null);
        list.AddDescriptor(registry, DnsmasqConfKeys.LogQueries, status, s => Config(s)?.LogQueries, s => Sources(s)?.LogQueries, null);
        list.AddDescriptor(registry, DnsmasqConfKeys.StrictOrder, status, s => Config(s)?.StrictOrder, s => Sources(s)?.StrictOrder, null);
        list.AddDescriptor(registry, DnsmasqConfKeys.AllServers, status, s => Config(s)?.AllServers, s => Sources(s)?.AllServers, null);
        list.AddDescriptor(registry, DnsmasqConfKeys.AuthTtl, status, s => Config(s)?.AuthTtl, s => Sources(s)?.AuthTtl, null);
        list.AddDescriptor(registry, DnsmasqConfKeys.EdnsPacketMax, status, s => Config(s)?.EdnsPacketMax, s => Sources(s)?.EdnsPacketMax, null);
        list.AddDescriptor(registry, DnsmasqConfKeys.QueryPort, status, s => Config(s)?.QueryPort, s => Sources(s)?.QueryPort, null);
        list.AddDescriptor(registry, DnsmasqConfKeys.PortLimit, status, s => Config(s)?.PortLimit, s => Sources(s)?.PortLimit, null);
        list.AddDescriptor(registry, DnsmasqConfKeys.MinPort, status, s => Config(s)?.MinPort, s => Sources(s)?.MinPort, null);
        list.AddDescriptor(registry, DnsmasqConfKeys.MaxPort, status, s => Config(s)?.MaxPort, s => Sources(s)?.MaxPort, null);
        list.AddDescriptor(registry, DnsmasqConfKeys.DnsLoopDetect, status, s => Config(s)?.DnsLoopDetect, s => Sources(s)?.DnsLoopDetect, null);
        list.AddDescriptor(registry, DnsmasqConfKeys.StopDnsRebind, status, s => Config(s)?.StopDnsRebind, s => Sources(s)?.StopDnsRebind, null);
        list.AddDescriptor(registry, DnsmasqConfKeys.RebindLocalhostOk, status, s => Config(s)?.RebindLocalhostOk, s => Sources(s)?.RebindLocalhostOk, null);
        list.AddDescriptor(registry, DnsmasqConfKeys.ClearOnReload, status, s => Config(s)?.ClearOnReload, s => Sources(s)?.ClearOnReload, null);
        list.AddDescriptor(registry, DnsmasqConfKeys.ExpandHosts, status, s => Config(s)?.ExpandHosts, s => Sources(s)?.ExpandHosts, null);
        list.AddDescriptor(registry, DnsmasqConfKeys.BogusPriv, status, s => Config(s)?.BogusPriv, s => Sources(s)?.BogusPriv, null);
        list.AddDescriptor(registry, DnsmasqConfKeys.RebindDomainOk, status, null, null, Items(ec => ec?.RebindDomainOkValues, src => src?.RebindDomainOkValues));
        list.AddDescriptor(registry, DnsmasqConfKeys.BogusNxdomain, status, null, null, Items(ec => ec?.BogusNxdomainValues, src => src?.BogusNxdomainValues));
        list.AddDescriptor(registry, DnsmasqConfKeys.IgnoreAddress, status, null, null, Items(ec => ec?.IgnoreAddressValues, src => src?.IgnoreAddressValues));
        list.AddDescriptor(registry, DnsmasqConfKeys.Alias, status, null, null, Items(ec => ec?.AliasValues, src => src?.AliasValues));
        list.AddDescriptor(registry, DnsmasqConfKeys.FilterRr, status, null, null, Items(ec => ec?.FilterRrValues, src => src?.FilterRrValues));
        list.AddDescriptor(registry, DnsmasqConfKeys.Filterwin2k, status, s => Config(s)?.Filterwin2k, s => Sources(s)?.Filterwin2k, null);
        list.AddDescriptor(registry, DnsmasqConfKeys.FilterA, status, s => Config(s)?.FilterA, s => Sources(s)?.FilterA, null);
        list.AddDescriptor(registry, DnsmasqConfKeys.FilterAaaa, status, s => Config(s)?.FilterAaaa, s => Sources(s)?.FilterAaaa, null);
        list.AddDescriptor(registry, DnsmasqConfKeys.LocaliseQueries, status, s => Config(s)?.LocaliseQueries, s => Sources(s)?.LocaliseQueries, null);
        list.AddDescriptor(registry, DnsmasqConfKeys.FastDnsRetry, status, s => Config(s)?.FastDnsRetry, s => Sources(s)?.FastDnsRetry, null);
        list.AddDescriptor(registry, DnsmasqConfKeys.Ipset, status, null, null, Items(ec => ec?.IpsetValues, src => src?.IpsetValues));
        list.AddDescriptor(registry, DnsmasqConfKeys.Nftset, status, null, null, Items(ec => ec?.NftsetValues, src => src?.NftsetValues));

        // DNS records
        list.AddDescriptor(registry, DnsmasqConfKeys.Domain, status, null, null, Items(ec => ec?.DomainValues, src => src?.DomainValues));
        list.AddDescriptor(registry, DnsmasqConfKeys.Cname, status, null, null, Items(ec => ec?.CnameValues, src => src?.CnameValues));
        list.AddDescriptor(registry, DnsmasqConfKeys.MxHost, status, null, null, Items(ec => ec?.MxHostValues, src => src?.MxHostValues));
        list.AddDescriptor(registry, DnsmasqConfKeys.Srv, status, null, null, Items(ec => ec?.SrvValues, src => src?.SrvValues));
        list.AddDescriptor(registry, DnsmasqConfKeys.PtrRecord, status, null, null, Items(ec => ec?.PtrRecordValues, src => src?.PtrRecordValues));
        list.AddDescriptor(registry, DnsmasqConfKeys.TxtRecord, status, null, null, Items(ec => ec?.TxtRecordValues, src => src?.TxtRecordValues));
        list.AddDescriptor(registry, DnsmasqConfKeys.NaptrRecord, status, null, null, Items(ec => ec?.NaptrRecordValues, src => src?.NaptrRecordValues));
        list.AddDescriptor(registry, DnsmasqConfKeys.HostRecord, status, null, null, Items(ec => ec?.HostRecordValues, src => src?.HostRecordValues));
        list.AddDescriptor(registry, DnsmasqConfKeys.DynamicHost, status, null, null, Items(ec => ec?.DynamicHostValues, src => src?.DynamicHostValues));
        list.AddDescriptor(registry, DnsmasqConfKeys.InterfaceName, status, null, null, Items(ec => ec?.InterfaceNameValues, src => src?.InterfaceNameValues));
        list.AddDescriptor(registry, DnsmasqConfKeys.MxTarget, status, s => Config(s)?.MxTarget, s => Sources(s)?.MxTarget, null);
        list.AddDescriptor(registry, DnsmasqConfKeys.Localmx, status, s => Config(s)?.Localmx, s => Sources(s)?.Localmx, null);
        list.AddDescriptor(registry, DnsmasqConfKeys.Selfmx, status, s => Config(s)?.Selfmx, s => Sources(s)?.Selfmx, null);

        // DHCP
        list.AddDescriptor(registry, DnsmasqConfKeys.DhcpAuthoritative, status, s => Config(s)?.DhcpAuthoritative, s => Sources(s)?.DhcpAuthoritative, null);
        list.AddDescriptor(registry, DnsmasqConfKeys.DhcpRapidCommit, status, s => Config(s)?.DhcpRapidCommit, s => Sources(s)?.DhcpRapidCommit, null);
        list.AddDescriptor(registry, DnsmasqConfKeys.LeasefileRo, status, s => Config(s)?.LeasefileRo, s => Sources(s)?.LeasefileRo, null);
        list.AddDescriptor(registry, DnsmasqConfKeys.DhcpScript, status, s => Config(s)?.DhcpScriptPath, s => Sources(s)?.DhcpScriptPath, null);
        list.AddDescriptor(registry, DnsmasqConfKeys.DhcpLeasefile, status, s => Config(s)?.DhcpLeaseFilePath, s => Sources(s)?.DhcpLeaseFilePath, null);
        list.AddDescriptor(registry, DnsmasqConfKeys.DhcpLeaseMax, status, s => Config(s)?.DhcpLeaseMax, s => Sources(s)?.DhcpLeaseMax, null);
        list.AddDescriptor(registry, DnsmasqConfKeys.DhcpTtl, status, s => Config(s)?.DhcpTtl, s => Sources(s)?.DhcpTtl, null);
        list.AddDescriptor(registry, DnsmasqConfKeys.DhcpRange, status, null, null, Items(ec => ec?.DhcpRanges, src => src?.DhcpRanges));
        list.AddDescriptor(registry, DnsmasqConfKeys.DhcpHost, status, null, null, Items(ec => ec?.DhcpHostLines, src => src?.DhcpHostLines));
        list.AddDescriptor(registry, DnsmasqConfKeys.DhcpOption, status, null, null, Items(ec => ec?.DhcpOptionLines, src => src?.DhcpOptionLines));
        list.AddDescriptor(registry, DnsmasqConfKeys.DhcpOptionForce, status, null, null, Items(ec => ec?.DhcpOptionForceLines, src => src?.DhcpOptionForceLines));
        list.AddDescriptor(registry, DnsmasqConfKeys.DhcpMatch, status, null, null, Items(ec => ec?.DhcpMatchValues, src => src?.DhcpMatchValues));
        list.AddDescriptor(registry, DnsmasqConfKeys.DhcpMac, status, null, null, Items(ec => ec?.DhcpMacValues, src => src?.DhcpMacValues));
        list.AddDescriptor(registry, DnsmasqConfKeys.DhcpNameMatch, status, null, null, Items(ec => ec?.DhcpNameMatchValues, src => src?.DhcpNameMatchValues));
        list.AddDescriptor(registry, DnsmasqConfKeys.DhcpIgnoreNames, status, null, null, Items(ec => ec?.DhcpIgnoreNamesValues, src => src?.DhcpIgnoreNamesValues));
        list.AddDescriptor(registry, DnsmasqConfKeys.DhcpHostsfile, status, null, null, Items(ec => ec?.DhcpHostsfilePaths, src => src?.DhcpHostsfilePaths));
        list.AddDescriptor(registry, DnsmasqConfKeys.DhcpOptsfile, status, null, null, Items(ec => ec?.DhcpOptsfilePaths, src => src?.DhcpOptsfilePaths));
        list.AddDescriptor(registry, DnsmasqConfKeys.DhcpHostsdir, status, null, null, Items(ec => ec?.DhcpHostsdirPaths, src => src?.DhcpHostsdirPaths));
        list.AddDescriptor(registry, DnsmasqConfKeys.DhcpBoot, status, null, null, Items(ec => ec?.DhcpBootValues, src => src?.DhcpBootValues));
        list.AddDescriptor(registry, DnsmasqConfKeys.DhcpIgnore, status, null, null, Items(ec => ec?.DhcpIgnoreValues, src => src?.DhcpIgnoreValues));
        list.AddDescriptor(registry, DnsmasqConfKeys.DhcpVendorclass, status, null, null, Items(ec => ec?.DhcpVendorclassValues, src => src?.DhcpVendorclassValues));
        list.AddDescriptor(registry, DnsmasqConfKeys.DhcpUserclass, status, null, null, Items(ec => ec?.DhcpUserclassValues, src => src?.DhcpUserclassValues));
        list.AddDescriptor(registry, DnsmasqConfKeys.RaParam, status, null, null, Items(ec => ec?.RaParamValues, src => src?.RaParamValues));
        list.AddDescriptor(registry, DnsmasqConfKeys.Slaac, status, null, null, Items(ec => ec?.SlaacValues, src => src?.SlaacValues));

        // TFTP / PXE
        list.AddDescriptor(registry, DnsmasqConfKeys.EnableTftp, status, s => Config(s)?.EnableTftp, s => Sources(s)?.EnableTftp, null);
        list.AddDescriptor(registry, DnsmasqConfKeys.TftpSecure, status, s => Config(s)?.TftpSecure, s => Sources(s)?.TftpSecure, null);
        list.AddDescriptor(registry, DnsmasqConfKeys.TftpNoFail, status, s => Config(s)?.TftpNoFail, s => Sources(s)?.TftpNoFail, null);
        list.AddDescriptor(registry, DnsmasqConfKeys.TftpNoBlocksize, status, s => Config(s)?.TftpNoBlocksize, s => Sources(s)?.TftpNoBlocksize, null);
        list.AddDescriptor(registry, DnsmasqConfKeys.TftpRoot, status, s => Config(s)?.TftpRootPath, s => Sources(s)?.TftpRootPath, null);
        list.AddDescriptor(registry, DnsmasqConfKeys.PxePrompt, status, s => Config(s)?.PxePrompt, s => Sources(s)?.PxePrompt, null);
        list.AddDescriptor(registry, DnsmasqConfKeys.PxeService, status, null, null, Items(ec => ec?.PxeServiceValues, src => src?.PxeServiceValues));

        // DNSSEC
        list.AddDescriptor(registry, DnsmasqConfKeys.Dnssec, status, s => Config(s)?.Dnssec, s => Sources(s)?.Dnssec, null);
        list.AddDescriptor(registry, DnsmasqConfKeys.DnssecCheckUnsigned, status, s => Config(s)?.DnssecCheckUnsigned, s => Sources(s)?.DnssecCheckUnsigned, null);
        list.AddDescriptor(registry, DnsmasqConfKeys.ProxyDnssec, status, s => Config(s)?.ProxyDnssec, s => Sources(s)?.ProxyDnssec, null);
        list.AddDescriptor(registry, DnsmasqConfKeys.TrustAnchor, status, null, null, Items(ec => ec?.TrustAnchorValues, src => src?.TrustAnchorValues));

        // Cache
        list.AddDescriptor(registry, DnsmasqConfKeys.CacheRr, status, null, null, Items(ec => ec?.CacheRrValues, src => src?.CacheRrValues));
        list.AddDescriptor(registry, DnsmasqConfKeys.CacheSize, status, s => Config(s)?.CacheSize, s => Sources(s)?.CacheSize, null);
        list.AddDescriptor(registry, DnsmasqConfKeys.LocalTtl, status, s => Config(s)?.LocalTtl, s => Sources(s)?.LocalTtl, null);
        list.AddDescriptor(registry, DnsmasqConfKeys.NoNegcache, status, s => Config(s)?.NoNegcache, s => Sources(s)?.NoNegcache, null);
        list.AddDescriptor(registry, DnsmasqConfKeys.NegTtl, status, s => Config(s)?.NegTtl, s => Sources(s)?.NegTtl, null);
        list.AddDescriptor(registry, DnsmasqConfKeys.MaxTtl, status, s => Config(s)?.MaxTtl, s => Sources(s)?.MaxTtl, null);
        list.AddDescriptor(registry, DnsmasqConfKeys.MaxCacheTtl, status, s => Config(s)?.MaxCacheTtl, s => Sources(s)?.MaxCacheTtl, null);
        list.AddDescriptor(registry, DnsmasqConfKeys.MinCacheTtl, status, s => Config(s)?.MinCacheTtl, s => Sources(s)?.MinCacheTtl, null);

        // Process & networking
        list.AddDescriptor(registry, DnsmasqConfKeys.NoPoll, status, s => Config(s)?.NoPoll, s => Sources(s)?.NoPoll, null);
        list.AddDescriptor(registry, DnsmasqConfKeys.BindInterfaces, status, s => Config(s)?.BindInterfaces, s => Sources(s)?.BindInterfaces, null);
        list.AddDescriptor(registry, DnsmasqConfKeys.BindDynamic, status, s => Config(s)?.BindDynamic, s => Sources(s)?.BindDynamic, null);
        list.AddDescriptor(registry, DnsmasqConfKeys.LogDebug, status, s => Config(s)?.LogDebug, s => Sources(s)?.LogDebug, null);
        list.AddDescriptor(registry, DnsmasqConfKeys.LogAsync, status, s => Config(s)?.LogAsync, s => Sources(s)?.LogAsync, null);
        list.AddDescriptor(registry, DnsmasqConfKeys.LocalService, status, s => Config(s)?.LocalService, s => Sources(s)?.LocalService, null);
        list.AddDescriptor(registry, DnsmasqConfKeys.Interface, status, null, null, Items(ec => ec?.Interfaces, src => src?.Interfaces));
        list.AddDescriptor(registry, DnsmasqConfKeys.ListenAddress, status, null, null, Items(ec => ec?.ListenAddresses, src => src?.ListenAddresses));
        list.AddDescriptor(registry, DnsmasqConfKeys.ExceptInterface, status, null, null, Items(ec => ec?.ExceptInterfaces, src => src?.ExceptInterfaces));
        list.AddDescriptor(registry, DnsmasqConfKeys.AuthServer, status, null, null, Items(ec => ec?.AuthServerValues, src => src?.AuthServerValues));
        list.AddDescriptor(registry, DnsmasqConfKeys.NoDhcpInterface, status, null, null, Items(ec => ec?.NoDhcpInterfaceValues, src => src?.NoDhcpInterfaceValues));
        list.AddDescriptor(registry, DnsmasqConfKeys.NoDhcpv4Interface, status, null, null, Items(ec => ec?.NoDhcpv4InterfaceValues, src => src?.NoDhcpv4InterfaceValues));
        list.AddDescriptor(registry, DnsmasqConfKeys.NoDhcpv6Interface, status, null, null, Items(ec => ec?.NoDhcpv6InterfaceValues, src => src?.NoDhcpv6InterfaceValues));
        list.AddDescriptor(registry, DnsmasqConfKeys.PidFile, status, s => Config(s)?.PidFilePath, s => Sources(s)?.PidFilePath, null);
        list.AddDescriptor(registry, DnsmasqConfKeys.User, status, s => Config(s)?.User, s => Sources(s)?.User, null);
        list.AddDescriptor(registry, DnsmasqConfKeys.Group, status, s => Config(s)?.Group, s => Sources(s)?.Group, null);
        list.AddDescriptor(registry, DnsmasqConfKeys.LogFacility, status, s => Config(s)?.LogFacility, s => Sources(s)?.LogFacility, null);
        list.AddDescriptor(registry, DnsmasqConfKeys.EnableDbus, status, s => Config(s)?.EnableDbus, s => Sources(s)?.EnableDbus, null);
        list.AddDescriptor(registry, DnsmasqConfKeys.EnableUbus, status, s => Config(s)?.EnableUbus, s => Sources(s)?.EnableUbus, null);
        list.AddDescriptor(registry, DnsmasqConfKeys.EnableRa, status, s => Config(s)?.EnableRa, s => Sources(s)?.EnableRa, null);
        list.AddDescriptor(registry, DnsmasqConfKeys.LogDhcp, status, s => Config(s)?.LogDhcp, s => Sources(s)?.LogDhcp, null);
        list.AddDescriptor(registry, DnsmasqConfKeys.KeepInForeground, status, s => Config(s)?.KeepInForeground, s => Sources(s)?.KeepInForeground, null);
        list.AddDescriptor(registry, DnsmasqConfKeys.NoDaemon, status, s => Config(s)?.NoDaemon, s => Sources(s)?.NoDaemon, null);
        list.AddDescriptor(registry, DnsmasqConfKeys.Conntrack, status, s => Config(s)?.Conntrack, s => Sources(s)?.Conntrack, null);

        return list;
    }

    /// <summary>Creates a single-value descriptor; uses registry factory when registered (e.g. integer), otherwise a plain descriptor. Used by <see cref="EffectiveConfigFieldDescriptorListExtensions.AddSingleDescriptor"/>.</summary>
    public static EffectiveConfigFieldDescriptor GetSingleDescriptor(
        IEffectiveConfigRenderFragmentRegistry? registry,
        string optionName,
        DnsmasqServiceStatus? status,
        Func<DnsmasqServiceStatus?, object?>? getValue,
        Func<DnsmasqServiceStatus?, ConfigValueSource?>? getSource,
        Func<DnsmasqServiceStatus?, IReadOnlyList<ValueWithSource>?>? getItems)
    {
        var sectionId = EffectiveConfigSections.GetSectionId(optionName);
        var factory = registry?.GetDescriptorFactory(sectionId, optionName);
        if (factory != null)
            return (EffectiveConfigFieldDescriptor)factory(sectionId, optionName, status, getValue, getSource, getItems);
        return Single(optionName, status, getValue, getSource, getItems);
    }

    private static EffectiveConfigFieldDescriptor Single(string optionName, DnsmasqServiceStatus? status,
        Func<DnsmasqServiceStatus?, object?>? getValue,
        Func<DnsmasqServiceStatus?, ConfigValueSource?>? getSource,
        Func<DnsmasqServiceStatus?, IReadOnlyList<ValueWithSource>?>? getItems)
    {
        var sectionId = EffectiveConfigSections.GetSectionId(optionName);
        return new EffectiveConfigFieldDescriptor(sectionId, optionName, false, status, getValue, getSource, getItems);
    }

    private static IReadOnlyList<ValueWithSource>? ToValueWithSourceList(IReadOnlyList<string>? paths, IReadOnlyList<PathWithSource>? withSource)
    {
        if (paths == null || paths.Count == 0) return null;
        return withSource != null && withSource.Count == paths.Count
            ? withSource.Select(p => new ValueWithSource(p.Path, p.Source)).ToList()
            : paths.Select(p => new ValueWithSource(p, null)).ToList();
    }
}
