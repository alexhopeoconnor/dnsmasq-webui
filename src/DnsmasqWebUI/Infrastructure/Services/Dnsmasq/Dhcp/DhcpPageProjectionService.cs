using System.Net;
using System.Net.Sockets;
using DnsmasqWebUI.Infrastructure.Serialization.OptionHandlers.Abstractions;
using DnsmasqWebUI.Infrastructure.Services.Dnsmasq.Dhcp.Abstractions;
using DnsmasqWebUI.Infrastructure.Services.EffectiveConfig.Metadata;
using DnsmasqWebUI.Models.Dhcp;
using DnsmasqWebUI.Models.Dhcp.Ui;
using DnsmasqWebUI.Models.Dnsmasq;
using DnsmasqWebUI.Models.Dnsmasq.EffectiveConfig;

namespace DnsmasqWebUI.Infrastructure.Services.Dnsmasq.Dhcp;

public sealed class DhcpPageProjectionService : IDhcpPageProjectionService
{
    public IReadOnlyList<DhcpHostPageRow> BuildHostRows(
        DnsmasqServiceStatus status,
        IReadOnlyList<string> effectiveDhcpHostValues,
        IStructuredOptionValueHandler<DhcpHostEntry> handler,
        IReadOnlyList<LeaseEntry>? leases = null)
    {
        var baseline = status.EffectiveConfigSources?.DhcpHostLines;
        var usedBaseline = baseline != null ? new bool[baseline.Count] : Array.Empty<bool>();

        var rows = new List<DhcpHostPageRow>(effectiveDhcpHostValues.Count);
        for (var i = 0; i < effectiveDhcpHostValues.Count; i++)
        {
            var valueString = effectiveDhcpHostValues[i];
            ConfigValueSource? matchedSource = null;
            if (baseline != null)
            {
                for (var j = 0; j < baseline.Count; j++)
                {
                    if (usedBaseline[j]) continue;
                    if (string.Equals(baseline[j].Value, valueString, StringComparison.Ordinal))
                    {
                        matchedSource = baseline[j].Source;
                        usedBaseline[j] = true;
                        break;
                    }
                }
            }

            var parsedOk = handler.TryParseValue(valueString, i + 1, out var entry) && entry != null;
            var e = parsedOk
                ? entry!
                : new DhcpHostEntry
                {
                    LineNumber = i + 1,
                    IsComment = true,
                    RawLine = valueString,
                    MacAddresses = new List<string>()
                };

            e.SourcePath = matchedSource?.FilePath;
            var isUnmatchedDraft = matchedSource == null;
            var isManagedSource = matchedSource?.IsManaged == true || isUnmatchedDraft;
            var sourceKind = isUnmatchedDraft
                ? DhcpSourceKind.Managed
                : ClassifySourceKind(matchedSource!.FilePath, matchedSource.IsManaged, status);
            var isEditable = isManagedSource;
            e.IsEditable = isEditable;

            rows.Add(new DhcpHostPageRow(
                EffectiveIndex: i,
                ValueString: valueString,
                RowKey: $"dhcp-host:{i}:{Fnv1aHash(valueString)}",
                SourceKind: sourceKind,
                SourcePath: matchedSource?.FilePath ?? status.ManagedFilePath ?? "",
                IsEditable: isEditable,
                IsActive: !e.IsDeleted && !e.IsComment,
                Entry: e,
                Conflict: null,
                LinkedLease: null,
                MatchesManagedStatic: sourceKind == DhcpSourceKind.Managed && isEditable));
        }

        ApplyHostConflictsAndLeases(rows, status, leases);
        return rows;
    }

