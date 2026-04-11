using DnsmasqWebUI.Infrastructure.Serialization.Parsers.Filters;
using DnsmasqWebUI.Infrastructure.Services.Dnsmasq.Filters.Abstractions;
using DnsmasqWebUI.Infrastructure.Services.EffectiveConfig.Abstractions;
using DnsmasqWebUI.Infrastructure.Services.EffectiveConfig.Metadata;
using DnsmasqWebUI.Models.Dnsmasq;
using DnsmasqWebUI.Models.Dnsmasq.EffectiveConfig;
using DnsmasqWebUI.Models.Filters;

namespace DnsmasqWebUI.Infrastructure.Services.Dnsmasq.Filters;

public sealed class FiltersPageProjectionService : IFiltersPageProjectionService
{
    private readonly IFilterPolicySummaryFormatter _summaries;
    private readonly IEffectiveMultiValueProjectionService _multiValueProjection;

    public FiltersPageProjectionService(
        IFilterPolicySummaryFormatter summaries,
        IEffectiveMultiValueProjectionService multiValueProjection)
    {
        _summaries = summaries;
        _multiValueProjection = multiValueProjection;
    }

    public IReadOnlyList<FilterPolicyGroup> BuildGroups(
        DnsmasqServiceStatus status,
        FilterPolicyQueryState query,
        Func<string, IReadOnlyList<string>>? currentValuesAccessor = null)
    {
        var all = BuildAllRows(status, currentValuesAccessor);
        var filtered = ApplyQuery(all, query);

        if (query.Category is { } only)
        {
            var rows = filtered.Where(r => r.Category == only)
                .OrderBy(r => r.Summary, StringComparer.OrdinalIgnoreCase)
                .ToList();
            return [new FilterPolicyGroup(only, FilterPolicyCategoryMap.CategoryTitle(only), rows)];
        }

        var orderedCategories = new[]
        {
            FilterPolicyCategory.Blocking,
            FilterPolicyCategory.SplitDns,
            FilterPolicyCategory.Safety,
            FilterPolicyCategory.ResponseShaping,
            FilterPolicyCategory.SetTargeting
        };

        return orderedCategories
            .Select(cat => new FilterPolicyGroup(
                cat,
                FilterPolicyCategoryMap.CategoryTitle(cat),
                filtered.Where(r => r.Category == cat)
                    .OrderBy(r => r.Summary, StringComparer.OrdinalIgnoreCase)
                    .ToList()))
            .ToList();
    }

    private IReadOnlyList<FilterPolicyRow> BuildAllRows(
        DnsmasqServiceStatus status,
        Func<string, IReadOnlyList<string>>? currentValuesAccessor)
    {
        if (status.EffectiveConfig == null)
            return Array.Empty<FilterPolicyRow>();

        var cfg = status.EffectiveConfig;
        var src = status.EffectiveConfigSources;
        var list = new List<FilterPolicyRow>();

        list.AddRange(ProjectMulti(FilterPolicyKind.Address, "Blocking / sinkhole", Project(status, DnsmasqConfKeys.Address, currentValuesAccessor ?? (_ => cfg.AddressValues), src?.AddressValues), FacetsAddress));
        list.AddRange(ProjectMulti(FilterPolicyKind.Server, "Split DNS (forward)", Project(status, DnsmasqConfKeys.Server, currentValuesAccessor ?? (_ => cfg.ServerValues), src?.ServerValues), FacetsServer));
        list.AddRange(ProjectMulti(FilterPolicyKind.RevServer, "Reverse zone forward", Project(status, DnsmasqConfKeys.RevServer, currentValuesAccessor ?? (_ => cfg.RevServerValues), src?.RevServerValues), FacetsRevServer));
        list.AddRange(ProjectMulti(FilterPolicyKind.Local, "Local-only domain", Project(status, DnsmasqConfKeys.Local, currentValuesAccessor ?? (_ => cfg.LocalValues), src?.LocalValues), FacetsLocal));
        list.AddRange(ProjectMulti(FilterPolicyKind.RebindDomainOk, "Rebind domain exception", Project(status, DnsmasqConfKeys.RebindDomainOk, currentValuesAccessor ?? (_ => cfg.RebindDomainOkValues), src?.RebindDomainOkValues), FacetsRebind));
        list.AddRange(ProjectMulti(FilterPolicyKind.BogusNxdomain, "Bogus NXDOMAIN", Project(status, DnsmasqConfKeys.BogusNxdomain, currentValuesAccessor ?? (_ => cfg.BogusNxdomainValues), src?.BogusNxdomainValues), FacetsBogus));
        list.AddRange(ProjectMulti(FilterPolicyKind.IgnoreAddress, "Ignore answer address", Project(status, DnsmasqConfKeys.IgnoreAddress, currentValuesAccessor ?? (_ => cfg.IgnoreAddressValues), src?.IgnoreAddressValues), FacetsIgnore));
        list.AddRange(ProjectMulti(FilterPolicyKind.FilterRr, "Filter RR", Project(status, DnsmasqConfKeys.FilterRr, currentValuesAccessor ?? (_ => cfg.FilterRrValues), src?.FilterRrValues), FacetsFilterRr));
        list.AddRange(ProjectMulti(FilterPolicyKind.Alias, "Alias / rewrite", Project(status, DnsmasqConfKeys.Alias, currentValuesAccessor ?? (_ => cfg.AliasValues), src?.AliasValues), FacetsAlias));
        list.AddRange(ProjectMulti(FilterPolicyKind.Ipset, "ipset targeting", Project(status, DnsmasqConfKeys.Ipset, currentValuesAccessor ?? (_ => cfg.IpsetValues), src?.IpsetValues), FacetsIpset));
        list.AddRange(ProjectMulti(FilterPolicyKind.Nftset, "nftset targeting", Project(status, DnsmasqConfKeys.Nftset, currentValuesAccessor ?? (_ => cfg.NftsetValues), src?.NftsetValues), FacetsNftset));
        list.AddRange(ProjectMulti(FilterPolicyKind.ConnmarkAllowlist, "Connmark allowlist", Project(status, DnsmasqConfKeys.ConnmarkAllowlist, currentValuesAccessor ?? (_ => cfg.ConnmarkAllowlistValues), src?.ConnmarkAllowlistValues), FacetsConnmark));

        return list;
    }

