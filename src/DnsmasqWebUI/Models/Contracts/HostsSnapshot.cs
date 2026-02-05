using DnsmasqWebUI.Models.Hosts;

namespace DnsmasqWebUI.Models.Contracts;

/// <summary>Immutable snapshot from the hosts cache: managed hosts file entries and read-only hosts files (system + addn-hosts).</summary>
/// <param name="ManagedEntries">Entries from the app-managed hosts file.</param>
/// <param name="ReadOnlyFiles">Read-only hosts files (system hosts when configured, then addn-hosts that are not the managed file).</param>
public record HostsSnapshot(
    IReadOnlyList<HostEntry> ManagedEntries,
    IReadOnlyList<ReadOnlyHostsFile> ReadOnlyFiles
);
