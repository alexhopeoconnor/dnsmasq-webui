using DnsmasqWebUI.Infrastructure.Services.EffectiveConfig.Abstractions;
using DnsmasqWebUI.Models.Dnsmasq.EffectiveConfig;

namespace DnsmasqWebUI.Infrastructure.Services.EffectiveConfig.Metadata;

/// <summary>
/// Defines which sections and (optionally) which fields are visible per EffectiveConfigContext.
/// Visibility only; descriptor construction is done by <see cref="IEffectiveConfigDescriptorProvider"/>.
/// </summary>
public static class EffectiveConfigViews
{
    /// <summary>
    /// Ordered list of sections and optional field allow-lists for the given context.
    /// </summary>
    public static IReadOnlyList<EffectiveConfigSectionView> GetViewsForContext(EffectiveConfigContext context)
    {
        return context switch
        {
            EffectiveConfigContext.All => GetAllSections(),
            EffectiveConfigContext.Hosts => GetHostsView(),
            EffectiveConfigContext.Dhcp => GetDhcpView(),
            EffectiveConfigContext.DnsRecords => GetDnsRecordsView(),
            EffectiveConfigContext.Filters => GetFiltersView(),
            _ => [],
        };
    }

    private static IReadOnlyList<EffectiveConfigSectionView> GetAllSections()
    {
        return EffectiveConfigSections.Sections
            .Select(s => new EffectiveConfigSectionView(s.SectionId, s.Title, null))
            .ToList();
    }

    private static IReadOnlyList<EffectiveConfigSectionView> GetHostsView() =>
    [
        new(EffectiveConfigSections.SectionHosts, "Hosts", [
            DnsmasqConfKeys.NoHosts,
            DnsmasqConfKeys.AddnHosts,
            DnsmasqConfKeys.Hostsdir,
        ]),
        new(EffectiveConfigSections.SectionResolver, "Resolver / DNS", [
            DnsmasqConfKeys.ExpandHosts,
        ]),
    ];

    private static IReadOnlyList<EffectiveConfigSectionView> GetDhcpView() =>
    [
        new(EffectiveConfigSections.SectionDhcp, "DHCP", null),
        new(EffectiveConfigSections.SectionTftpPxe, "TFTP / PXE", null),
    ];

    private static IReadOnlyList<EffectiveConfigSectionView> GetDnsRecordsView() =>
    [
        new(EffectiveConfigSections.SectionDnsRecords, "DNS records", null),
    ];

    private static IReadOnlyList<EffectiveConfigSectionView> GetFiltersView() =>
    [
        new(EffectiveConfigSections.SectionResolver, "Resolver / DNS", [
            DnsmasqConfKeys.DomainNeeded,
            DnsmasqConfKeys.Local,
            DnsmasqConfKeys.Server,
            DnsmasqConfKeys.RevServer,
            DnsmasqConfKeys.Address,
            DnsmasqConfKeys.BogusPriv,
            DnsmasqConfKeys.BogusNxdomain,
            DnsmasqConfKeys.IgnoreAddress,
            DnsmasqConfKeys.Alias,
            DnsmasqConfKeys.Filterwin2k,
            DnsmasqConfKeys.FilterA,
            DnsmasqConfKeys.FilterAaaa,
            DnsmasqConfKeys.FilterRr,
            DnsmasqConfKeys.StopDnsRebind,
            DnsmasqConfKeys.RebindLocalhostOk,
            DnsmasqConfKeys.RebindDomainOk,
            DnsmasqConfKeys.Ipset,
            DnsmasqConfKeys.Nftset,
            DnsmasqConfKeys.ConnmarkAllowlistEnable,
            DnsmasqConfKeys.ConnmarkAllowlist,
            DnsmasqConfKeys.NoRoundRobin,
        ]),
    ];
}
