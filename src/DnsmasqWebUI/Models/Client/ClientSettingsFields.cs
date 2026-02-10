using DnsmasqWebUI.Models.Client.Abstractions;

namespace DnsmasqWebUI.Models.Client;

/// <summary>
/// Static field definitions and hydration/serialization helpers.
/// </summary>
public static class ClientSettingsFields
{
    static ClientSettingsFields()
    {
        HydrateFrom(new ClientSettings());
    }

    public static readonly ClientSettingsMinMaxField ServiceStatusPollingInterval = new("Service status polling interval", 5, 300);
    public static readonly ClientSettingsMinMaxField RecentLogsPollingInterval = new("Recent logs polling interval", 5, 300);
    public static readonly ClientSettingsMinMaxField AppLogsPollingInterval = new("App logs polling interval", 5, 300);
    public static readonly ClientSettingsMinMaxField LeasesPollingInterval = new("DHCP leases refresh interval", 5, 300);
    public static readonly ClientSettingsMinMaxField RecentLogsMaxLines = new("Recent logs max lines", 100, 2000, "lines");
    public static readonly ClientSettingsMinMaxField AppLogsMaxLines = new("App logs max lines", 100, 2000, "lines");
    public static readonly ClientSettingsBoolField RecentLogsAutoScroll = new("Recent logs auto-scroll");
    public static readonly ClientSettingsBoolField AppLogsAutoScroll = new("App logs auto-scroll");

    private static readonly (string Section, IClientSettingsField Field)[] ValidationMap =
    [
        (SettingsModalSections.ServiceStatus, ServiceStatusPollingInterval),
        (SettingsModalSections.Logs, RecentLogsPollingInterval),
        (SettingsModalSections.AppLogs, AppLogsPollingInterval),
        (SettingsModalSections.Leases, LeasesPollingInterval),
        (SettingsModalSections.RecentLogsDisplay, RecentLogsMaxLines),
        (SettingsModalSections.RecentLogsDisplay, RecentLogsAutoScroll),
        (SettingsModalSections.AppLogsDisplay, AppLogsMaxLines),
        (SettingsModalSections.AppLogsDisplay, AppLogsAutoScroll),
    ];

    public static IReadOnlyList<IClientSettingsField> All => [ServiceStatusPollingInterval, RecentLogsPollingInterval, AppLogsPollingInterval, LeasesPollingInterval, RecentLogsMaxLines, AppLogsMaxLines, RecentLogsAutoScroll, AppLogsAutoScroll];

    public static IReadOnlyList<(string Section, IClientSettingsField Field)> ValidationChecks => ValidationMap;

    public static void HydrateFrom(ClientSettings dto)
    {
        ServiceStatusPollingInterval.Value = dto.ServiceStatusPollingIntervalSeconds;
        RecentLogsPollingInterval.Value = dto.RecentLogsPollingIntervalSeconds;
        AppLogsPollingInterval.Value = dto.AppLogsPollingIntervalSeconds;
        LeasesPollingInterval.Value = dto.LeasesPollingIntervalSeconds;
        RecentLogsMaxLines.Value = dto.RecentLogsMaxLines;
        RecentLogsAutoScroll.Value = dto.RecentLogsAutoScroll;
        AppLogsMaxLines.Value = dto.AppLogsMaxLines;
        AppLogsAutoScroll.Value = dto.AppLogsAutoScroll;
    }

    public static ClientSettings ToDto()
    {
        return new ClientSettings
        {
            ServiceStatusPollingIntervalSeconds = ServiceStatusPollingInterval.Value,
            RecentLogsPollingIntervalSeconds = RecentLogsPollingInterval.Value,
            AppLogsPollingIntervalSeconds = AppLogsPollingInterval.Value,
            LeasesPollingIntervalSeconds = LeasesPollingInterval.Value,
            RecentLogsMaxLines = RecentLogsMaxLines.Value,
            RecentLogsAutoScroll = RecentLogsAutoScroll.Value,
            AppLogsMaxLines = AppLogsMaxLines.Value,
            AppLogsAutoScroll = AppLogsAutoScroll.Value,
        };
    }
}
