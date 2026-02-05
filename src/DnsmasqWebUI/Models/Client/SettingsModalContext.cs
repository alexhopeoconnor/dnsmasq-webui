namespace DnsmasqWebUI.Models.Client;

/// <summary>
/// Which subset of client settings to show in the settings modal.
/// </summary>
public enum SettingsModalContext
{
    /// <summary>Show all settings with optional search.</summary>
    All,

    /// <summary>Show only service status polling interval.</summary>
    ServicePolling,

    /// <summary>Show only recent logs polling interval.</summary>
    LogsPolling,

    /// <summary>Show only DHCP leases refresh interval.</summary>
    LeasesPolling
}
