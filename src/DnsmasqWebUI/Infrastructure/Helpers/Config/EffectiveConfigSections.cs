using System.Collections.Frozen;

namespace DnsmasqWebUI.Infrastructure.Helpers.Config;

/// <summary>
/// Single source of truth: which effective-config sections exist, their display order and titles,
/// and which option (conf key) belongs to which section. Used by the field builder (section lookup)
/// and by the UI (section order, context filtering).
/// </summary>
public static class EffectiveConfigSections
{
    // --- Section IDs (used by registry, views, etc.) ---
    public const string SectionHosts = "hosts";
    public const string SectionResolver = "resolver";
    public const string SectionDnsRecords = "dns-records";
    public const string SectionDhcp = "dhcp";
    public const string SectionTftpPxe = "tftp-pxe";
    public const string SectionDnssec = "dnssec";
    public const string SectionCache = "cache";
    public const string SectionProcess = "process";

    /// <summary>Section definition: id, display title, option names in display order.</summary>
    public sealed record SectionDef(string SectionId, string Title, string[] OptionNames);

    /// <summary>All sections in display order with their option names. Defines option → section mapping.</summary>
    public static readonly IReadOnlyList<SectionDef> Sections =
    [
        new SectionDef(SectionHosts, "Hosts", [
            DnsmasqConfKeys.NoHosts,
            DnsmasqConfKeys.AddnHosts,
            DnsmasqConfKeys.Hostsdir,
            DnsmasqConfKeys.ReadEthers,
        ]),
        new SectionDef(SectionResolver, "Resolver / DNS", [
            DnsmasqOptionTooltips.ServerLocalLabel,
            DnsmasqConfKeys.RevServer,
            DnsmasqConfKeys.Address,
            DnsmasqConfKeys.ResolvFile,
            DnsmasqConfKeys.NoResolv,
            DnsmasqConfKeys.DomainNeeded,
            DnsmasqConfKeys.Port,
            DnsmasqConfKeys.LogQueries,
            DnsmasqConfKeys.StrictOrder,
            DnsmasqConfKeys.AllServers,
            DnsmasqConfKeys.AuthTtl,
            DnsmasqConfKeys.EdnsPacketMax,
            DnsmasqConfKeys.QueryPort,
            DnsmasqConfKeys.PortLimit,
            DnsmasqConfKeys.MinPort,
            DnsmasqConfKeys.MaxPort,
            DnsmasqConfKeys.DnsLoopDetect,
            DnsmasqConfKeys.StopDnsRebind,
            DnsmasqConfKeys.RebindLocalhostOk,
            DnsmasqConfKeys.ClearOnReload,
            DnsmasqConfKeys.ExpandHosts,
            DnsmasqConfKeys.BogusPriv,
            DnsmasqConfKeys.RebindDomainOk,
            DnsmasqConfKeys.BogusNxdomain,
            DnsmasqConfKeys.IgnoreAddress,
            DnsmasqConfKeys.Alias,
            DnsmasqConfKeys.FilterRr,
            DnsmasqConfKeys.Filterwin2k,
            DnsmasqConfKeys.FilterA,
            DnsmasqConfKeys.FilterAaaa,
            DnsmasqConfKeys.LocaliseQueries,
            DnsmasqConfKeys.FastDnsRetry,
            DnsmasqConfKeys.Ipset,
            DnsmasqConfKeys.Nftset,
        ]),
        new SectionDef(SectionDnsRecords, "DNS records", [
            DnsmasqConfKeys.Domain,
            DnsmasqConfKeys.Cname,
            DnsmasqConfKeys.MxHost,
            DnsmasqConfKeys.Srv,
            DnsmasqConfKeys.PtrRecord,
            DnsmasqConfKeys.TxtRecord,
            DnsmasqConfKeys.NaptrRecord,
            DnsmasqConfKeys.HostRecord,
            DnsmasqConfKeys.DynamicHost,
            DnsmasqConfKeys.InterfaceName,
            DnsmasqConfKeys.MxTarget,
            DnsmasqConfKeys.Localmx,
            DnsmasqConfKeys.Selfmx,
        ]),
        new SectionDef(SectionDhcp, "DHCP", [
            DnsmasqConfKeys.DhcpAuthoritative,
            DnsmasqConfKeys.DhcpRapidCommit,
            DnsmasqConfKeys.LeasefileRo,
            DnsmasqConfKeys.DhcpScript,
            DnsmasqConfKeys.DhcpLeasefile,
            DnsmasqConfKeys.DhcpLeaseMax,
            DnsmasqConfKeys.DhcpTtl,
            DnsmasqConfKeys.DhcpRange,
            DnsmasqConfKeys.DhcpHost,
            DnsmasqConfKeys.DhcpOption,
            DnsmasqConfKeys.DhcpOptionForce,
            DnsmasqConfKeys.DhcpMatch,
            DnsmasqConfKeys.DhcpMac,
            DnsmasqConfKeys.DhcpNameMatch,
            DnsmasqConfKeys.DhcpIgnoreNames,
            DnsmasqConfKeys.DhcpHostsfile,
            DnsmasqConfKeys.DhcpOptsfile,
            DnsmasqConfKeys.DhcpHostsdir,
            DnsmasqConfKeys.DhcpBoot,
            DnsmasqConfKeys.DhcpIgnore,
            DnsmasqConfKeys.DhcpVendorclass,
            DnsmasqConfKeys.DhcpUserclass,
            DnsmasqConfKeys.RaParam,
            DnsmasqConfKeys.Slaac,
        ]),
        new SectionDef(SectionTftpPxe, "TFTP / PXE", [
            DnsmasqConfKeys.EnableTftp,
            DnsmasqConfKeys.TftpSecure,
            DnsmasqConfKeys.TftpNoFail,
            DnsmasqConfKeys.TftpNoBlocksize,
            DnsmasqConfKeys.TftpRoot,
            DnsmasqConfKeys.PxePrompt,
            DnsmasqConfKeys.PxeService,
        ]),
        new SectionDef(SectionDnssec, "DNSSEC", [
            DnsmasqConfKeys.Dnssec,
            DnsmasqConfKeys.DnssecCheckUnsigned,
            DnsmasqConfKeys.ProxyDnssec,
            DnsmasqConfKeys.TrustAnchor,
        ]),
        new SectionDef(SectionCache, "Cache", [
            DnsmasqConfKeys.CacheRr,
            DnsmasqConfKeys.CacheSize,
            DnsmasqConfKeys.LocalTtl,
            DnsmasqConfKeys.NoNegcache,
            DnsmasqConfKeys.NegTtl,
            DnsmasqConfKeys.MaxTtl,
            DnsmasqConfKeys.MaxCacheTtl,
            DnsmasqConfKeys.MinCacheTtl,
        ]),
        new SectionDef(SectionProcess, "Process & networking", [
            DnsmasqConfKeys.NoPoll,
            DnsmasqConfKeys.BindInterfaces,
            DnsmasqConfKeys.BindDynamic,
            DnsmasqConfKeys.LogDebug,
            DnsmasqConfKeys.LogAsync,
            DnsmasqConfKeys.LocalService,
            DnsmasqConfKeys.Interface,
            DnsmasqConfKeys.ListenAddress,
            DnsmasqConfKeys.ExceptInterface,
            DnsmasqConfKeys.AuthServer,
            DnsmasqConfKeys.NoDhcpInterface,
            DnsmasqConfKeys.NoDhcpv4Interface,
            DnsmasqConfKeys.NoDhcpv6Interface,
            DnsmasqConfKeys.PidFile,
            DnsmasqConfKeys.User,
            DnsmasqConfKeys.Group,
            DnsmasqConfKeys.LogFacility,
            DnsmasqConfKeys.EnableDbus,
            DnsmasqConfKeys.EnableUbus,
            DnsmasqConfKeys.EnableRa,
            DnsmasqConfKeys.LogDhcp,
            DnsmasqConfKeys.KeepInForeground,
            DnsmasqConfKeys.NoDaemon,
            DnsmasqConfKeys.Conntrack,
        ]),
    ];

