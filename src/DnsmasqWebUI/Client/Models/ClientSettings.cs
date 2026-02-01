namespace DnsmasqWebUI.Client.Models;

/// <summary>
/// Client-side preferences persisted in browser local storage.
/// </summary>
public class ClientSettings
{
    /// <summary>Polling interval for the service status block (seconds).</summary>
    public int ServiceStatusPollingIntervalSeconds { get; set; } = 15;

    /// <summary>Polling interval for the recent logs block (seconds).</summary>
    public int RecentLogsPollingIntervalSeconds { get; set; } = 5;
}
