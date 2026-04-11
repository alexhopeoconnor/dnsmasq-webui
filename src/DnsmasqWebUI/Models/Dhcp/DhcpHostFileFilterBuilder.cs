using DnsmasqWebUI.Models.Dhcp.Ui;
using DnsmasqWebUI.Models.Ui;

namespace DnsmasqWebUI.Models.Dhcp;

/// <summary>Builds grouped file filter options for dhcp-host rows (mirrors <see cref="Hosts.HostsFileFilterBuilder"/>).</summary>
public sealed class DhcpHostFileFilterBuilder : IGroupedSelectBuilder<string>, IGroupedSelectTriggerSummary<string>
{
    private readonly IReadOnlyList<DhcpHostPageRow> _rows;
    private readonly string? _managedConfigPath;

    public DhcpHostFileFilterBuilder(IReadOnlyList<DhcpHostPageRow> rows, string? managedConfigPath)
    {
        _rows = rows;
        _managedConfigPath = managedConfigPath;
    }

    public string TriggerTitle => "Show static host lines from all config sources, or only one file";

    public string TriggerAriaLabel => "Filter by config file";

    public string MenuAriaLabel => "DHCP host sources";

    public GroupedSelectModel<string> Build()
    {
        var byPath = _rows
            .Where(r => !string.IsNullOrWhiteSpace(r.SourcePath))
            .GroupBy(r => r.SourcePath.Trim(), StringComparer.OrdinalIgnoreCase)
            .Select(g => new
            {
                Path = g.Key,
                SourceKind = g.First().SourceKind,
                Count = g.Count(r => r.IsActive && !r.Entry.IsComment)
            })
            .ToList();

        var managed = _managedConfigPath?.Trim();
        if (!string.IsNullOrWhiteSpace(managed)
            && !byPath.Any(x => string.Equals(x.Path, managed, StringComparison.OrdinalIgnoreCase)))
        {
            byPath.Add(new
            {
                Path = managed,
                SourceKind = DhcpSourceKind.Managed,
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
                Label: "All sources",
                Count: _rows.Count(r => r.IsActive && !r.Entry.IsComment),
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
                    AccessibleFullText: "All sources",
                    Kind: GroupedSelectTriggerSummaryKind.AllSources,
                    CategoryPrefix: null,
                    PrimaryLabel: "All sources",
                    SecondaryMeta: null);
            }

            return new GroupedSelectTriggerSummary(
                AccessibleFullText: $"All sources · {count} rows",
                Kind: GroupedSelectTriggerSummaryKind.AllSources,
                CategoryPrefix: null,
                PrimaryLabel: "All sources",
                SecondaryMeta: $"{count} rows");
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

    private static int SourceOrder(DhcpSourceKind kind) => kind switch
    {
        DhcpSourceKind.Managed => 0,
        DhcpSourceKind.MainConfig => 1,
        DhcpSourceKind.DhcpHostsFile => 2,
        DhcpSourceKind.DhcpHostsDir => 3,
        DhcpSourceKind.OtherIncluded => 4,
        _ => 99
    };

    private static string GroupTitle(DhcpSourceKind kind) => kind switch
    {
        DhcpSourceKind.Managed => "Managed config",
        DhcpSourceKind.MainConfig => "Main config",
        DhcpSourceKind.DhcpHostsFile => "dhcp-hostsfile",
        DhcpSourceKind.DhcpHostsDir => "dhcp-hostsdir",
        DhcpSourceKind.OtherIncluded => "Other included files",
        _ => "Sources"
    };

    private static string GroupKind(DhcpSourceKind kind) => kind switch
    {
        DhcpSourceKind.Managed => "managed",
        DhcpSourceKind.MainConfig => "main",
        DhcpSourceKind.DhcpHostsFile => "hostsfile",
        DhcpSourceKind.DhcpHostsDir => "hostsdir",
        DhcpSourceKind.OtherIncluded => "other",
        _ => "other"
    };

    private static string SummaryPrefix(string? kind) => kind switch
    {
        "managed" => "Managed",
        "main" => "Main",
        "hostsfile" => "Hosts file",
        "hostsdir" => "Hosts dir",
        _ => "Source"
    };
}
