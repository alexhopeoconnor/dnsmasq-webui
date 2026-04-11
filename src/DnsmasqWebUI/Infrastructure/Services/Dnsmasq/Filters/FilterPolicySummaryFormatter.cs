using DnsmasqWebUI.Infrastructure.Serialization.Parsers.Filters;
using DnsmasqWebUI.Infrastructure.Services.Dnsmasq.Filters.Abstractions;
using DnsmasqWebUI.Models.Filters;

namespace DnsmasqWebUI.Infrastructure.Services.Dnsmasq.Filters;

public sealed class FilterPolicySummaryFormatter : IFilterPolicySummaryFormatter
{
    public string Format(FilterPolicyKind kind, string? rawValue) => kind switch
    {
        FilterPolicyKind.Address => AddressRuleFields.Parse(rawValue).ToSummary(),
        FilterPolicyKind.Server => ServerRuleFields.Parse(rawValue).ToSummary(),
        FilterPolicyKind.RevServer => RevServerRuleFields.Parse(rawValue).ToSummary(),
        FilterPolicyKind.Local => LocalRuleFields.Parse(rawValue).ToSummary(),
        FilterPolicyKind.RebindDomainOk => RebindDomainOkRuleFields.Parse(rawValue).ToSummary(),
        FilterPolicyKind.BogusNxdomain => BogusNxdomainRuleFields.Parse(rawValue).ToSummary(),
        FilterPolicyKind.IgnoreAddress => IgnoreAddressRuleFields.Parse(rawValue).ToSummary(),
        FilterPolicyKind.Alias => AliasRuleFields.Parse(rawValue).ToSummary(),
        FilterPolicyKind.FilterRr => FilterRrRuleFields.Parse(rawValue).ToSummary(),
        FilterPolicyKind.Ipset => IpsetRuleFields.Parse(rawValue).ToSummary(),
        FilterPolicyKind.Nftset => NftsetRuleFields.Parse(rawValue).ToSummary(),
        FilterPolicyKind.ConnmarkAllowlist => ConnmarkAllowlistRuleFields.Parse(rawValue).ToSummary(),
        _ => (rawValue ?? "").Trim()
    };
}
