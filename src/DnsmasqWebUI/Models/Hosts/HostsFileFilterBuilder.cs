using DnsmasqWebUI.Models.Ui;

namespace DnsmasqWebUI.Models.Hosts;

public sealed class HostsFileFilterBuilder : IGroupedSelectBuilder<string>, IGroupedSelectTriggerSummary<string>
{
    private readonly IReadOnlyList<HostsPageRow> _rows;
    private readonly string? _managedHostsPath;

    public HostsFileFilterBuilder(IReadOnlyList<HostsPageRow> rows, string? managedHostsPath)
    {
        _rows = rows;
        _managedHostsPath = managedHostsPath;
    }

    public string TriggerTitle =>
        "Show entries from all loaded hosts files, or only one file";

    public string TriggerAriaLabel => "Filter by hosts file";

    public string MenuAriaLabel => "Hosts files";

    public GroupedSelectModel<string> Build()
    {
        var byPath = _rows
            .Where(r => !string.IsNullOrWhiteSpace(r.SourcePath))
            .GroupBy(r => r.SourcePath.Trim(), StringComparer.OrdinalIgnoreCase)
            .Select(g => new
            {
                Path = g.Key,
                SourceKind = g.First().SourceKind,
                Count = g.Count(r => !r.IsComment)
            })
            .ToList();

        var managed = _managedHostsPath?.Trim();
        if (!string.IsNullOrWhiteSpace(managed)
            && !byPath.Any(x => string.Equals(x.Path, managed, StringComparison.OrdinalIgnoreCase)))
        {
            byPath.Add(new
            {
                Path = managed,
                SourceKind = HostsRowSourceKind.Managed,
                Count = 0
            });
        }

        var sections = byPath
            .GroupBy(x => x.SourceKind)
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
                Label: "All files",
                Count: _rows.Count(r => !r.IsComment),
                Title: null),
            Sections: sections);
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
                    AccessibleFullText: "All files",
                    Kind: GroupedSelectTriggerSummaryKind.AllSources,
                    CategoryPrefix: null,
                    PrimaryLabel: "All files",
                    SecondaryMeta: null);
            }

            return new GroupedSelectTriggerSummary(
                AccessibleFullText: $"All files · {count} records",
                Kind: GroupedSelectTriggerSummaryKind.AllSources,
                CategoryPrefix: null,
                PrimaryLabel: "All files",
                SecondaryMeta: $"{count} records");
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

    private static int SourceOrder(HostsRowSourceKind kind) => kind switch
    {
        HostsRowSourceKind.Managed => 0,
        HostsRowSourceKind.SystemHosts => 1,
        HostsRowSourceKind.AddnHosts => 2,
        HostsRowSourceKind.Hostsdir => 3,
        _ => 99
    };

    private static string GroupTitle(HostsRowSourceKind kind) => kind switch
    {
        HostsRowSourceKind.Managed => "Managed hosts",
        HostsRowSourceKind.SystemHosts => "System hosts",
        HostsRowSourceKind.AddnHosts => "Additional hosts",
        HostsRowSourceKind.Hostsdir => "Hosts directory",
        _ => "Hosts files"
    };

    private static string GroupKind(HostsRowSourceKind kind) => kind switch
    {
        HostsRowSourceKind.Managed => "managed",
        HostsRowSourceKind.SystemHosts => "system",
        HostsRowSourceKind.AddnHosts => "addn",
        HostsRowSourceKind.Hostsdir => "hostsdir",
        _ => "other"
    };

    private static string SummaryPrefix(string? kind) => kind switch
    {
        "managed" => "Managed",
        "system" => "System",
        "addn" => "Additional",
        "hostsdir" => "Hosts dir",
        _ => "Hosts"
    };
}