    private static FrozenDictionary<string, string>? _optionToSection;

    /// <summary>Maps option name (conf key or display label) to section id. Used by Single/Multi in the builder.</summary>
    public static string GetSectionId(string optionName)
    {
        _optionToSection ??= BuildOptionToSection();
        return _optionToSection.TryGetValue(optionName, out var sectionId) ? sectionId : SectionResolver;
    }

    /// <summary>Section id and title in display order. Use for rendering section headers.</summary>
    public static IReadOnlyList<(string SectionId, string Title)> GetSectionsInOrder() =>
        Sections.Select(s => (s.SectionId, s.Title)).ToList();

    /// <summary>Option names in the given section (display order). For context/views that restrict by section.</summary>
    public static IReadOnlyList<string> GetOptionsInSection(string sectionId)
    {
        var def = Sections.FirstOrDefault(s => string.Equals(s.SectionId, sectionId, StringComparison.OrdinalIgnoreCase));
        return def?.OptionNames ?? Array.Empty<string>();
    }

    private static FrozenDictionary<string, string> BuildOptionToSection()
    {
        var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var section in Sections)
        {
            foreach (var option in section.OptionNames)
                dict[option] = section.SectionId;
        }
        return dict.ToFrozenDictionary(StringComparer.OrdinalIgnoreCase);
    }
}