    private IEnumerable<FilterPolicyRow> ProjectMulti(
        FilterPolicyKind kind,
        string title,
        IReadOnlyList<ProjectedMultiValueOccurrence> items,
        Func<string, IReadOnlyDictionary<string, string>> facetsFactory)
    {
        var category = FilterPolicyCategoryMap.GetCategory(kind);
        for (var i = 0; i < items.Count; i++)
        {
            var item = items[i];
            var id = $"{kind}:{item.EffectiveIndex}:{item.Source?.FilePath ?? item.DisplaySourcePath}:{item.Source?.LineNumber}:{item.Value.GetHashCode(StringComparison.Ordinal):X8}";
            var summary = _summaries.Format(kind, item.Value);
            var facets = facetsFactory(item.Value);
            yield return new FilterPolicyRow(
                id,
                item.OccurrenceId,
                category,
                kind,
                title,
                summary,
                item.Value,
                item.IsEditable,
                IsActive: true,
                item.Source,
                item.DisplaySourcePath,
                item.DisplaySourceLabel,
                item.IsDraftOnly,
                facets);
        }
    }

    private IReadOnlyList<ProjectedMultiValueOccurrence> Project(
        DnsmasqServiceStatus status,
        string optionName,
        Func<string, IReadOnlyList<string>> currentValuesAccessor,
        IReadOnlyList<ValueWithSource>? baselineValues)
    {
        return _multiValueProjection.Project(
            currentValuesAccessor(optionName),
            baselineValues,
            status.ManagedFilePath);
    }

    public IReadOnlyList<ProjectedMultiValueOccurrence> ProjectOccurrences(
        DnsmasqServiceStatus status,
        string optionName,
        IReadOnlyList<string> currentValues)
    {
        return _multiValueProjection.Project(
            currentValues,
            GetBaselineValues(status, optionName),
            status.ManagedFilePath);
    }

    private static IReadOnlyList<ValueWithSource>? GetBaselineValues(DnsmasqServiceStatus status, string optionName)
    {
        var src = status.EffectiveConfigSources;
        return optionName switch
        {
            DnsmasqConfKeys.Address => src?.AddressValues,
            DnsmasqConfKeys.Server => src?.ServerValues,
            DnsmasqConfKeys.RevServer => src?.RevServerValues,
            DnsmasqConfKeys.Local => src?.LocalValues,
            DnsmasqConfKeys.RebindDomainOk => src?.RebindDomainOkValues,
            DnsmasqConfKeys.BogusNxdomain => src?.BogusNxdomainValues,
            DnsmasqConfKeys.IgnoreAddress => src?.IgnoreAddressValues,
            DnsmasqConfKeys.FilterRr => src?.FilterRrValues,
            DnsmasqConfKeys.Alias => src?.AliasValues,
            DnsmasqConfKeys.Ipset => src?.IpsetValues,
            DnsmasqConfKeys.Nftset => src?.NftsetValues,
            DnsmasqConfKeys.ConnmarkAllowlist => src?.ConnmarkAllowlistValues,
            _ => null
        };
    }

