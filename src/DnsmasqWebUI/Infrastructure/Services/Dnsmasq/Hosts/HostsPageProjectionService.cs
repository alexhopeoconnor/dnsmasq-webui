using DnsmasqWebUI.Infrastructure.Services.Dnsmasq.Hosts.Abstractions;
using DnsmasqWebUI.Models.Hosts;

namespace DnsmasqWebUI.Infrastructure.Services.Dnsmasq.Hosts;

public sealed class HostsPageProjectionService : IHostsPageProjectionService
{
    public IReadOnlyList<HostsPageGroup> BuildGroups(IReadOnlyList<HostsPageRow> rows, HostsPageQueryState query)
    {
        if (rows.Count == 0)
            return Array.Empty<HostsPageGroup>();

        IEnumerable<HostsPageRow> filtered = rows;

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var s = query.Search.Trim();
            filtered = filtered.Where(r =>
                r.Address.Contains(s, StringComparison.OrdinalIgnoreCase) ||
                r.Names.Any(n => n.Contains(s, StringComparison.OrdinalIgnoreCase)) ||
                r.EffectiveNames.Any(n => n.Contains(s, StringComparison.OrdinalIgnoreCase)) ||
                r.SourcePath.Contains(s, StringComparison.OrdinalIgnoreCase));
        }

        if (query.SourceKindFilter is { } sk)
            filtered = filtered.Where(r => r.SourceKind == sk);

        if (query.EditableFilter is { } ed)
            filtered = filtered.Where(r => r.IsEditable == ed);

        if (query.ActiveFilter is { } act)
            filtered = filtered.Where(r => r.IsActive == act);

        // When dnsmasq ignores system hosts (e.g. no-hosts), do not show those rows in the table.
        filtered = filtered.Where(r =>
            !(r.SourceKind == HostsRowSourceKind.SystemHosts && !r.IsActive));

        var list = filtered.ToList();
        if (list.Count == 0)
            return Array.Empty<HostsPageGroup>();

        return BuildSourceGroups(list, query);
    }

    private static IReadOnlyList<HostsPageGroup> BuildSourceGroups(List<HostsPageRow> rows, HostsPageQueryState query)
    {
        var orderedGroups = rows
            .GroupBy(r => (r.SourceKind, r.SourcePath))
            .OrderBy(g => GetSourceOrder(g.Key.SourceKind))
            .ThenBy(g => g.Key.SourcePath, StringComparer.OrdinalIgnoreCase);

        var result = new List<HostsPageGroup>();
        foreach (var g in orderedGroups)
        {
            var sortedRows = SortRows(g, query.Sort, query.Descending).ToList();
            if (sortedRows.Count == 0)
                continue;

            var first = sortedRows[0];
            var title = TitleForSourceKind(first.SourceKind);
            var subtitle = BuildSubtitle(first);
            var inactiveReason = first.SourceKind == HostsRowSourceKind.SystemHosts && !first.IsActive
                ? first.InactiveReason
                : null;

            var isSourceEditable = first.SourceKind == HostsRowSourceKind.Managed
                || sortedRows.Any(r => r.IsEditable);

            result.Add(new HostsPageGroup(
                Key: $"{first.SourceKind}:{first.SourcePath}",
                Title: title,
                Subtitle: subtitle,
                SourceKind: first.SourceKind,
                GroupContainsEditableRows: sortedRows.Any(r => r.IsEditable),
                IsActive: sortedRows.Any(r => r.IsActive),
                InactiveReason: inactiveReason,
                VisibleRowCount: sortedRows.Count,
                Rows: sortedRows,
                IsSourceEditable: isSourceEditable));
        }

        return result;
    }

    private static IEnumerable<HostsPageRow> SortRows(IEnumerable<HostsPageRow> rows, HostsSortMode sort, bool descending)
    {
        IEnumerable<HostsPageRow> ordered = sort switch
        {
            HostsSortMode.Name => rows.OrderBy(
                x => x.Names.FirstOrDefault() ?? "",
                StringComparer.OrdinalIgnoreCase),
            HostsSortMode.EffectiveName => rows.OrderBy(
                x => x.EffectiveNames.FirstOrDefault() ?? "",
                StringComparer.OrdinalIgnoreCase),
            HostsSortMode.LineNumber => rows.OrderBy(x => x.LineNumber),
            _ => rows.OrderBy(x => x.Address, StringComparer.OrdinalIgnoreCase)
        };

        return descending ? ordered.Reverse() : ordered;
    }

    private static int GetSourceOrder(HostsRowSourceKind kind) => kind switch
    {
        HostsRowSourceKind.Managed => 0,
        HostsRowSourceKind.SystemHosts => 1,
        HostsRowSourceKind.AddnHosts => 2,
        HostsRowSourceKind.Hostsdir => 3,
        _ => 99
    };

    private static string TitleForSourceKind(HostsRowSourceKind kind) => kind switch
    {
        HostsRowSourceKind.Managed => "Managed hosts",
        HostsRowSourceKind.SystemHosts => "System hosts",
        HostsRowSourceKind.AddnHosts => "Additional hosts file",
        HostsRowSourceKind.Hostsdir => "Hosts directory file",
        _ => "Hosts"
    };

    private static string? BuildSubtitle(HostsPageRow first)
    {
        if (string.IsNullOrWhiteSpace(first.SourcePath))
            return null;
        if (!first.IsActive && !string.IsNullOrWhiteSpace(first.InactiveReason))
            return $"{first.SourcePath} — {first.InactiveReason}";
        return first.SourcePath;
    }
}
