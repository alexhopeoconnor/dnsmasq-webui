namespace DnsmasqWebUI.Models;

/// <summary>Path and parsed entries for a read-only addn-hosts file (not the managed hosts file).</summary>
public record ReadOnlyHostsFile(string Path, IReadOnlyList<HostEntry> Entries);
