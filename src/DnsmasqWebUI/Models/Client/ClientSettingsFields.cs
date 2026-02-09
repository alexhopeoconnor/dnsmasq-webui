namespace DnsmasqWebUI.Models.Client;

/// <summary>
/// Per-field metadata for client settings: bounds, display names for validation errors.
/// Used by SettingsModal (validation on save) and by pages/components (defensive clamp when loading from storage).
/// </summary>
public static class ClientSettingsFields
{
    public sealed record FieldBounds(int Min, int Max, string DisplayName)
    {
        public bool IsValid(int value) => value >= Min && value <= Max;
        public int Clamp(int value) => Math.Clamp(value, Min, Max);
        public string? Validate(int value) =>
            IsValid(value) ? null : $"{DisplayName} must be between {Min} and {Max} seconds. You entered {value}.";
    }

    public static readonly FieldBounds ServiceStatusPollingInterval = new(5, 300, "Service status polling interval");
    public static readonly FieldBounds RecentLogsPollingInterval = new(5, 300, "Recent logs polling interval");
    public static readonly FieldBounds AppLogsPollingInterval = new(5, 300, "App logs polling interval");
    public static readonly FieldBounds LeasesPollingInterval = new(5, 300, "DHCP leases refresh interval");
    public static readonly FieldBounds RecentLogsMaxLines = new(100, 2000, "Recent logs max lines");
    public static readonly FieldBounds AppLogsMaxLines = new(100, 2000, "App logs max lines");
}