    public IReadOnlyList<DhcpHostPageGroup> BuildHostGroups(
        IReadOnlyList<DhcpHostPageRow> rows,
        DhcpPageQueryState query,
        string? managedFilePath)
    {
        IEnumerable<DhcpHostPageRow> filtered = rows;
        if (!string.IsNullOrWhiteSpace(query.SourcePathFilter))
        {
            var p = query.SourcePathFilter.Trim();
            filtered = filtered.Where(r => string.Equals(r.SourcePath, p, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var s = query.Search.Trim();
            filtered = filtered.Where(r => HostRowMatchesSearch(r, s));
        }

        var list = filtered.ToList();
        if (list.Count == 0)
            return Array.Empty<DhcpHostPageGroup>();

        var ordered = SortHostRows(list, query.Sort, query.Descending);
        var groups = ordered
            .GroupBy(r => (r.SourceKind, r.SourcePath))
            .OrderBy(g => SourceGroupOrder(g.Key.SourceKind))
            .ThenBy(g => g.Key.SourcePath, StringComparer.OrdinalIgnoreCase)
            .Select(g =>
            {
                var first = g.First();
                var title = GroupTitle(first.SourceKind);
                var subtitle = string.IsNullOrWhiteSpace(first.SourcePath) ? null : first.SourcePath;
                var editable = g.Any(x => x.IsEditable);
                return new DhcpHostPageGroup(
                    Key: $"{first.SourceKind}:{first.SourcePath}",
                    Title: title,
                    Subtitle: subtitle,
                    SourceKind: first.SourceKind,
                    IsSourceEditable: editable,
                    IsActive: g.Any(x => x.IsActive),
                    VisibleRowCount: g.Count(),
                    Rows: g.ToList());
            })
            .ToList();

        return EnsureManagedGroupFirst(groups, managedFilePath, query.SourcePathFilter);
    }

    public DhcpExternalSourcesViewModel BuildExternalSources(DnsmasqServiceStatus status)
    {
        var ec = status.EffectiveConfig;
        return new DhcpExternalSourcesViewModel(
            ReadEthers: ec?.ReadEthers ?? false,
            DhcpHostsfilePaths: ec?.DhcpHostsfilePaths ?? Array.Empty<string>(),
            DhcpHostsdirPaths: ec?.DhcpHostsdirPaths ?? Array.Empty<string>(),
            DhcpOptsfilePaths: ec?.DhcpOptsfilePaths ?? Array.Empty<string>(),
            DhcpOptsdirPaths: ec?.DhcpOptsdirPaths ?? Array.Empty<string>());
    }

    public IReadOnlyList<DhcpLeaseRowViewModel> BuildLeaseRows(
        DnsmasqServiceStatus status,
        IReadOnlyList<LeaseEntry>? leases,
        IReadOnlyList<DhcpHostPageRow> hostRows)
    {
        if (leases == null || leases.Count == 0)
            return Array.Empty<DhcpLeaseRowViewModel>();

        uint? start = null, end = null;
        if (!string.IsNullOrEmpty(status.DhcpRangeStart) &&
            IPAddress.TryParse(status.DhcpRangeStart, out var sip) &&
            sip.AddressFamily == AddressFamily.InterNetwork)
            start = ToUint(sip);
        if (!string.IsNullOrEmpty(status.DhcpRangeEnd) &&
            IPAddress.TryParse(status.DhcpRangeEnd, out var eip) &&
            eip.AddressFamily == AddressFamily.InterNetwork)
            end = ToUint(eip);

        var result = new List<DhcpLeaseRowViewModel>(leases.Count);
        foreach (var lease in leases)
        {
            var hasManaged = hostRows.Any(r =>
                r.IsEditable &&
                !r.Entry.IsComment &&
                r.Entry.MacAddresses.Any(m => MacEquals(m, lease.Mac)));
            var hasAny = hostRows.Any(r =>
                !r.Entry.IsComment &&
                r.Entry.MacAddresses.Any(m => MacEquals(m, lease.Mac)));
            var addrMatch = hostRows.Any(r =>
                !r.Entry.IsComment &&
                !string.IsNullOrWhiteSpace(r.Entry.Address) &&
                string.Equals(r.Entry.Address?.Trim(), lease.Address?.Trim(), StringComparison.OrdinalIgnoreCase));

            var inRange = false;
            string? rangeCtx = null;
            if (start.HasValue && end.HasValue &&
                IPAddress.TryParse(lease.Address, out var lip) &&
                lip.AddressFamily == AddressFamily.InterNetwork)
            {
                var u = ToUint(lip);
                var lo = Math.Min(start.Value, end.Value);
                var hi = Math.Max(start.Value, end.Value);
                inRange = u >= lo && u <= hi;
                rangeCtx = inRange ? "Inside first IPv4 dhcp-range" : "Outside first IPv4 dhcp-range";
            }
            else if (start == null || end == null)
                rangeCtx = "No IPv4 range detected";
            else
                rangeCtx = "Non-IPv4 or unparseable lease address";

            result.Add(new DhcpLeaseRowViewModel(
                lease,
                hasManaged,
                hasAny,
                addrMatch,
                inRange,
                rangeCtx));
        }

        return result;
    }

    public IReadOnlyList<DhcpClassificationRuleRow> BuildClassificationRules(DnsmasqServiceStatus status)
    {
        var ec = status.EffectiveConfig;
        if (ec == null)
            return Array.Empty<DhcpClassificationRuleRow>();

        var src = status.EffectiveConfigSources;
        var list = new List<DhcpClassificationRuleRow>();
        var order = 0;

        void AddFromStrings(string key, string label, IReadOnlyList<string> values)
        {
            foreach (var item in values)
            {
                order++;
                list.Add(new DhcpClassificationRuleRow(order, key, label, item, null, false));
            }
        }

        if (src == null)
        {
            AddFromStrings(DnsmasqConfKeys.DhcpMatch, "dhcp-match", ec.DhcpMatchValues);
            AddFromStrings(DnsmasqConfKeys.DhcpMac, "dhcp-mac", ec.DhcpMacValues);
            AddFromStrings(DnsmasqConfKeys.DhcpNameMatch, "dhcp-name-match", ec.DhcpNameMatchValues);
            AddFromStrings(DnsmasqConfKeys.DhcpVendorclass, "dhcp-vendorclass", ec.DhcpVendorclassValues);
            AddFromStrings(DnsmasqConfKeys.DhcpUserclass, "dhcp-userclass", ec.DhcpUserclassValues);
            AddFromStrings(DnsmasqConfKeys.TagIf, "tag-if", ec.TagIfValues);
            AddFromStrings(DnsmasqConfKeys.DhcpIgnore, "dhcp-ignore", ec.DhcpIgnoreValues);
            return list;
        }

        void Add(string key, string label, IReadOnlyList<ValueWithSource> items)
        {
            foreach (var item in items)
            {
                order++;
                list.Add(new DhcpClassificationRuleRow(
                    order,
                    key,
                    label,
                    item.Value,
                    item.Source?.FilePath,
                    item.Source?.IsManaged == true));
            }
        }

        Add(DnsmasqConfKeys.DhcpMatch, "dhcp-match", src.DhcpMatchValues);
        Add(DnsmasqConfKeys.DhcpMac, "dhcp-mac", src.DhcpMacValues);
        Add(DnsmasqConfKeys.DhcpNameMatch, "dhcp-name-match", src.DhcpNameMatchValues);
        Add(DnsmasqConfKeys.DhcpVendorclass, "dhcp-vendorclass", src.DhcpVendorclassValues);
        Add(DnsmasqConfKeys.DhcpUserclass, "dhcp-userclass", src.DhcpUserclassValues);
        Add(DnsmasqConfKeys.TagIf, "tag-if", src.TagIfValues);
        Add(DnsmasqConfKeys.DhcpIgnore, "dhcp-ignore", src.DhcpIgnoreValues);
        return list;
    }

    /// <summary>Recompute conflicts and lease links after row mutation.</summary>
    public static void ApplyHostConflictsAndLeases(
        IList<DhcpHostPageRow> rows,
        DnsmasqServiceStatus? _,
        IReadOnlyList<LeaseEntry>? leases)
    {
        var activeRows = rows.Where(r => r.IsActive && !r.Entry.IsComment).ToList();
        var macCounts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var addrCounts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (var r in activeRows)
        {
            foreach (var mac in r.Entry.MacAddresses.Where(m => !string.IsNullOrWhiteSpace(m)))
            {
                var k = mac.Trim();
                macCounts[k] = macCounts.TryGetValue(k, out var c) ? c + 1 : 1;
            }

            var a = r.Entry.Address?.Trim();
            if (!string.IsNullOrEmpty(a))
                addrCounts[a] = addrCounts.TryGetValue(a, out var c2) ? c2 + 1 : 1;
        }

        var leaseByMac = new Dictionary<string, LeaseEntry>(StringComparer.OrdinalIgnoreCase);
        if (leases != null)
        {
            foreach (var l in leases)
            {
                if (!string.IsNullOrWhiteSpace(l.Mac) && !leaseByMac.ContainsKey(l.Mac.Trim()))
                    leaseByMac[l.Mac.Trim()] = l;
            }
        }

        for (var i = 0; i < rows.Count; i++)
        {
            var r = rows[i];
            if (!r.IsActive || r.Entry.IsComment)
            {
                rows[i] = r with { Conflict = null, LinkedLease = null };
                continue;
            }

            var dupMac = r.Entry.MacAddresses.Any(m =>
            {
                var k = m.Trim();
                return k.Length > 0 && macCounts.TryGetValue(k, out var n) && n > 1;
            });
            var addr = r.Entry.Address?.Trim();
            var dupAddr = !string.IsNullOrEmpty(addr) && addrCounts.TryGetValue(addr, out var an) && an > 1;

            LeaseEntry? link = null;
            foreach (var mac in r.Entry.MacAddresses)
            {
                if (string.IsNullOrWhiteSpace(mac)) continue;
                if (leaseByMac.TryGetValue(mac.Trim(), out var le))
                {
                    link = le;
                    break;
                }
            }

            var leaseAddrMismatch = link != null &&
                                    !string.IsNullOrWhiteSpace(r.Entry.Address) &&
                                    !string.IsNullOrWhiteSpace(link.Address) &&
                                    !string.Equals(r.Entry.Address.Trim(), link.Address.Trim(), StringComparison.OrdinalIgnoreCase);

            var c = (dupMac || dupAddr || leaseAddrMismatch)
                ? new DhcpHostConflictInfo(dupMac, dupAddr, leaseAddrMismatch, false)
                : null;

            rows[i] = r with { Conflict = c, LinkedLease = link };
        }
    }

    private static IReadOnlyList<DhcpHostPageGroup> EnsureManagedGroupFirst(
        IReadOnlyList<DhcpHostPageGroup> groups,
        string? managedFilePath,
        string? sourcePathFilter)
    {
        if (string.IsNullOrWhiteSpace(managedFilePath))
            return groups;

        var path = managedFilePath.Trim();
        if (groups.Any(g => g.SourceKind == DhcpSourceKind.Managed))
            return groups;

        if (!string.IsNullOrWhiteSpace(sourcePathFilter)
            && !string.Equals(sourcePathFilter.Trim(), path, StringComparison.OrdinalIgnoreCase))
            return groups;

        var placeholder = new DhcpHostPageGroup(
            Key: $"{DhcpSourceKind.Managed}:{path}",
            Title: "Managed config",
            Subtitle: path,
            SourceKind: DhcpSourceKind.Managed,
            IsSourceEditable: true,
            IsActive: true,
            VisibleRowCount: 0,
            Rows: Array.Empty<DhcpHostPageRow>());

        return new[] { placeholder }.Concat(groups).ToList();
    }

    private static IEnumerable<DhcpHostPageRow> SortHostRows(
        List<DhcpHostPageRow> rows,
        DhcpHostSortMode sort,
        bool descending)
    {
        IEnumerable<DhcpHostPageRow> ordered = sort switch
        {
            DhcpHostSortMode.Mac => rows.OrderBy(r => r.Entry.MacAddresses.FirstOrDefault() ?? "", StringComparer.OrdinalIgnoreCase),
            DhcpHostSortMode.Name => rows.OrderBy(r => r.Entry.Name ?? "", StringComparer.OrdinalIgnoreCase),
            DhcpHostSortMode.Address => rows.OrderBy(r => r.Entry.Address ?? "", StringComparer.OrdinalIgnoreCase),
            _ => rows.OrderBy(r => r.EffectiveIndex)
        };
        return descending ? ordered.Reverse() : ordered;
    }

    private static bool HostRowMatchesSearch(DhcpHostPageRow r, string keyword)
    {
        if (string.IsNullOrEmpty(keyword)) return true;
        static bool Has(string? v, string k) =>
            !string.IsNullOrEmpty(v) && v.Contains(k, StringComparison.OrdinalIgnoreCase);

        if (Has(r.ValueString, keyword)) return true;
        if (Has(r.SourcePath, keyword)) return true;
        if (r.Entry.MacAddresses.Any(m => Has(m, keyword))) return true;
        if (Has(r.Entry.Name, keyword)) return true;
        if (Has(r.Entry.Address, keyword)) return true;
        if (Has(r.Entry.Lease, keyword)) return true;
        if (Has(r.Entry.Comment, keyword)) return true;
        return r.Entry.Extra.Any(e => Has(e, keyword));
    }

    private static DhcpSourceKind ClassifySourceKind(string? filePath, bool isManaged, DnsmasqServiceStatus status)
    {
        if (isManaged) return DhcpSourceKind.Managed;
        if (string.IsNullOrWhiteSpace(filePath)) return DhcpSourceKind.OtherIncluded;

        var fp = filePath.Trim();
        if (!string.IsNullOrEmpty(status.MainConfigPath) &&
            string.Equals(fp, status.MainConfigPath.Trim(), StringComparison.OrdinalIgnoreCase))
            return DhcpSourceKind.MainConfig;

        var ec = status.EffectiveConfig;
        if (ec?.DhcpHostsfilePaths != null &&
            ec.DhcpHostsfilePaths.Any(p => string.Equals(p?.Trim(), fp, StringComparison.OrdinalIgnoreCase)))
            return DhcpSourceKind.DhcpHostsFile;

        if (ec?.DhcpHostsdirPaths != null)
        {
            foreach (var dir in ec.DhcpHostsdirPaths)
            {
                if (string.IsNullOrWhiteSpace(dir)) continue;
                var d = dir.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                if (fp.StartsWith(d + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase) ||
                    fp.StartsWith(d + Path.AltDirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
                    return DhcpSourceKind.DhcpHostsDir;
            }
        }

        return DhcpSourceKind.OtherIncluded;
    }

    private static int SourceGroupOrder(DhcpSourceKind k) => k switch
    {
        DhcpSourceKind.Managed => 0,
        DhcpSourceKind.MainConfig => 1,
        DhcpSourceKind.DhcpHostsFile => 2,
        DhcpSourceKind.DhcpHostsDir => 3,
        _ => 4
    };

    private static string GroupTitle(DhcpSourceKind k) => k switch
    {
        DhcpSourceKind.Managed => "Managed config",
        DhcpSourceKind.MainConfig => "Main config",
        DhcpSourceKind.DhcpHostsFile => "dhcp-hostsfile",
        DhcpSourceKind.DhcpHostsDir => "dhcp-hostsdir",
        _ => "Included file"
    };

    private static bool MacEquals(string a, string b) =>
        string.Equals(a.Trim(), b.Trim(), StringComparison.OrdinalIgnoreCase);

    private static uint ToUint(IPAddress ip)
    {
        var b = ip.GetAddressBytes();
        return ((uint)b[0] << 24) | ((uint)b[1] << 16) | ((uint)b[2] << 8) | b[3];
    }

    private static uint Fnv1aHash(string s)
    {
        unchecked
        {
            const uint offset = 2166136261;
            const uint prime = 16777619;
            var h = offset;
            foreach (var c in s)
            {
                h ^= c;
                h *= prime;
            }
            return h;
        }
    }
}
