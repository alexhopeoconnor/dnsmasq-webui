namespace DnsmasqWebUI.Models.Hosts;

/// <summary>Labels for hosts <see cref="HostsPageRow.SourcePath"/> in UI (e.g. file filter dropdown).</summary>
public static class HostsSourcePathDisplay
{
    public static string FormatOptionLabel(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return path;

        var trimmed = path.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        var name = Path.GetFileName(trimmed);
        return string.IsNullOrEmpty(name) ? path : name;
    }

    /// <summary>
    /// Builds grouped menu data: paths grouped by <see cref="HostsRowSourceKind"/> with per-file record counts
    /// (non-comment lines). Ensures <paramref name="managedHostsPathAlwaysShow"/> appears even when there are no managed rows yet.
    /// </summary>
    public static IReadOnlyList<HostsFileFilterMenuGroup> BuildFileFilterMenuGroups(
        IReadOnlyList<HostsPageRow> rows,
        string? managedHostsPathAlwaysShow)
    {
        var byPath = rows
            .Where(r => !string.IsNullOrWhiteSpace(r.SourcePath))
            .GroupBy(r => r.SourcePath.Trim(), StringComparer.OrdinalIgnoreCase)
            .Select(g => (
                Path: g.Key,
                Kind: g.First().SourceKind,
                Count: g.Count(r => !r.IsComment)))
            .ToList();

        var managedTrim = managedHostsPathAlwaysShow?.Trim();
        if (!string.IsNullOrEmpty(managedTrim)
            && !byPath.Any(e => string.Equals(e.Path, managedTrim, StringComparison.OrdinalIgnoreCase)))
            byPath.Add((managedTrim, HostsRowSourceKind.Managed, 0));

        return byPath
            .GroupBy(e => e.Kind)
            .OrderBy(g => SourceKindOrder(g.Key))
            .Select(g =>
            {
                var files = g.OrderBy(e => e.Path, StringComparer.OrdinalIgnoreCase)
                    .Select(e => new HostsFileFilterMenuItem(e.Path, FormatOptionLabel(e.Path), e.Count))
                    .ToList();
                return new HostsFileFilterMenuGroup(
                    g.Key,
                    GroupMenuTitle(g.Key),
                    files);
            })
            .ToList();
    }

    public static string GroupMenuTitle(HostsRowSourceKind kind) => kind switch
    {
        HostsRowSourceKind.Managed => "Managed hosts",
        HostsRowSourceKind.SystemHosts => "System hosts",
        HostsRowSourceKind.AddnHosts => "Additional hosts",
        HostsRowSourceKind.Hostsdir => "Hosts directory",
        _ => "Hosts files"
    };

    public static string GroupSummaryPrefix(HostsRowSourceKind kind) => kind switch
    {
        HostsRowSourceKind.Managed => "Managed",
        HostsRowSourceKind.SystemHosts => "System",
        HostsRowSourceKind.AddnHosts => "Additional",
        HostsRowSourceKind.Hostsdir => "Hosts dir",
        _ => "Hosts"
    };

    private static int SourceKindOrder(HostsRowSourceKind kind) => kind switch
    {
        HostsRowSourceKind.Managed => 0,
        HostsRowSourceKind.SystemHosts => 1,
        HostsRowSourceKind.AddnHosts => 2,
        HostsRowSourceKind.Hostsdir => 3,
        _ => 99
    };

}
