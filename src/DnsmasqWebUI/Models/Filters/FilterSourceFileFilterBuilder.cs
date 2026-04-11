using DnsmasqWebUI.Models.Ui;

namespace DnsmasqWebUI.Models.Filters;

public sealed class FilterSourceFileFilterBuilder : IGroupedSelectBuilder<string>, IGroupedSelectTriggerSummary<string>
{
    private readonly IReadOnlyList<FilterPolicyRow> _rows;
    private readonly string? _managedConfigPath;

    public FilterSourceFileFilterBuilder(IReadOnlyList<FilterPolicyRow> rows, string? managedConfigPath)
    {
        _rows = rows;
        _managedConfigPath = managedConfigPath;
    }

    public string TriggerTitle =>
        "Show rules from all config sources, or only one file";

    public string TriggerAriaLabel => "Filter by config file";

    public string MenuAriaLabel => "Config files";

    public GroupedSelectModel<string> Build()
    {
        var byPath = _rows
            .Select(r => r.Source?.FilePath)
            .Where(p => !string.IsNullOrWhiteSpace(p))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Select(path => path!.Trim())
            .Select(path => new
            {
                Path = path,
                Kind = KindForPath(path),
                Count = _rows.Count(r => string.Equals(r.Source?.FilePath, path, StringComparison.OrdinalIgnoreCase))
            })
            .ToList();

        var managed = _managedConfigPath?.Trim();
        if (!string.IsNullOrWhiteSpace(managed)
            && !byPath.Any(x => string.Equals(x.Path, managed, StringComparison.OrdinalIgnoreCase)))
        {
            byPath.Add(new
            {
                Path = managed,
                Kind = FilterPolicySourceKind.Managed,
                Count = 0
            });
        }

        var sections = byPath
            .GroupBy(x => x.Kind)
            .OrderBy(g => SourceOrder(g.Key))
            .Select(g => new GroupedSelectSection<string>(
                Label: GroupTitle(g.Key),
                Kind: GroupKind(g.Key),
                Order: SourceOrder(g.Key),
                Options: g
                    .OrderBy(x => x.Path, StringComparer.OrdinalIgnoreCase)
                    .Select(x => new GroupedSelectOption<string>(
                        Value: x.Path,
                        Label: FormatOptionLabel(x.Path),
                        Count: x.Count,
                        Title: x.Path))
                    .ToList()))
            .ToList();

        return new GroupedSelectModel<string>(
            AllOption: new GroupedSelectOption<string>(
                Value: "",
                Label: "All sources",
                Count: _rows.Count,
                Title: null),
            Sections: sections);
    }

    private FilterPolicySourceKind KindForPath(string path)
    {
        var m = _managedConfigPath?.Trim();
        if (!string.IsNullOrWhiteSpace(m) && string.Equals(path, m, StringComparison.OrdinalIgnoreCase))
            return FilterPolicySourceKind.Managed;
        return FilterPolicySourceKind.Included;
    }

    public string GetSummaryText(string? selectedValue) =>
        GetTriggerSummary(selectedValue).AccessibleFullText;

    public GroupedSelectTriggerSummary GetTriggerSummary(string? selectedValue)
    {
        var model = Build();
        if (string.IsNullOrWhiteSpace(selectedValue))
        {
            var count = model.AllOption?.Count ?? 0;
            if (count == 0)
            {
                return new GroupedSelectTriggerSummary(
                    AccessibleFullText: "All sources",
                    Kind: GroupedSelectTriggerSummaryKind.AllSources,
                    CategoryPrefix: null,
                    PrimaryLabel: "All sources",
                    SecondaryMeta: null);
            }

            return new GroupedSelectTriggerSummary(
                AccessibleFullText: $"All sources · {count} rules",
                Kind: GroupedSelectTriggerSummaryKind.AllSources,
                CategoryPrefix: null,
                PrimaryLabel: "All sources",
                SecondaryMeta: $"{count} rules");
        }

        var selected = selectedValue.Trim();
        foreach (var section in model.Sections)
        {
            foreach (var option in section.Options)
            {
                if (string.Equals(option.Value, selected, StringComparison.OrdinalIgnoreCase))
                {
                    var count = option.Count ?? 0;
                    var prefix = SummaryPrefix(section.Kind);
                    return new GroupedSelectTriggerSummary(
                        AccessibleFullText: $"{prefix} · {option.Label} ({count})",
                        Kind: GroupedSelectTriggerSummaryKind.SingleSource,
                        CategoryPrefix: prefix,
                        PrimaryLabel: option.Label,
                        SecondaryMeta: count.ToString());
                }
            }
        }

        var fallback = FormatOptionLabel(selected);
        return new GroupedSelectTriggerSummary(
            AccessibleFullText: fallback,
            Kind: GroupedSelectTriggerSummaryKind.UnknownSource,
            CategoryPrefix: null,
            PrimaryLabel: fallback,
            SecondaryMeta: null);
    }

    private static string FormatOptionLabel(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return path;

        var trimmed = path.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        var name = Path.GetFileName(trimmed);
        return string.IsNullOrEmpty(name) ? path : name;
    }

    private static int SourceOrder(FilterPolicySourceKind kind) => kind switch
    {
        FilterPolicySourceKind.Managed => 0,
        FilterPolicySourceKind.Included => 1,
        _ => 99
    };

    private static string GroupTitle(FilterPolicySourceKind kind) => kind switch
    {
        FilterPolicySourceKind.Managed => "Managed config",
        FilterPolicySourceKind.Included => "Included config",
        _ => "Config files"
    };

    private static string GroupKind(FilterPolicySourceKind kind) => kind switch
    {
        FilterPolicySourceKind.Managed => "managed",
        FilterPolicySourceKind.Included => "included",
        _ => "other"
    };

    private static string SummaryPrefix(string? kind) => kind switch
    {
        "managed" => "Managed",
        "included" => "Included",
        _ => "Config"
    };
}
