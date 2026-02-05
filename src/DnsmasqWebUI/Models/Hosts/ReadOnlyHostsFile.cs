namespace DnsmasqWebUI.Models.Hosts;

/// <summary>Path and parsed entries for a read-only addn-hosts file (not the managed hosts file).</summary>
/// <param name="Path">Absolute path of the hosts file (e.g. /etc/hosts or an addn-hosts path).</param>
/// <param name="Entries">Parsed entries from the file (IP, names, comments, passthrough).</param>
public record ReadOnlyHostsFile(string Path, IReadOnlyList<HostEntry> Entries);
