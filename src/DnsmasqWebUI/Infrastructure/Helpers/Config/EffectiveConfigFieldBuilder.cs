using DnsmasqWebUI.Models.Dnsmasq;
using DnsmasqWebUI.Models.Dnsmasq.EffectiveConfig;

namespace DnsmasqWebUI.Infrastructure.Helpers.Config;

/// <summary>
/// Builds field descriptors for viewing and editing. Mapping is done once here via delegates passed to each descriptor.
/// </summary>
public static class EffectiveConfigFieldBuilder
{
    private static EffectiveDnsmasqConfig? Config(DnsmasqServiceStatus? s) => s?.EffectiveConfig;
    private static EffectiveConfigSources? Sources(DnsmasqServiceStatus? s) => s?.EffectiveConfigSources;

    /// <summary>Builds getItems from config/source list selectors; used by Multi() for fields that have both.</summary>
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

    public static IReadOnlyList<EffectiveConfigFieldDescriptor> BuildFieldDescriptors(DnsmasqServiceStatus? status)
    {
        if (status == null || status.EffectiveConfig == null)
            return Array.Empty<EffectiveConfigFieldDescriptor>();

        var list = new List<EffectiveConfigFieldDescriptor>();

        // Hosts
        list.Add(Single(DnsmasqConfKeys.NoHosts, status, s => Config(s)?.NoHosts, s => Sources(s)?.NoHosts, null));
        list.Add(Multi(DnsmasqConfKeys.AddnHosts, status, s => ToValueWithSourceList(s?.AddnHostsPaths, Sources(s)?.AddnHostsPaths)));
        list.Add(Single(DnsmasqConfKeys.Hostsdir, status, s => Config(s)?.HostsdirPath, s => Sources(s)?.HostsdirPath, null));
        list.Add(Single(DnsmasqConfKeys.ReadEthers, status, s => Config(s)?.ReadEthers, s => Sources(s)?.ReadEthers, null));

        // Resolver / DNS
        list.Add(Multi(DnsmasqOptionTooltips.ServerLocalLabel, status, Items(ec => ec?.ServerLocalValues, src => src?.ServerLocalValues)));
        list.Add(Multi(DnsmasqConfKeys.RevServer, status, Items(ec => ec?.RevServerValues, src => src?.RevServerValues)));
        list.Add(Multi(DnsmasqConfKeys.Address, status, Items(ec => ec?.AddressValues, src => src?.AddressValues)));
        list.Add(Multi(DnsmasqConfKeys.ResolvFile, status, Items(ec => ec?.ResolvFiles, src => src?.ResolvFiles)));
        list.Add(Single(DnsmasqConfKeys.NoResolv, status, s => Config(s)?.NoResolv, s => Sources(s)?.NoResolv, null));
        list.Add(Single(DnsmasqConfKeys.DomainNeeded, status, s => Config(s)?.DomainNeeded, s => Sources(s)?.DomainNeeded, null));
        list.Add(Single(DnsmasqConfKeys.Port, status, s => Config(s)?.Port, s => Sources(s)?.Port, null));
        list.Add(Single(DnsmasqConfKeys.LogQueries, status, s => Config(s)?.LogQueries, s => Sources(s)?.LogQueries, null));
        list.Add(Single(DnsmasqConfKeys.StrictOrder, status, s => Config(s)?.StrictOrder, s => Sources(s)?.StrictOrder, null));
        list.Add(Single(DnsmasqConfKeys.AllServers, status, s => Config(s)?.AllServers, s => Sources(s)?.AllServers, null));
        list.Add(Single(DnsmasqConfKeys.AuthTtl, status, s => Config(s)?.AuthTtl, s => Sources(s)?.AuthTtl, null));
        list.Add(Single(DnsmasqConfKeys.EdnsPacketMax, status, s => Config(s)?.EdnsPacketMax, s => Sources(s)?.EdnsPacketMax, null));
        list.Add(Single(DnsmasqConfKeys.QueryPort, status, s => Config(s)?.QueryPort, s => Sources(s)?.QueryPort, null));
        list.Add(Single(DnsmasqConfKeys.PortLimit, status, s => Config(s)?.PortLimit, s => Sources(s)?.PortLimit, null));
        list.Add(Single(DnsmasqConfKeys.MinPort, status, s => Config(s)?.MinPort, s => Sources(s)?.MinPort, null));
        list.Add(Single(DnsmasqConfKeys.MaxPort, status, s => Config(s)?.MaxPort, s => Sources(s)?.MaxPort, null));
        list.Add(Single(DnsmasqConfKeys.DnsLoopDetect, status, s => Config(s)?.DnsLoopDetect, s => Sources(s)?.DnsLoopDetect, null));
        list.Add(Single(DnsmasqConfKeys.StopDnsRebind, status, s => Config(s)?.StopDnsRebind, s => Sources(s)?.StopDnsRebind, null));
        list.Add(Single(DnsmasqConfKeys.RebindLocalhostOk, status, s => Config(s)?.RebindLocalhostOk, s => Sources(s)?.RebindLocalhostOk, null));
        list.Add(Single(DnsmasqConfKeys.ClearOnReload, status, s => Config(s)?.ClearOnReload, s => Sources(s)?.ClearOnReload, null));
        list.Add(Single(DnsmasqConfKeys.ExpandHosts, status, s => Config(s)?.ExpandHosts, s => Sources(s)?.ExpandHosts, null));
        list.Add(Single(DnsmasqConfKeys.BogusPriv, status, s => Config(s)?.BogusPriv, s => Sources(s)?.BogusPriv, null));
        list.Add(Multi(DnsmasqConfKeys.RebindDomainOk, status, Items(ec => ec?.RebindDomainOkValues, src => src?.RebindDomainOkValues)));
        list.Add(Multi(DnsmasqConfKeys.BogusNxdomain, status, Items(ec => ec?.BogusNxdomainValues, src => src?.BogusNxdomainValues)));
        list.Add(Multi(DnsmasqConfKeys.IgnoreAddress, status, Items(ec => ec?.IgnoreAddressValues, src => src?.IgnoreAddressValues)));
        list.Add(Multi(DnsmasqConfKeys.Alias, status, Items(ec => ec?.AliasValues, src => src?.AliasValues)));
        list.Add(Multi(DnsmasqConfKeys.FilterRr, status, Items(ec => ec?.FilterRrValues, src => src?.FilterRrValues)));
        list.Add(Single(DnsmasqConfKeys.Filterwin2k, status, s => Config(s)?.Filterwin2k, s => Sources(s)?.Filterwin2k, null));
        list.Add(Single(DnsmasqConfKeys.FilterA, status, s => Config(s)?.FilterA, s => Sources(s)?.FilterA, null));
        list.Add(Single(DnsmasqConfKeys.FilterAaaa, status, s => Config(s)?.FilterAaaa, s => Sources(s)?.FilterAaaa, null));
        list.Add(Single(DnsmasqConfKeys.LocaliseQueries, status, s => Config(s)?.LocaliseQueries, s => Sources(s)?.LocaliseQueries, null));
        list.Add(Single(DnsmasqConfKeys.FastDnsRetry, status, s => Config(s)?.FastDnsRetry, s => Sources(s)?.FastDnsRetry, null));
        list.Add(Multi(DnsmasqConfKeys.Ipset, status, Items(ec => ec?.IpsetValues, src => src?.IpsetValues)));
        list.Add(Multi(DnsmasqConfKeys.Nftset, status, Items(ec => ec?.NftsetValues, src => src?.NftsetValues)));

        // DNS records
        list.Add(Multi(DnsmasqConfKeys.Domain, status, Items(ec => ec?.DomainValues, src => src?.DomainValues)));
        list.Add(Multi(DnsmasqConfKeys.Cname, status, Items(ec => ec?.CnameValues, src => src?.CnameValues)));
        list.Add(Multi(DnsmasqConfKeys.MxHost, status, Items(ec => ec?.MxHostValues, src => src?.MxHostValues)));
        list.Add(Multi(DnsmasqConfKeys.Srv, status, Items(ec => ec?.SrvValues, src => src?.SrvValues)));
        list.Add(Multi(DnsmasqConfKeys.PtrRecord, status, Items(ec => ec?.PtrRecordValues, src => src?.PtrRecordValues)));
        list.Add(Multi(DnsmasqConfKeys.TxtRecord, status, Items(ec => ec?.TxtRecordValues, src => src?.TxtRecordValues)));
        list.Add(Multi(DnsmasqConfKeys.NaptrRecord, status, Items(ec => ec?.NaptrRecordValues, src => src?.NaptrRecordValues)));
        list.Add(Multi(DnsmasqConfKeys.HostRecord, status, Items(ec => ec?.HostRecordValues, src => src?.HostRecordValues)));
        list.Add(Multi(DnsmasqConfKeys.DynamicHost, status, Items(ec => ec?.DynamicHostValues, src => src?.DynamicHostValues)));
        list.Add(Multi(DnsmasqConfKeys.InterfaceName, status, Items(ec => ec?.InterfaceNameValues, src => src?.InterfaceNameValues)));
        list.Add(Single(DnsmasqConfKeys.MxTarget, status, s => Config(s)?.MxTarget, s => Sources(s)?.MxTarget, null));
        list.Add(Single(DnsmasqConfKeys.Localmx, status, s => Config(s)?.Localmx, s => Sources(s)?.Localmx, null));
        list.Add(Single(DnsmasqConfKeys.Selfmx, status, s => Config(s)?.Selfmx, s => Sources(s)?.Selfmx, null));

        // DHCP
        list.Add(Single(DnsmasqConfKeys.DhcpAuthoritative, status, s => Config(s)?.DhcpAuthoritative, s => Sources(s)?.DhcpAuthoritative, null));
        list.Add(Single(DnsmasqConfKeys.DhcpRapidCommit, status, s => Config(s)?.DhcpRapidCommit, s => Sources(s)?.DhcpRapidCommit, null));
        list.Add(Single(DnsmasqConfKeys.LeasefileRo, status, s => Config(s)?.LeasefileRo, s => Sources(s)?.LeasefileRo, null));
        list.Add(Single(DnsmasqConfKeys.DhcpScript, status, s => Config(s)?.DhcpScriptPath, s => Sources(s)?.DhcpScriptPath, null));
        list.Add(Single(DnsmasqConfKeys.DhcpLeasefile, status, s => Config(s)?.DhcpLeaseFilePath, s => Sources(s)?.DhcpLeaseFilePath, null));
        list.Add(Single(DnsmasqConfKeys.DhcpLeaseMax, status, s => Config(s)?.DhcpLeaseMax, s => Sources(s)?.DhcpLeaseMax, null));
        list.Add(Single(DnsmasqConfKeys.DhcpTtl, status, s => Config(s)?.DhcpTtl, s => Sources(s)?.DhcpTtl, null));
        list.Add(Multi(DnsmasqConfKeys.DhcpRange, status, Items(ec => ec?.DhcpRanges, src => src?.DhcpRanges)));
        list.Add(Multi(DnsmasqConfKeys.DhcpHost, status, Items(ec => ec?.DhcpHostLines, src => src?.DhcpHostLines)));
        list.Add(Multi(DnsmasqConfKeys.DhcpOption, status, Items(ec => ec?.DhcpOptionLines, src => src?.DhcpOptionLines)));
        list.Add(Multi(DnsmasqConfKeys.DhcpOptionForce, status, Items(ec => ec?.DhcpOptionForceLines, src => src?.DhcpOptionForceLines)));
        list.Add(Multi(DnsmasqConfKeys.DhcpMatch, status, Items(ec => ec?.DhcpMatchValues, src => src?.DhcpMatchValues)));
        list.Add(Multi(DnsmasqConfKeys.DhcpMac, status, Items(ec => ec?.DhcpMacValues, src => src?.DhcpMacValues)));
        list.Add(Multi(DnsmasqConfKeys.DhcpNameMatch, status, Items(ec => ec?.DhcpNameMatchValues, src => src?.DhcpNameMatchValues)));
        list.Add(Multi(DnsmasqConfKeys.DhcpIgnoreNames, status, Items(ec => ec?.DhcpIgnoreNamesValues, src => src?.DhcpIgnoreNamesValues)));
        list.Add(Multi(DnsmasqConfKeys.DhcpHostsfile, status, Items(ec => ec?.DhcpHostsfilePaths, src => src?.DhcpHostsfilePaths)));
        list.Add(Multi(DnsmasqConfKeys.DhcpOptsfile, status, Items(ec => ec?.DhcpOptsfilePaths, src => src?.DhcpOptsfilePaths)));
        list.Add(Multi(DnsmasqConfKeys.DhcpHostsdir, status, Items(ec => ec?.DhcpHostsdirPaths, src => src?.DhcpHostsdirPaths)));
        list.Add(Multi(DnsmasqConfKeys.DhcpBoot, status, Items(ec => ec?.DhcpBootValues, src => src?.DhcpBootValues)));
        list.Add(Multi(DnsmasqConfKeys.DhcpIgnore, status, Items(ec => ec?.DhcpIgnoreValues, src => src?.DhcpIgnoreValues)));
        list.Add(Multi(DnsmasqConfKeys.DhcpVendorclass, status, Items(ec => ec?.DhcpVendorclassValues, src => src?.DhcpVendorclassValues)));
        list.Add(Multi(DnsmasqConfKeys.DhcpUserclass, status, Items(ec => ec?.DhcpUserclassValues, src => src?.DhcpUserclassValues)));
        list.Add(Multi(DnsmasqConfKeys.RaParam, status, Items(ec => ec?.RaParamValues, src => src?.RaParamValues)));
        list.Add(Multi(DnsmasqConfKeys.Slaac, status, Items(ec => ec?.SlaacValues, src => src?.SlaacValues)));

        // TFTP / PXE
        list.Add(Single(DnsmasqConfKeys.EnableTftp, status, s => Config(s)?.EnableTftp, s => Sources(s)?.EnableTftp, null));
        list.Add(Single(DnsmasqConfKeys.TftpSecure, status, s => Config(s)?.TftpSecure, s => Sources(s)?.TftpSecure, null));
        list.Add(Single(DnsmasqConfKeys.TftpNoFail, status, s => Config(s)?.TftpNoFail, s => Sources(s)?.TftpNoFail, null));
        list.Add(Single(DnsmasqConfKeys.TftpNoBlocksize, status, s => Config(s)?.TftpNoBlocksize, s => Sources(s)?.TftpNoBlocksize, null));
        list.Add(Single(DnsmasqConfKeys.TftpRoot, status, s => Config(s)?.TftpRootPath, s => Sources(s)?.TftpRootPath, null));
        list.Add(Single(DnsmasqConfKeys.PxePrompt, status, s => Config(s)?.PxePrompt, s => Sources(s)?.PxePrompt, null));
        list.Add(Multi(DnsmasqConfKeys.PxeService, status, Items(ec => ec?.PxeServiceValues, src => src?.PxeServiceValues)));

        // DNSSEC
        list.Add(Single(DnsmasqConfKeys.Dnssec, status, s => Config(s)?.Dnssec, s => Sources(s)?.Dnssec, null));
        list.Add(Single(DnsmasqConfKeys.DnssecCheckUnsigned, status, s => Config(s)?.DnssecCheckUnsigned, s => Sources(s)?.DnssecCheckUnsigned, null));
        list.Add(Single(DnsmasqConfKeys.ProxyDnssec, status, s => Config(s)?.ProxyDnssec, s => Sources(s)?.ProxyDnssec, null));
        list.Add(Multi(DnsmasqConfKeys.TrustAnchor, status, Items(ec => ec?.TrustAnchorValues, src => src?.TrustAnchorValues)));

        // Cache
        list.Add(Multi(DnsmasqConfKeys.CacheRr, status, Items(ec => ec?.CacheRrValues, src => src?.CacheRrValues)));
        list.Add(Single(DnsmasqConfKeys.CacheSize, status, s => Config(s)?.CacheSize, s => Sources(s)?.CacheSize, null));
        list.Add(Single(DnsmasqConfKeys.LocalTtl, status, s => Config(s)?.LocalTtl, s => Sources(s)?.LocalTtl, null));
        list.Add(Single(DnsmasqConfKeys.NoNegcache, status, s => Config(s)?.NoNegcache, s => Sources(s)?.NoNegcache, null));
        list.Add(Single(DnsmasqConfKeys.NegTtl, status, s => Config(s)?.NegTtl, s => Sources(s)?.NegTtl, null));
        list.Add(Single(DnsmasqConfKeys.MaxTtl, status, s => Config(s)?.MaxTtl, s => Sources(s)?.MaxTtl, null));
        list.Add(Single(DnsmasqConfKeys.MaxCacheTtl, status, s => Config(s)?.MaxCacheTtl, s => Sources(s)?.MaxCacheTtl, null));
        list.Add(Single(DnsmasqConfKeys.MinCacheTtl, status, s => Config(s)?.MinCacheTtl, s => Sources(s)?.MinCacheTtl, null));

        // Process & networking
        list.Add(Single(DnsmasqConfKeys.NoPoll, status, s => Config(s)?.NoPoll, s => Sources(s)?.NoPoll, null));
        list.Add(Single(DnsmasqConfKeys.BindInterfaces, status, s => Config(s)?.BindInterfaces, s => Sources(s)?.BindInterfaces, null));
        list.Add(Single(DnsmasqConfKeys.BindDynamic, status, s => Config(s)?.BindDynamic, s => Sources(s)?.BindDynamic, null));
        list.Add(Single(DnsmasqConfKeys.LogDebug, status, s => Config(s)?.LogDebug, s => Sources(s)?.LogDebug, null));
        list.Add(Single(DnsmasqConfKeys.LogAsync, status, s => Config(s)?.LogAsync, s => Sources(s)?.LogAsync, null));
        list.Add(Single(DnsmasqConfKeys.LocalService, status, s => Config(s)?.LocalService, s => Sources(s)?.LocalService, null));
        list.Add(Multi(DnsmasqConfKeys.Interface, status, Items(ec => ec?.Interfaces, src => src?.Interfaces)));
        list.Add(Multi(DnsmasqConfKeys.ListenAddress, status, Items(ec => ec?.ListenAddresses, src => src?.ListenAddresses)));
        list.Add(Multi(DnsmasqConfKeys.ExceptInterface, status, Items(ec => ec?.ExceptInterfaces, src => src?.ExceptInterfaces)));
        list.Add(Multi(DnsmasqConfKeys.AuthServer, status, Items(ec => ec?.AuthServerValues, src => src?.AuthServerValues)));
        list.Add(Multi(DnsmasqConfKeys.NoDhcpInterface, status, Items(ec => ec?.NoDhcpInterfaceValues, src => src?.NoDhcpInterfaceValues)));
        list.Add(Multi(DnsmasqConfKeys.NoDhcpv4Interface, status, Items(ec => ec?.NoDhcpv4InterfaceValues, src => src?.NoDhcpv4InterfaceValues)));
        list.Add(Multi(DnsmasqConfKeys.NoDhcpv6Interface, status, Items(ec => ec?.NoDhcpv6InterfaceValues, src => src?.NoDhcpv6InterfaceValues)));
        list.Add(Single(DnsmasqConfKeys.PidFile, status, s => Config(s)?.PidFilePath, s => Sources(s)?.PidFilePath, null));
        list.Add(Single(DnsmasqConfKeys.User, status, s => Config(s)?.User, s => Sources(s)?.User, null));
        list.Add(Single(DnsmasqConfKeys.Group, status, s => Config(s)?.Group, s => Sources(s)?.Group, null));
        list.Add(Single(DnsmasqConfKeys.LogFacility, status, s => Config(s)?.LogFacility, s => Sources(s)?.LogFacility, null));
        list.Add(Single(DnsmasqConfKeys.EnableDbus, status, s => Config(s)?.EnableDbus, s => Sources(s)?.EnableDbus, null));
        list.Add(Single(DnsmasqConfKeys.EnableUbus, status, s => Config(s)?.EnableUbus, s => Sources(s)?.EnableUbus, null));
        list.Add(Single(DnsmasqConfKeys.EnableRa, status, s => Config(s)?.EnableRa, s => Sources(s)?.EnableRa, null));
        list.Add(Single(DnsmasqConfKeys.LogDhcp, status, s => Config(s)?.LogDhcp, s => Sources(s)?.LogDhcp, null));
        list.Add(Single(DnsmasqConfKeys.KeepInForeground, status, s => Config(s)?.KeepInForeground, s => Sources(s)?.KeepInForeground, null));
        list.Add(Single(DnsmasqConfKeys.NoDaemon, status, s => Config(s)?.NoDaemon, s => Sources(s)?.NoDaemon, null));
        list.Add(Single(DnsmasqConfKeys.Conntrack, status, s => Config(s)?.Conntrack, s => Sources(s)?.Conntrack, null));

        return list;
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

    private static EffectiveConfigFieldDescriptor Multi(string optionName, DnsmasqServiceStatus? status,
        Func<DnsmasqServiceStatus?, IReadOnlyList<ValueWithSource>?>? getItems)
    {
        var sectionId = EffectiveConfigSections.GetSectionId(optionName);
        return new EffectiveConfigFieldDescriptor(sectionId, optionName, true, status, null, null, getItems);
    }
}
