namespace DnsmasqWebUI.Options;

public class DnsmasqOptions
{
    /// <summary>Configuration section name (e.g. "Dnsmasq" for appsettings and Dnsmasq__* env vars).</summary>
    public const string SectionName = "Dnsmasq";

    /// <summary>Path to the hosts file (e.g. /etc/hosts). Read and written by HostsFileService.</summary>
    public string HostsPath { get; set; } = "";

    /// <summary>Path to the single dnsmasq config file we manage (e.g. /etc/dnsmasq.d/dhcp.conf). Parsed with DnsmasqConfigParser; we only edit dhcp-host= lines and preserve the rest.</summary>
    public string ConfigPath { get; set; } = "";

    /// <summary>Path to the dnsmasq DHCP leases file (e.g. /var/lib/misc/dnsmasq.leases). Read-only; LeasesFileService reads it for the Leases page.</summary>
    public string? LeasesPath { get; set; }

    /// <summary>Command to run after config changes (e.g. "systemctl reload dnsmasq"). Override per deployment via config/env.</summary>
    public string? ReloadCommand { get; set; }

    /// <summary>Optional command to check dnsmasq service state (e.g. "systemctl is-active dnsmasq"). Exit 0 = active, non-zero = inactive.</summary>
    public string? StatusCommand { get; set; }
}
