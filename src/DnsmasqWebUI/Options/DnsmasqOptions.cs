namespace DnsmasqWebUI.Options;

/// <summary>
/// Configuration for dnsmasq paths and reload/status commands.
/// File permissions: the app must be able to read MainConfigPath and the conf-dir (or conf-file) target,
/// and to create/update the managed config file and HostsPath. In the Docker image (Dockerfile)
/// app and dnsmasq run in one container as root, so this works. When the UI runs in a container and
/// dnsmasq is on the host, bind-mount the host config dir (e.g. /etc/dnsmasq.d) into the container;
/// the container process typically needs to run as root (or the host dir must be writable by the
/// container user) to create/update files there. ReloadCommand and StatusCommand run inside the
/// container, so they only affect processes in that container; to reload host dnsmasq from a
/// container you need a host-side mechanism (e.g. a small service on the host that runs
/// systemctl reload dnsmasq when triggered by the UI).
/// </summary>
public class DnsmasqOptions
{
    /// <summary>Configuration section name (e.g. "Dnsmasq" for appsettings and Dnsmasq__* env vars).</summary>
    public const string SectionName = "Dnsmasq";

    /// <summary>Path to the main dnsmasq config (e.g. /etc/dnsmasq.conf). App appends conf-file= at the end if missing so the managed file is included; process must have write access.</summary>
    public string MainConfigPath { get; set; } = "";

    /// <summary>Filename of the managed config (e.g. zz-dnsmasq-webui.conf), created in &lt;main-config-dir&gt;/dnsmasq.d/ and included via conf-file= at the end of the main config so it loads last. Managed file content parsed with DnsmasqConfFileLineParser.</summary>
    public string ManagedFileName { get; set; } = "zz-dnsmasq-webui.conf";

    /// <summary>Path we write as addn-hosts= in the managed file; HostsFileService reads/writes this path. Process must have read/write access.</summary>
    public string HostsPath { get; set; } = "";

    /// <summary>Command to run after config changes (e.g. "systemctl reload dnsmasq" or "pkill -HUP -x dnsmasq"). Runs in the same environment as the app; if app is in a container and dnsmasq is on the host, this runs in the container and will not reload host dnsmasq unless you use a host-side relay.</summary>
    public string? ReloadCommand { get; set; }

    /// <summary>Optional command to check dnsmasq service state (e.g. "systemctl is-active dnsmasq" or "pgrep -x dnsmasq"). Runs in the same environment as the app; if app is in a container, this checks for dnsmasq in the container, not on the host.</summary>
    public string? StatusCommand { get; set; }
}
