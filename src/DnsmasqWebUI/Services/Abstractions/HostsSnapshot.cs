using DnsmasqWebUI.Models.Hosts;

namespace DnsmasqWebUI.Services.Abstractions;

/// <summary>Immutable snapshot from the hosts cache: managed hosts file entries and read-only hosts files (system + addn-hosts).</summary>
public record HostsSnapshot(
    IReadOnlyList<HostEntry> ManagedEntries,
    IReadOnlyList<ReadOnlyHostsFile> ReadOnlyFiles
);
