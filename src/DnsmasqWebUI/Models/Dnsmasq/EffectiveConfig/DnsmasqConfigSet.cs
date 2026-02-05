namespace DnsmasqWebUI.Models.Dnsmasq.EffectiveConfig;

/// <summary>Ordered set of dnsmasq config files (main + conf-file + conf-dir). ManagedFilePath is the single config file we read/write; ManagedHostsFilePath is the single hosts file we read/write.</summary>
public record DnsmasqConfigSet(
    string MainConfigPath,
    string ManagedFilePath,
    string? ManagedHostsFilePath,
    IReadOnlyList<DnsmasqConfigSetEntry> Files
);
