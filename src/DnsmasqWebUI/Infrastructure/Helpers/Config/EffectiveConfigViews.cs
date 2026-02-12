using DnsmasqWebUI.Models.Dnsmasq;
using DnsmasqWebUI.Models.Dnsmasq.EffectiveConfig;

namespace DnsmasqWebUI.Infrastructure.Helpers.Config;

/// <summary>
/// Defines which sections and (optionally) which fields are visible per EffectiveConfigContext.
/// Single place to add new contexts or change what shows on Hosts/DHCP/etc.
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
            DnsmasqConfKeys.ReadEthers,
        ]),
    ];

    private static IReadOnlyList<EffectiveConfigSectionView> GetDhcpView() =>
    [
        new(EffectiveConfigSections.SectionDhcp, "DHCP", null),
        new(EffectiveConfigSections.SectionTftpPxe, "TFTP / PXE", null),
    ];

    /// <summary>
    /// For each section in the view, returns the context-visible descriptors for that section.
    /// Does not apply search; component applies search to each section's list and hides sections with no matches.
    /// </summary>
    public static IReadOnlyDictionary<string, IReadOnlyList<EffectiveConfigFieldDescriptor>> GetDescriptorsBySection(
        DnsmasqServiceStatus? status,
        IReadOnlyList<EffectiveConfigSectionView> views)
    {
        if (status == null || views.Count == 0)
            return new Dictionary<string, IReadOnlyList<EffectiveConfigFieldDescriptor>>();

        var allDescriptors = EffectiveConfigFieldBuilder.BuildFieldDescriptors(status);
        var result = new Dictionary<string, List<EffectiveConfigFieldDescriptor>>(StringComparer.OrdinalIgnoreCase);

        foreach (var view in views)
        {
            var list = allDescriptors
                .Where(d => string.Equals(d.SectionId, view.SectionId, StringComparison.OrdinalIgnoreCase))
                .Where(d => view.AllowedOptionNames == null
                    || view.AllowedOptionNames.Contains(d.OptionName, StringComparer.OrdinalIgnoreCase))
                .ToList();
            if (list.Count > 0)
                result[view.SectionId] = list;
        }

        return result.ToDictionary(kv => kv.Key, kv => (IReadOnlyList<EffectiveConfigFieldDescriptor>)kv.Value, StringComparer.OrdinalIgnoreCase);
    }
}
