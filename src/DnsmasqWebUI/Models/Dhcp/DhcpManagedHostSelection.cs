namespace DnsmasqWebUI.Models.Dhcp;

/// <summary>
/// Checkbox selection for managed <c>dhcp-host</c> lines uses list indices; remaps after removals.
/// </summary>
public static class DhcpManagedHostSelection
{
    public static void RemapAfterRemovals(HashSet<int> selected, IReadOnlyCollection<int> removedIndices)
    {
        var removed = removedIndices as HashSet<int> ?? removedIndices.Distinct().ToHashSet();
        if (removed.Count == 0) return;
        var next = new HashSet<int>(
            selected
                .Where(i => !removed.Contains(i))
                .Select(i => i - removed.Count(r => r < i)));
        selected.Clear();
        foreach (var i in next)
            selected.Add(i);
    }
}
