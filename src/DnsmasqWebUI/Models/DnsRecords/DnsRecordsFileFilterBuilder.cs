using System.Linq;
using DnsmasqWebUI.Models.Ui;

namespace DnsmasqWebUI.Models.DnsRecords;

/// <summary>Source file dropdown for the DNS records page (mirrors <see cref="Hosts.HostsFileFilterBuilder"/>).</summary>
public sealed class DnsRecordsFileFilterBuilder : IGroupedSelectBuilder<string>, IGroupedSelectTriggerSummary<string>
{
    private readonly IReadOnlyList<DnsRecordRow> _rows;
    private readonly string? _managedConfigPath;

    public DnsRecordsFileFilterBuilder(IReadOnlyList<DnsRecordRow> rows, string? managedConfigPath)
    {
        _rows = rows;
        _managedConfigPath = managedConfigPath;
    }

    public string TriggerTitle =>
        "Show records from all config files, or only one file";

    public string TriggerAriaLabel => "Filter by config file";

    public string MenuAriaLabel => "Config files";

    public GroupedSelectModel<string> Build()
    {
        var byPath = _rows
            .Where(r => !string.IsNullOrWhiteSpace(r.SourcePath))
            .GroupBy(r => r.SourcePath!.Trim(), StringComparer.OrdinalIgnoreCase)
            .Select(g => new
            {
                Path = g.Key,
                Managed = IsManagedPath(g.Key),
                Count = g.Count()
            })
            .ToList();

        var sections = byPath
            .GroupBy(x => x.Managed)
            .OrderBy(g => g.Key ? 0 : 1)
            .Select(g => new GroupedSelectSection<string>(
                Label: g.Key ? "Managed config" : "Other config files",
                Options: g
                    .OrderBy(x => x.Path, StringComparer.OrdinalIgnoreCase)
                    .Select(x => new GroupedSelectOption<string>(
                        Value: x.Path,
                        Label: FormatOptionLabel(x.Path),
                        Count: x.Count,
                        Title: x.Path))
                    .ToList(),
                Kind: g.Key ? "managed" : "other",
                Order: g.Key ? 0 : 1))
            .ToList();

        return new GroupedSelectModel<string>(
            AllOption: new GroupedSelectOption<string>(
                Value: "",
                Label: "All files",
                Count: _rows.Count,
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

    private static string SummaryPrefix(string? kind) => kind switch
    {
        "managed" => "Managed",
        _ => "Config"
    };

    private bool IsManagedPath(string path)
    {
        var managed = _managedConfigPath?.Trim();
        return !string.IsNullOrWhiteSpace(managed)
            && string.Equals(path, managed, StringComparison.OrdinalIgnoreCase);
    }
}
