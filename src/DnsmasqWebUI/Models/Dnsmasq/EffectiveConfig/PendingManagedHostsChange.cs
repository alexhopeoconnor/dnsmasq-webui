using DnsmasqWebUI.Models.Hosts;

namespace DnsmasqWebUI.Models.Dnsmasq.EffectiveConfig;

/// <summary>A pending change for the managed hosts file. Written via IHostsFileService on save.</summary>
public sealed record PendingManagedHostsChange(
    IReadOnlyList<HostEntry> OldEntries,
    IReadOnlyList<HostEntry> NewEntries,
    string ManagedHostsFilePath)
    : PendingDnsmasqChange("hosts:managed")
{
    /// <summary>True when NewEntries differs from OldEntries.</summary>
    public bool HasChanges => !EntriesEqual(OldEntries, NewEntries);

    private static bool EntriesEqual(IReadOnlyList<HostEntry> left, IReadOnlyList<HostEntry> right)
    {
        if (left.Count != right.Count) return false;
        for (var i = 0; i < left.Count; i++)
        {
            var a = left[i];
            var b = right[i];
            if (a.Id != b.Id ||
                a.Address != b.Address ||
                !(a.Names ?? new List<string>()).SequenceEqual(b.Names ?? new List<string>(), StringComparer.Ordinal) ||
                a.RawLine != b.RawLine ||
                a.IsComment != b.IsComment ||
                a.IsPassthrough != b.IsPassthrough)
                return false;
        }
        return true;
    }
}
