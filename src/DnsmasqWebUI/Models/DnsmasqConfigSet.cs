namespace DnsmasqWebUI.Models;

/// <summary>Ordered set of dnsmasq config files (main + conf-file + conf-dir). ManagedFilePath is the single file we read/write.</summary>
public record DnsmasqConfigSet(
    string MainConfigPath,
    string ManagedFilePath,
    IReadOnlyList<DnsmasqConfigSetEntry> Files
);
