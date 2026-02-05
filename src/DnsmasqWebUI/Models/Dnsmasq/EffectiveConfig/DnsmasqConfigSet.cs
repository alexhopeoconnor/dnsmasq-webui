namespace DnsmasqWebUI.Models.Dnsmasq.EffectiveConfig;

/// <summary>Ordered set of dnsmasq config files (main + conf-file + conf-dir). ManagedFilePath is the single config file we read/write; ManagedHostsFilePath is the single hosts file we read/write.</summary>
/// <param name="MainConfigPath">Path to the main dnsmasq config file (e.g. /etc/dnsmasq.conf).</param>
/// <param name="ManagedFilePath">Path to the app-managed config file (e.g. zz-dnsmasq-webui.conf).</param>
/// <param name="ManagedHostsFilePath">Path to the app-managed hosts file; null when not configured.</param>
/// <param name="Files">Ordered list of config files (main + included) for display.</param>
public record DnsmasqConfigSet(
    string MainConfigPath,
    string ManagedFilePath,
    string? ManagedHostsFilePath,
    IReadOnlyList<DnsmasqConfigSetEntry> Files
);
