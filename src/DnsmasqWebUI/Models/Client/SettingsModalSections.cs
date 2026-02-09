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

/// <summary>
/// Central definition of settings modal sections: keys, display names, field labels, and searchable text.
/// </summary>
public static class SettingsModalSections
{
    public const string ServiceStatus = "ServiceStatus";
    public const string Logs = "Logs";
    public const string Leases = "Leases";

    /// <summary>Section metadata: display name (header), field label (for input), and searchable text for filtering.</summary>
    public sealed record SectionMeta(string DisplayName, string FieldLabel, string SearchableText);

    public static readonly IReadOnlyDictionary<string, SectionMeta> All = new Dictionary<string, SectionMeta>(StringComparer.OrdinalIgnoreCase)
    {
        [ServiceStatus] = new("Service status", "Polling interval (seconds)", "service status polling interval seconds"),
        [Logs] = new("Recent logs", "Polling interval (seconds)", "recent logs polling interval seconds"),
        [Leases] = new("DHCP leases", "Refresh interval (seconds)", "dhcp leases refresh interval seconds"),
    };

    /// <summary>Maps a focused context (single-section view) to its section key. Returns null for All.</summary>
    public static string? GetSectionKeyForContext(SettingsModalContext context) => context switch
    {
        SettingsModalContext.ServicePolling => ServiceStatus,
        SettingsModalContext.LogsPolling => Logs,
        SettingsModalContext.LeasesPolling => Leases,
        _ => null
    };

    public static bool MatchesSearch(string key, string searchTerm)
    {
        if (string.IsNullOrWhiteSpace(searchTerm)) return true;
        if (!All.TryGetValue(key, out var meta)) return false;
        return meta.SearchableText.Contains(searchTerm.Trim(), StringComparison.OrdinalIgnoreCase);
    }
}