    private static IReadOnlyDictionary<string, string> FacetsAddress(string v)
    {
        var p = AddressRuleFields.Parse(v);
        return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["matchMode"] = p.MatchMode.ToString(),
            ["responseMode"] = p.ResponseMode.ToString(),
            ["domainPath"] = p.DomainPath,
            ["responseValue"] = p.ResponseValue
        };
    }

    private static IReadOnlyDictionary<string, string> FacetsServer(string v)
    {
        var p = ServerRuleFields.Parse(v);
        return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["scoped"] = p.IsScoped.ToString(),
            ["domainPath"] = p.DomainPath,
            ["target"] = p.Target
        };
    }

    private static IReadOnlyDictionary<string, string> FacetsRevServer(string v)
    {
        var p = RevServerRuleFields.Parse(v);
        return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["cidr"] = p.Cidr,
            ["upstream"] = p.Upstream
        };
    }

    private static IReadOnlyDictionary<string, string> FacetsLocal(string v)
    {
        var p = LocalRuleFields.Parse(v);
        return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) { ["domainPath"] = p.DomainPath };
    }

    private static IReadOnlyDictionary<string, string> FacetsRebind(string v)
    {
        var p = RebindDomainOkRuleFields.Parse(v);
        return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) { ["domain"] = p.Domain };
    }

    private static IReadOnlyDictionary<string, string> FacetsBogus(string v)
    {
        var p = BogusNxdomainRuleFields.Parse(v);
        return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) { ["address"] = p.AddressOrSubnet };
    }

    private static IReadOnlyDictionary<string, string> FacetsIgnore(string v)
    {
        var p = IgnoreAddressRuleFields.Parse(v);
        return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) { ["address"] = p.AddressOrSubnet };
    }

    private static IReadOnlyDictionary<string, string> FacetsFilterRr(string v)
    {
        var p = FilterRrRuleFields.Parse(v);
        return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["match"] = p.MatchValue,
            ["rr"] = p.RecordType
        };
    }

    private static IReadOnlyDictionary<string, string> FacetsAlias(string v)
    {
        var p = AliasRuleFields.Parse(v);
        return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["left"] = p.Left,
            ["right"] = p.Right
        };
    }

    private static IReadOnlyDictionary<string, string> FacetsIpset(string v)
    {
        var p = IpsetRuleFields.Parse(v);
        return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["domainPath"] = p.DomainPath,
            ["set"] = p.SetName
        };
    }

    private static IReadOnlyDictionary<string, string> FacetsNftset(string v)
    {
        var p = NftsetRuleFields.Parse(v);
        return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["domainPath"] = p.DomainPath,
            ["specs"] = p.SetSpecs
        };
    }

    private static IReadOnlyDictionary<string, string> FacetsConnmark(string v)
    {
        var p = ConnmarkAllowlistRuleFields.Parse(v);
        return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["mark"] = p.Mark,
            ["domain"] = p.Domain
        };
    }

    private static IReadOnlyList<ValueWithSource> Merge(
        IReadOnlyList<string>? values,
        IReadOnlyList<ValueWithSource>? sourced)
    {
        if (values == null || values.Count == 0)
            return Array.Empty<ValueWithSource>();
        if (sourced != null && sourced.Count == values.Count)
            return sourced;
        return values.Select(v => new ValueWithSource(v, null)).ToList();
    }

    private static IReadOnlyList<FilterPolicyRow> ApplyQuery(IReadOnlyList<FilterPolicyRow> rows, FilterPolicyQueryState query)
    {
        IEnumerable<FilterPolicyRow> q = rows;

        if (query.Category is { } cat)
            q = q.Where(r => r.Category == cat);

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var s = query.Search.Trim();
            q = q.Where(r =>
                r.Title.Contains(s, StringComparison.OrdinalIgnoreCase) ||
                r.Summary.Contains(s, StringComparison.OrdinalIgnoreCase) ||
                r.RawValue.Contains(s, StringComparison.OrdinalIgnoreCase) ||
                (r.SourceLabel?.Contains(s, StringComparison.OrdinalIgnoreCase) ?? false) ||
                (r.SourcePath?.Contains(s, StringComparison.OrdinalIgnoreCase) ?? false));
        }

        if (!string.IsNullOrWhiteSpace(query.SourcePathFilter))
        {
            var p = query.SourcePathFilter.Trim();
            q = q.Where(r => string.Equals(r.SourcePath, p, StringComparison.OrdinalIgnoreCase));
        }

        if (query.EditableFilter is { } ed)
            q = q.Where(r => r.IsEditable == ed);

        if (query.ActiveFilter is { } act)
            q = q.Where(r => r.IsActive == act);

        return q.ToList();
    }
}
