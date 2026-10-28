using DnsmasqWebUI.Models.Hosts;

namespace DnsmasqWebUI.Models.Contracts;

/// <summary>Immutable snapshot from the hosts cache: managed hosts file entries, non-managed hosts files, and the complete resolved source inventory.</summary>
/// <param name="ManagedEntries">Entries from the app-managed hosts file.</param>
/// <param name="ReadOnlyFiles">Non-managed host file sources. Includes empty entries for configured missing/unreadable sources.</param>
public record HostsSnapshot(
    IReadOnlyList<HostEntry> ManagedEntries,
    IReadOnlyList<ReadOnlyHostsFile> ReadOnlyFiles
)
{
    /// <summary>
    /// Complete source inventory including the managed path and configured directories. This is separate
    /// from <see cref="ReadOnlyFiles"/> because a directory itself and an inactive/missing managed path
    /// may have no parsed rows.
    /// </summary>
    public IReadOnlyList<HostSource> Sources { get; init; } = Array.Empty<HostSource>();
}
