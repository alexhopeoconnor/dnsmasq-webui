namespace DnsmasqWebUI.Models.Client;

/// <summary>
/// Client-side preferences persisted in browser local storage.
/// </summary>
public class ClientSettings
{
    /// <summary>Polling interval for the service status block (seconds).</summary>
    public int ServiceStatusPollingIntervalSeconds { get; set; } = 15;

    /// <summary>Polling interval for the recent dnsmasq logs block (seconds).</summary>
    public int RecentLogsPollingIntervalSeconds { get; set; } = 5;

    /// <summary>Polling interval for the app logs block fallback (seconds).</summary>
    public int AppLogsPollingIntervalSeconds { get; set; } = 5;

    /// <summary>Polling interval for the DHCP leases list (seconds).</summary>
    public int LeasesPollingIntervalSeconds { get; set; } = 5;

    /// <summary>Maximum number of lines in the dnsmasq (recent) logs panel. Older lines are truncated.</summary>
    public int RecentLogsMaxLines { get; set; } = 500;

    /// <summary>Whether to auto-scroll the dnsmasq logs panel when new lines arrive.</summary>
    public bool RecentLogsAutoScroll { get; set; } = true;

    /// <summary>Maximum number of lines in the app logs panel. Older lines are truncated.</summary>
    public int AppLogsMaxLines { get; set; } = 500;

    /// <summary>Whether to auto-scroll the app logs panel when new lines arrive.</summary>
    public bool AppLogsAutoScroll { get; set; } = true;
}
