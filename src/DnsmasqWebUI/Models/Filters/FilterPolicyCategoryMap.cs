namespace DnsmasqWebUI.Models.Filters;

public static class FilterPolicyCategoryMap
{
    public static FilterPolicyCategory GetCategory(FilterPolicyKind kind) => kind switch
    {
        FilterPolicyKind.Address => FilterPolicyCategory.Blocking,
        FilterPolicyKind.Server => FilterPolicyCategory.SplitDns,
        FilterPolicyKind.RevServer => FilterPolicyCategory.SplitDns,
        FilterPolicyKind.Local => FilterPolicyCategory.SplitDns,
        FilterPolicyKind.StopDnsRebind => FilterPolicyCategory.Safety,
        FilterPolicyKind.RebindLocalhostOk => FilterPolicyCategory.Safety,
        FilterPolicyKind.RebindDomainOk => FilterPolicyCategory.Safety,
        FilterPolicyKind.DomainNeeded => FilterPolicyCategory.Safety,
        FilterPolicyKind.BogusPriv => FilterPolicyCategory.Safety,
        FilterPolicyKind.BogusNxdomain => FilterPolicyCategory.Safety,
        FilterPolicyKind.IgnoreAddress => FilterPolicyCategory.Safety,
        FilterPolicyKind.FilterRr => FilterPolicyCategory.ResponseShaping,
        FilterPolicyKind.FilterA => FilterPolicyCategory.ResponseShaping,
        FilterPolicyKind.FilterAaaa => FilterPolicyCategory.ResponseShaping,
        FilterPolicyKind.Filterwin2k => FilterPolicyCategory.ResponseShaping,
        FilterPolicyKind.NoRoundRobin => FilterPolicyCategory.ResponseShaping,
        FilterPolicyKind.Alias => FilterPolicyCategory.SetTargeting,
        FilterPolicyKind.Ipset => FilterPolicyCategory.SetTargeting,
        FilterPolicyKind.Nftset => FilterPolicyCategory.SetTargeting,
        FilterPolicyKind.ConnmarkAllowlistEnable => FilterPolicyCategory.SetTargeting,
        FilterPolicyKind.ConnmarkAllowlist => FilterPolicyCategory.SetTargeting,
        _ => FilterPolicyCategory.Safety
    };

    public static string CategoryTitle(FilterPolicyCategory c) => c switch
    {
        FilterPolicyCategory.Blocking => "Blocking / sinkhole",
        FilterPolicyCategory.SplitDns => "Split DNS & local routing",
        FilterPolicyCategory.Safety => "Safety & rebinding",
        FilterPolicyCategory.ResponseShaping => "Response shaping",
        FilterPolicyCategory.SetTargeting => "Rewrite & set targeting",
        _ => c.ToString()
    };

    public static string CategoryChipLabel(FilterPolicyCategory c) => c switch
    {
        FilterPolicyCategory.Blocking => "Blocking",
        FilterPolicyCategory.SplitDns => "Split DNS",
        FilterPolicyCategory.Safety => "Safety",
        FilterPolicyCategory.ResponseShaping => "Response shaping",
        FilterPolicyCategory.SetTargeting => "Set targeting",
        _ => c.ToString()
    };
}
