namespace DnsmasqWebUI.Models;

/// <summary>
/// Dnsmasq service and config status returned by GET api/status.
/// Includes paths from config, command configuration flags, and live output from status/logs commands.
/// </summary>
/// <param name="SystemHostsPath">Path to the hosts file the app can edit (e.g. /etc/hosts). Null when hosts UI is disabled.</param>
/// <param name="SystemHostsPathExists">True if <paramref name="SystemHostsPath"/> is set and the file exists on disk.</param>
/// <param name="NoHosts">True when dnsmasq has no-hosts set (hosts files disabled).</param>
/// <param name="AddnHostsPaths">Effective addn-hosts paths dnsmasq loads. Empty when none configured.</param>
/// <param name="EffectiveConfig">Effective dnsmasq config (single-value, flags, multi-value) after parsing all config files.</param>
/// <param name="EffectiveConfigSources">Source per field (file path, readonly). Use for tooltips and readonly badges; null when not available.</param>
/// <param name="MainConfigPath">Path to the main dnsmasq config file (e.g. /etc/dnsmasq.conf).</param>
/// <param name="ManagedFilePath">Path to the app's managed config file (same directory as main config, e.g. zz-dnsmasq-webui.conf).</param>
/// <param name="LeasesPath">Path to the DHCP leases file from effective config (dhcp-leasefile). Null when not configured.</param>
/// <param name="MainConfigPathExists">True if <paramref name="MainConfigPath"/> exists on disk.</param>
/// <param name="ManagedFilePathExists">True if <paramref name="ManagedFilePath"/> exists on disk.</param>
/// <param name="LeasesPathConfigured">True when dhcp-leasefile is set in effective config.</param>
/// <param name="LeasesPathExists">True if <paramref name="LeasesPath"/> is set and the file exists on disk.</param>
/// <param name="ReloadCommandConfigured">True when ReloadCommand is set in app config (reload after save).</param>
/// <param name="StatusCommandConfigured">True when StatusCommand is set (used to determine dnsmasq active/inactive).</param>
/// <param name="StatusShowConfigured">True when StatusShowCommand is set (full service status output on Dnsmasq page).</param>
/// <param name="LogsConfigured">True when LogsCommand is set (recent logs preview on Dnsmasq page).</param>
/// <param name="LogsPath">Log file path from effective config (log-facility when it is a file path). Used for full download. Null when log-facility is not a file path or not set.</param>
/// <param name="StatusShowCommand">The StatusShowCommand string from config (e.g. systemctl status dnsmasq --no-pager). Null when not configured.</param>
/// <param name="LogsCommand">The LogsCommand string from config (e.g. tail -n 100 /var/log/dnsmasq.log). Null when not configured.</param>
/// <param name="DnsmasqStatus">Service state: "active", "inactive", or "unknown".</param>
/// <param name="StatusCommandExitCode">Exit code of StatusCommand when dnsmasq is not active. Null when active or not configured.</param>
/// <param name="StatusCommandStdout">Stdout of StatusCommand when dnsmasq is not active. Null when active or not configured.</param>
/// <param name="StatusCommandStderr">Stderr of StatusCommand when dnsmasq is not active. Null when active or not configured.</param>
/// <param name="StatusShowOutput">Output of StatusShowCommand (full service status). Null when not configured or command produced no output.</param>
/// <param name="LogsOutput">Output of LogsCommand (recent logs preview). Null when not configured or command produced no output.</param>
/// <param name="DhcpRangeStart">Start IP of the first dhcp-range= (e.g. 172.28.0.10). Null when not set or unparseable.</param>
/// <param name="DhcpRangeEnd">End IP of the first dhcp-range= (e.g. 172.28.0.50). Null when not set or unparseable.</param>
public record DnsmasqServiceStatus(
    string? SystemHostsPath,
    bool SystemHostsPathExists,
    bool NoHosts,
    IReadOnlyList<string> AddnHostsPaths,
    EffectiveDnsmasqConfig? EffectiveConfig,
    EffectiveConfigSources? EffectiveConfigSources,
    string? MainConfigPath,
    string? ManagedFilePath,
    string? LeasesPath,
    bool MainConfigPathExists,
    bool ManagedFilePathExists,
    bool LeasesPathConfigured,
    bool LeasesPathExists,
    bool ReloadCommandConfigured,
    bool StatusCommandConfigured,
    bool StatusShowConfigured,
    bool LogsConfigured,
    string? LogsPath,
    string? StatusShowCommand,
    string? LogsCommand,
    string DnsmasqStatus,
    int? StatusCommandExitCode,
    string? StatusCommandStdout,
    string? StatusCommandStderr,
    string? StatusShowOutput,
    string? LogsOutput,
    string? DhcpRangeStart,
    string? DhcpRangeEnd
);
