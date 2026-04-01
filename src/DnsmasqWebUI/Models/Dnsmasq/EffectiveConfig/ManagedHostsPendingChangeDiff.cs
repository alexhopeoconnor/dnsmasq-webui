using DnsmasqWebUI.Models.Hosts;

namespace DnsmasqWebUI.Models.Dnsmasq.EffectiveConfig;

public enum ManagedHostsLineDiffKind
{
    Added,
    Removed,
    Modified,
    /// <summary>Same entries (by id) but a different order in the file.</summary>
    OrderOnly
}

/// <summary>One line-level change for display in the save modal (before → after).</summary>
public readonly record struct ManagedHostsLineDiff(
    ManagedHostsLineDiffKind Kind,
    string? BeforeLine,
    string? AfterLine);

/// <summary>Builds a line-level diff between baseline and draft managed-hosts entries.</summary>
public static class ManagedHostsPendingChangeDiff
{
    public static IReadOnlyList<ManagedHostsLineDiff> Build(PendingManagedHostsChange change) =>
        Build(change.OldEntries, change.NewEntries);

    public static IReadOnlyList<ManagedHostsLineDiff> Build(
        IReadOnlyList<HostEntry> oldEntries,
        IReadOnlyList<HostEntry> newEntries)
    {
        var oldMap = ToUniqueMap(oldEntries);
        var newMap = ToUniqueMap(newEntries);
        var keys = oldMap.Keys.Union(newMap.Keys, StringComparer.Ordinal).ToHashSet(StringComparer.Ordinal);
        var list = new List<ManagedHostsLineDiff>();

        foreach (var key in keys)
        {
            oldMap.TryGetValue(key, out var o);
            newMap.TryGetValue(key, out var n);

            if (o == null && n != null)
                list.Add(new ManagedHostsLineDiff(ManagedHostsLineDiffKind.Added, null, FormatEntry(n)));
            else if (o != null && n == null)
                list.Add(new ManagedHostsLineDiff(ManagedHostsLineDiffKind.Removed, FormatEntry(o), null));
            else if (o != null && n != null && !ContentEquals(o, n))
                list.Add(new ManagedHostsLineDiff(ManagedHostsLineDiffKind.Modified, FormatEntry(o), FormatEntry(n)));
        }

        if (list.Count == 0 && DetectOrderOnly(oldEntries, newEntries))
            list.Add(new ManagedHostsLineDiff(ManagedHostsLineDiffKind.OrderOnly, null, null));

        return list
            .OrderBy(d => SortOrder(d))
            .ToList();
    }

    private static int SortOrder(ManagedHostsLineDiff d) => d.Kind switch
    {
        ManagedHostsLineDiffKind.Removed => 0,
        ManagedHostsLineDiffKind.Modified => 1,
        ManagedHostsLineDiffKind.OrderOnly => 2,
        ManagedHostsLineDiffKind.Added => 3,
        _ => 4
    };

    private static bool DetectOrderOnly(IReadOnlyList<HostEntry> oldList, IReadOnlyList<HostEntry> newList)
    {
        if (oldList.Count != newList.Count || oldList.Count == 0)
            return false;
        var oldKeys = oldList.Select(e => StableKey(e)).OrderBy(k => k, StringComparer.Ordinal).ToList();
        var newKeys = newList.Select(e => StableKey(e)).OrderBy(k => k, StringComparer.Ordinal).ToList();
        if (!oldKeys.SequenceEqual(newKeys, StringComparer.Ordinal))
            return false;
        for (var i = 0; i < oldList.Count; i++)
        {
            if (!string.Equals(StableKey(oldList[i]), StableKey(newList[i]), StringComparison.Ordinal))
                return true;
        }

        return false;
    }

    private static Dictionary<string, HostEntry> ToUniqueMap(IReadOnlyList<HostEntry> entries)
    {
        var map = new Dictionary<string, HostEntry>(StringComparer.Ordinal);
        foreach (var e in entries)
        {
            var k = StableKey(e);
            if (!map.ContainsKey(k))
                map[k] = e;
        }

        return map;
    }

    /// <summary>Matches <see cref="PendingManagedHostsChange"/> identity: Id, or managed:line when Id is empty.</summary>
    public static string StableKey(HostEntry e) =>
        !string.IsNullOrEmpty(e.Id) ? e.Id : $"managed:{e.LineNumber}";

    public static string FormatEntry(HostEntry e)
    {
        if (e.IsPassthrough)
            return string.IsNullOrEmpty(e.RawLine) ? "(unparsed line)" : e.RawLine.TrimEnd();
        if (e.IsComment)
            return string.IsNullOrEmpty(e.RawLine) ? "#" : e.RawLine.TrimEnd();
        var names = e.Names ?? new List<string>();
        if (string.IsNullOrWhiteSpace(e.Address) && names.Count == 0)
            return "(empty)";
        var core = $"{e.Address} {string.Join(' ', names)}".Trim();
        if (string.IsNullOrWhiteSpace(e.InlineComment))
            return core;
        return $"{core} # {e.InlineComment}";
    }

    private static bool ContentEquals(HostEntry a, HostEntry b)
    {
        if (string.Equals(a.Address ?? "", b.Address ?? "", StringComparison.Ordinal))
        {
            var an = a.Names ?? new List<string>();
            var bn = b.Names ?? new List<string>();
            if (an.SequenceEqual(bn, StringComparer.Ordinal)
                && a.IsComment == b.IsComment
                && a.IsPassthrough == b.IsPassthrough
                && string.Equals(a.RawLine ?? "", b.RawLine ?? "", StringComparison.Ordinal)
                && string.Equals(a.InlineComment ?? "", b.InlineComment ?? "", StringComparison.Ordinal))
                return true;
        }

        return false;
    }
}
