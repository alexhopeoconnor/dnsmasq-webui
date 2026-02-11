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

    public const string SectionHosts = "hosts";
    public const string SectionResolver = "resolver";
    public const string SectionDhcp = "dhcp";
    public const string SectionCache = "cache";
    public const string SectionProcess = "process";

    public static IReadOnlyList<EffectiveConfigFieldDescriptor> BuildFieldDescriptors(DnsmasqServiceStatus? status)
    {
        if (status == null || status.EffectiveConfig == null)
            return Array.Empty<EffectiveConfigFieldDescriptor>();

        var list = new List<EffectiveConfigFieldDescriptor>();

        // --- Hosts ---
        list.Add(Single(SectionHosts, DnsmasqConfKeys.NoHosts, status, s => Config(s)?.NoHosts, s => Sources(s)?.NoHosts, null));
        list.Add(Multi(SectionHosts, DnsmasqConfKeys.AddnHosts, status,
            s => ToValueWithSourceList(s?.AddnHostsPaths, Sources(s)?.AddnHostsPaths)));

        // --- Resolver / DNS ---
        list.Add(Single(SectionResolver, DnsmasqConfKeys.ExpandHosts, status, s => Config(s)?.ExpandHosts, s => Sources(s)?.ExpandHosts, null));
        list.Add(Single(SectionResolver, DnsmasqConfKeys.BogusPriv, status, s => Config(s)?.BogusPriv, s => Sources(s)?.BogusPriv, null));
        list.Add(Single(SectionResolver, DnsmasqConfKeys.StrictOrder, status, s => Config(s)?.StrictOrder, s => Sources(s)?.StrictOrder, null));
        list.Add(Single(SectionResolver, DnsmasqConfKeys.NoResolv, status, s => Config(s)?.NoResolv, s => Sources(s)?.NoResolv, null));
        list.Add(Single(SectionResolver, DnsmasqConfKeys.DomainNeeded, status, s => Config(s)?.DomainNeeded, s => Sources(s)?.DomainNeeded, null));
        list.Add(Single(SectionResolver, DnsmasqConfKeys.AllServers, status, s => Config(s)?.AllServers, s => Sources(s)?.AllServers, null));
        list.Add(Single(SectionResolver, DnsmasqConfKeys.Port, status, s => Config(s)?.Port, s => Sources(s)?.Port, null));
        list.Add(Single(SectionResolver, DnsmasqConfKeys.LogQueries, status, s => Config(s)?.LogQueries, s => Sources(s)?.LogQueries, null));
        list.Add(Single(SectionResolver, DnsmasqConfKeys.AuthTtl, status, s => Config(s)?.AuthTtl, s => Sources(s)?.AuthTtl, null));
        list.Add(Single(SectionResolver, DnsmasqConfKeys.EdnsPacketMax, status, s => Config(s)?.EdnsPacketMax, s => Sources(s)?.EdnsPacketMax, null));
        list.Add(Single(SectionResolver, DnsmasqConfKeys.QueryPort, status, s => Config(s)?.QueryPort, s => Sources(s)?.QueryPort, null));
        list.Add(Single(SectionResolver, DnsmasqConfKeys.PortLimit, status, s => Config(s)?.PortLimit, s => Sources(s)?.PortLimit, null));
        list.Add(Single(SectionResolver, DnsmasqConfKeys.MinPort, status, s => Config(s)?.MinPort, s => Sources(s)?.MinPort, null));
        list.Add(Single(SectionResolver, DnsmasqConfKeys.MaxPort, status, s => Config(s)?.MaxPort, s => Sources(s)?.MaxPort, null));
        list.Add(Single(SectionResolver, DnsmasqConfKeys.DnsLoopDetect, status, s => Config(s)?.DnsLoopDetect, s => Sources(s)?.DnsLoopDetect, null));
        list.Add(Single(SectionResolver, DnsmasqConfKeys.StopDnsRebind, status, s => Config(s)?.StopDnsRebind, s => Sources(s)?.StopDnsRebind, null));
        list.Add(Single(SectionResolver, DnsmasqConfKeys.RebindLocalhostOk, status, s => Config(s)?.RebindLocalhostOk, s => Sources(s)?.RebindLocalhostOk, null));
        list.Add(Single(SectionResolver, DnsmasqConfKeys.ClearOnReload, status, s => Config(s)?.ClearOnReload, s => Sources(s)?.ClearOnReload, null));
        list.Add(Single(SectionResolver, DnsmasqConfKeys.Filterwin2k, status, s => Config(s)?.Filterwin2k, s => Sources(s)?.Filterwin2k, null));
        list.Add(Single(SectionResolver, DnsmasqConfKeys.FilterA, status, s => Config(s)?.FilterA, s => Sources(s)?.FilterA, null));
        list.Add(Single(SectionResolver, DnsmasqConfKeys.FilterAaaa, status, s => Config(s)?.FilterAaaa, s => Sources(s)?.FilterAaaa, null));
        list.Add(Single(SectionResolver, DnsmasqConfKeys.LocaliseQueries, status, s => Config(s)?.LocaliseQueries, s => Sources(s)?.LocaliseQueries, null));
        list.Add(Multi(SectionResolver, "server / local", status, Items(ec => ec?.ServerLocalValues, src => src?.ServerLocalValues)));
        list.Add(Multi(SectionResolver, DnsmasqConfKeys.RevServer, status, Items(ec => ec?.RevServerValues, src => src?.RevServerValues)));
        list.Add(Multi(SectionResolver, DnsmasqConfKeys.Address, status, Items(ec => ec?.AddressValues, src => src?.AddressValues)));
        list.Add(Multi(SectionResolver, DnsmasqConfKeys.ResolvFile, status, Items(ec => ec?.ResolvFiles, src => src?.ResolvFiles)));
        list.Add(Multi(SectionResolver, DnsmasqConfKeys.RebindDomainOk, status, Items(ec => ec?.RebindDomainOkValues, src => src?.RebindDomainOkValues)));
        list.Add(Multi(SectionResolver, DnsmasqConfKeys.BogusNxdomain, status, Items(ec => ec?.BogusNxdomainValues, src => src?.BogusNxdomainValues)));
        list.Add(Multi(SectionResolver, DnsmasqConfKeys.IgnoreAddress, status, Items(ec => ec?.IgnoreAddressValues, src => src?.IgnoreAddressValues)));
        list.Add(Multi(SectionResolver, DnsmasqConfKeys.Alias, status, Items(ec => ec?.AliasValues, src => src?.AliasValues)));
        list.Add(Multi(SectionResolver, DnsmasqConfKeys.FilterRr, status, Items(ec => ec?.FilterRrValues, src => src?.FilterRrValues)));

        // --- DHCP ---
        list.Add(Single(SectionDhcp, DnsmasqConfKeys.DhcpAuthoritative, status, s => Config(s)?.DhcpAuthoritative, s => Sources(s)?.DhcpAuthoritative, null));
        list.Add(Single(SectionDhcp, DnsmasqConfKeys.LeasefileRo, status, s => Config(s)?.LeasefileRo, s => Sources(s)?.LeasefileRo, null));
        list.Add(Single(SectionDhcp, DnsmasqConfKeys.DhcpLeasefile, status, s => Config(s)?.DhcpLeaseFilePath, s => Sources(s)?.DhcpLeaseFilePath, null));
        list.Add(Single(SectionDhcp, DnsmasqConfKeys.DhcpLeaseMax, status, s => Config(s)?.DhcpLeaseMax, s => Sources(s)?.DhcpLeaseMax, null));
        list.Add(Single(SectionDhcp, DnsmasqConfKeys.DhcpTtl, status, s => Config(s)?.DhcpTtl, s => Sources(s)?.DhcpTtl, null));
        list.Add(Multi(SectionDhcp, DnsmasqConfKeys.DhcpRange, status, Items(ec => ec?.DhcpRanges, src => src?.DhcpRanges)));
        list.Add(Multi(SectionDhcp, DnsmasqConfKeys.DhcpHost, status, Items(ec => ec?.DhcpHostLines, src => src?.DhcpHostLines)));
        list.Add(Multi(SectionDhcp, DnsmasqConfKeys.DhcpOption, status, Items(ec => ec?.DhcpOptionLines, src => src?.DhcpOptionLines)));

        // --- Cache ---
        list.Add(Multi(SectionCache, DnsmasqConfKeys.CacheRr, status, Items(ec => ec?.CacheRrValues, src => src?.CacheRrValues)));
        list.Add(Single(SectionCache, DnsmasqConfKeys.CacheSize, status, s => Config(s)?.CacheSize, s => Sources(s)?.CacheSize, null));
        list.Add(Single(SectionCache, DnsmasqConfKeys.LocalTtl, status, s => Config(s)?.LocalTtl, s => Sources(s)?.LocalTtl, null));
        list.Add(Single(SectionCache, DnsmasqConfKeys.NoNegcache, status, s => Config(s)?.NoNegcache, s => Sources(s)?.NoNegcache, null));
        list.Add(Single(SectionCache, DnsmasqConfKeys.NegTtl, status, s => Config(s)?.NegTtl, s => Sources(s)?.NegTtl, null));
        list.Add(Single(SectionCache, DnsmasqConfKeys.MaxTtl, status, s => Config(s)?.MaxTtl, s => Sources(s)?.MaxTtl, null));
        list.Add(Single(SectionCache, DnsmasqConfKeys.MaxCacheTtl, status, s => Config(s)?.MaxCacheTtl, s => Sources(s)?.MaxCacheTtl, null));
        list.Add(Single(SectionCache, DnsmasqConfKeys.MinCacheTtl, status, s => Config(s)?.MinCacheTtl, s => Sources(s)?.MinCacheTtl, null));

        // --- Process & networking ---
        list.Add(Single(SectionProcess, DnsmasqConfKeys.NoPoll, status, s => Config(s)?.NoPoll, s => Sources(s)?.NoPoll, null));
        list.Add(Single(SectionProcess, DnsmasqConfKeys.BindInterfaces, status, s => Config(s)?.BindInterfaces, s => Sources(s)?.BindInterfaces, null));
        list.Add(Single(SectionProcess, DnsmasqConfKeys.BindDynamic, status, s => Config(s)?.BindDynamic, s => Sources(s)?.BindDynamic, null));
        list.Add(Single(SectionProcess, DnsmasqConfKeys.LogDebug, status, s => Config(s)?.LogDebug, s => Sources(s)?.LogDebug, null));
        list.Add(Single(SectionProcess, DnsmasqConfKeys.LogAsync, status, s => Config(s)?.LogAsync, s => Sources(s)?.LogAsync, null));
        list.Add(Single(SectionProcess, DnsmasqConfKeys.LocalService, status, s => Config(s)?.LocalService, s => Sources(s)?.LocalService, null));
        list.Add(Multi(SectionProcess, DnsmasqConfKeys.Interface, status, Items(ec => ec?.Interfaces, src => src?.Interfaces)));
        list.Add(Multi(SectionProcess, DnsmasqConfKeys.ListenAddress, status, Items(ec => ec?.ListenAddresses, src => src?.ListenAddresses)));
        list.Add(Multi(SectionProcess, DnsmasqConfKeys.ExceptInterface, status, Items(ec => ec?.ExceptInterfaces, src => src?.ExceptInterfaces)));
        list.Add(Multi(SectionProcess, DnsmasqConfKeys.AuthServer, status, Items(ec => ec?.AuthServerValues, src => src?.AuthServerValues)));
        list.Add(Multi(SectionProcess, DnsmasqConfKeys.NoDhcpInterface, status, Items(ec => ec?.NoDhcpInterfaceValues, src => src?.NoDhcpInterfaceValues)));
        list.Add(Multi(SectionProcess, DnsmasqConfKeys.NoDhcpv4Interface, status, Items(ec => ec?.NoDhcpv4InterfaceValues, src => src?.NoDhcpv4InterfaceValues)));
        list.Add(Multi(SectionProcess, DnsmasqConfKeys.NoDhcpv6Interface, status, Items(ec => ec?.NoDhcpv6InterfaceValues, src => src?.NoDhcpv6InterfaceValues)));
        list.Add(Single(SectionProcess, DnsmasqConfKeys.PidFile, status, s => Config(s)?.PidFilePath, s => Sources(s)?.PidFilePath, null));
        list.Add(Single(SectionProcess, DnsmasqConfKeys.User, status, s => Config(s)?.User, s => Sources(s)?.User, null));
        list.Add(Single(SectionProcess, DnsmasqConfKeys.Group, status, s => Config(s)?.Group, s => Sources(s)?.Group, null));
        list.Add(Single(SectionProcess, DnsmasqConfKeys.LogFacility, status, s => Config(s)?.LogFacility, s => Sources(s)?.LogFacility, null));

        return list;
    }

    private static EffectiveConfigFieldDescriptor Single(string sectionId, string optionName, DnsmasqServiceStatus? status,
        Func<DnsmasqServiceStatus?, object?>? getValue,
        Func<DnsmasqServiceStatus?, ConfigValueSource?>? getSource,
        Func<DnsmasqServiceStatus?, IReadOnlyList<ValueWithSource>?>? getItems)
    {
        return new EffectiveConfigFieldDescriptor(sectionId, optionName, false, status, getValue, getSource, getItems);
    }

    private static IReadOnlyList<ValueWithSource>? ToValueWithSourceList(IReadOnlyList<string>? paths, IReadOnlyList<PathWithSource>? withSource)
    {
        if (paths == null || paths.Count == 0) return null;
        return withSource != null && withSource.Count == paths.Count
            ? withSource.Select(p => new ValueWithSource(p.Path, p.Source)).ToList()
            : paths.Select(p => new ValueWithSource(p, null)).ToList();
    }

    private static EffectiveConfigFieldDescriptor Multi(string sectionId, string optionName, DnsmasqServiceStatus? status,
        Func<DnsmasqServiceStatus?, IReadOnlyList<ValueWithSource>?>? getItems)
    {
        return new EffectiveConfigFieldDescriptor(sectionId, optionName, true, status, null, null, getItems);
    }
}
