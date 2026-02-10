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

    /// <summary>Show only recent dnsmasq logs polling interval.</summary>
    LogsPolling,

    /// <summary>Show only app logs polling interval.</summary>
    AppLogsPolling,

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
    public const string AppLogs = "AppLogs";
    public const string Leases = "Leases";
    public const string RecentLogsDisplay = "RecentLogsDisplay";
    public const string AppLogsDisplay = "AppLogsDisplay";

    /// <summary>Section metadata: display name (header), field label (for input), and searchable text for filtering.</summary>
    public sealed record SectionMeta(string DisplayName, string FieldLabel, string SearchableText);

    public static readonly IReadOnlyDictionary<string, SectionMeta> All = new Dictionary<string, SectionMeta>(StringComparer.OrdinalIgnoreCase)
    {
        [ServiceStatus] = new("Service status", "Polling interval (seconds)", "service status polling interval seconds"),
        [Logs] = new("Recent logs", "Polling interval (seconds)", "recent dnsmasq logs polling interval seconds"),
        [AppLogs] = new("App logs", "Polling interval (seconds)", "app logs polling interval seconds"),
        [Leases] = new("DHCP leases", "Refresh interval (seconds)", "dhcp leases refresh interval seconds"),
        [RecentLogsDisplay] = new("Recent logs display", "Max lines / Auto-scroll", "recent logs display max lines auto scroll"),
        [AppLogsDisplay] = new("App logs display", "Max lines / Auto-scroll", "app logs display max lines auto scroll"),
    };

    /// <summary>Maps a focused context (single-section view) to its section keys. Returns null for All (search mode).</summary>
    public static string[]? GetSectionKeysForContext(SettingsModalContext context) => context switch
    {
        SettingsModalContext.ServicePolling => [ServiceStatus],
        SettingsModalContext.LogsPolling => [Logs, RecentLogsDisplay],
        SettingsModalContext.AppLogsPolling => [AppLogs, AppLogsDisplay],
        SettingsModalContext.LeasesPolling => [Leases],
        _ => null
    };

    /// <summary>Legacy: returns first section key for context. Use GetSectionKeysForContext for multi-section support.</summary>
    public static string? GetSectionKeyForContext(SettingsModalContext context)
    {
        var keys = GetSectionKeysForContext(context);
        return keys is { Length: > 0 } ? keys[0] : null;
    }

    public static bool MatchesSearch(string key, string searchTerm)
    {
        if (string.IsNullOrWhiteSpace(searchTerm)) return true;
        if (!All.TryGetValue(key, out var meta)) return false;
        return meta.SearchableText.Contains(searchTerm.Trim(), StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>Collapsible group: display name and the section keys it contains.</summary>
    public sealed record CollapsibleGroup(string Id, string DisplayName, string[] SectionKeys);

    /// <summary>Groups for the collapsible settings UI. Order determines display order.</summary>
    public static readonly IReadOnlyList<CollapsibleGroup> Groups =
    [
        new CollapsibleGroup("service", "Service status", [ServiceStatus]),
        new CollapsibleGroup("dnsmasq-logs", "Dnsmasq logs", [Logs, RecentLogsDisplay]),
        new CollapsibleGroup("app-logs", "App logs", [AppLogs, AppLogsDisplay]),
        new CollapsibleGroup("leases", "DHCP leases", [Leases]),
    ];

    /// <summary>Whether a group should be shown: for context mode, any child section matches; for All+search, any child matches search.</summary>
    public static bool GroupMatchesContextOrSearch(CollapsibleGroup group, SettingsModalContext context, string searchTerm)
    {
        var keys = GetSectionKeysForContext(context);
        if (keys != null)
            return group.SectionKeys.Any(k => keys.Contains(k, StringComparer.OrdinalIgnoreCase));
        return group.SectionKeys.Any(k => MatchesSearch(k, searchTerm));
    }
}
