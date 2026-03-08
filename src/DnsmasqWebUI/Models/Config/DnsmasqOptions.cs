namespace DnsmasqWebUI.Models.Config;

/// <summary>
/// Configuration for dnsmasq paths and reload/status commands.
/// File permissions: the app must be able to read MainConfigPath and the conf-dir (or conf-file) target,
/// and to create/update the managed config file and SystemHostsPath. In the Docker image (Dockerfile)
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

    /// <summary>Filename of the managed config (e.g. zz-dnsmasq-webui.conf), created in the same directory as the main config and included only via a conf-file= directive as the last line of the main config. Managed file content parsed with DnsmasqConfFileLineParser.</summary>
    public string ManagedFileName { get; set; } = "zz-dnsmasq-webui.conf";

    /// <summary>Filename of the managed hosts file (e.g. zz-dnsmasq-webui.hosts), created in the same directory as the main config. The app adds addn-hosts=&lt;this path&gt; in the managed config so dnsmasq loads it last. This is the only hosts file the app writes to.</summary>
    public string ManagedHostsFileName { get; set; } = "zz-dnsmasq-webui.hosts";

    /// <summary>Optional path to the system hosts file (e.g. /etc/hosts). When set, shown in the UI as read-only so users can see those entries. The app never writes to it; editing is via the managed hosts file only. When unset, the system hosts row is not shown.</summary>
    public string? SystemHostsPath { get; set; }

    /// <summary>Command to run to apply config changes (e.g. "systemctl restart dnsmasq"). When set, this is used instead of <see cref="ReloadCommand"/> so that .conf file changes take effect (dnsmasq does not re-read config on SIGHUP). If unset, <see cref="ReloadCommand"/> is used.</summary>
    public string? RestartCommand { get; set; }

    /// <summary>Command to run after config changes when <see cref="RestartCommand"/> is not set (e.g. "systemctl reload dnsmasq" or "pkill -HUP -x dnsmasq"). SIGHUP only re-reads hosts/addn-hosts etc., not .conf files. Runs in the same environment as the app.</summary>
    public string? ReloadCommand { get; set; }

    /// <summary>Optional command to validate config before restart (e.g. "dnsmasq --test --conf-file=\"{{MainConfigPath}}\""). Token {{MainConfigPath}} is replaced with <see cref="MainConfigPath"/>. When set, run after write and before restart; if it fails, restart is not attempted. When unset, validation is skipped.</summary>
    public string? ValidateCommand { get; set; } = "dnsmasq --test --conf-file=\"{{MainConfigPath}}\"";

    /// <summary>Optional command to check dnsmasq service state (e.g. "systemctl is-active dnsmasq" or "pgrep -x dnsmasq"). Runs in the same environment as the app; if app is in a container, this checks for dnsmasq in the container, not on the host.</summary>
    public string? StatusCommand { get; set; }

    /// <summary>Optional command for full service status (e.g. "systemctl status dnsmasq --no-pager"). Output shown on Dnsmasq page.</summary>
    public string? StatusShowCommand { get; set; }

    /// <summary>Optional command for recent logs (e.g. "journalctl -u dnsmasq -n 100 --no-pager"). Output shown on Dnsmasq page.</summary>
    public string? LogsCommand { get; set; }

    // --- Timeouts (seconds) for the commands above ---

    /// <summary>Timeout in seconds for <see cref="RestartCommand"/> / <see cref="ReloadCommand"/>. Default 30.</summary>
    public int RestartTimeoutSeconds { get; set; } = 30;

    /// <summary>Timeout in seconds for <see cref="StatusCommand"/>. Default 5.</summary>
    public int StatusTimeoutSeconds { get; set; } = 5;

    /// <summary>Timeout in seconds for <see cref="StatusShowCommand"/>. Default 5.</summary>
    public int StatusShowTimeoutSeconds { get; set; } = 5;

    /// <summary>Timeout in seconds for <see cref="LogsCommand"/>. Default 10.</summary>
    public int LogsTimeoutSeconds { get; set; } = 10;

    /// <summary>Timeout in seconds for <see cref="ValidateCommand"/>. Default 10.</summary>
    public int ValidateTimeoutSeconds { get; set; } = 10;

    /// <summary>Command to probe dnsmasq version (e.g. "dnsmasq --version"). Used for minimum-version checks.</summary>
    public string? VersionCommand { get; set; } = "dnsmasq --version";

    /// <summary>Timeout in seconds for <see cref="VersionCommand"/>. Default 5.</summary>
    public int VersionTimeoutSeconds { get; set; } = 5;

    /// <summary>Minimum dnsmasq version required (e.g. "2.91"). Checked when <see cref="EnforceMinimumVersion"/> is true.</summary>
    public string MinimumVersion { get; set; } = "2.91";

    /// <summary>When true, application fails to start if dnsmasq version probe fails or version is below <see cref="MinimumVersion"/>.</summary>
    public bool EnforceMinimumVersion { get; set; } = true;

    /// <summary><see cref="VersionTimeoutSeconds"/> as <see cref="TimeSpan"/>.</summary>
    public TimeSpan VersionTimeout => TimeSpan.FromSeconds(VersionTimeoutSeconds);

    /// <summary><see cref="RestartTimeoutSeconds"/> as <see cref="TimeSpan"/>.</summary>
    public TimeSpan RestartTimeout => TimeSpan.FromSeconds(RestartTimeoutSeconds);

    /// <summary><see cref="StatusTimeoutSeconds"/> as <see cref="TimeSpan"/>.</summary>
    public TimeSpan StatusTimeout => TimeSpan.FromSeconds(StatusTimeoutSeconds);

    /// <summary><see cref="StatusShowTimeoutSeconds"/> as <see cref="TimeSpan"/>.</summary>
    public TimeSpan StatusShowTimeout => TimeSpan.FromSeconds(StatusShowTimeoutSeconds);

    /// <summary><see cref="LogsTimeoutSeconds"/> as <see cref="TimeSpan"/>.</summary>
    public TimeSpan LogsTimeout => TimeSpan.FromSeconds(LogsTimeoutSeconds);

    /// <summary><see cref="ValidateTimeoutSeconds"/> as <see cref="TimeSpan"/>.</summary>
    public TimeSpan ValidateTimeout => TimeSpan.FromSeconds(ValidateTimeoutSeconds);
}
