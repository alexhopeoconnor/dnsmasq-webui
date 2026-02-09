namespace DnsmasqWebUI.Models.Config;

/// <summary>
/// Configuration for the app logs UI: which logger categories to exclude from the live display.
/// Bound from "AppLogs" config section (appsettings.json, appsettings.Overrides.json, or AppLogs__* env vars).
/// </summary>
public class AppLogsOptions
{
    /// <summary>Configuration section name.</summary>
    public const string SectionName = "AppLogs";

    /// <summary>
    /// Logger category name prefixes to exclude from the app logs display.
    /// Any category that starts with one of these strings will not be forwarded to the buffer.
    /// Defaults exclude noisy framework categories: Components, Kestrel, HttpClient, etc.
    /// </summary>
    public List<string> ExcludedCategoryPrefixes { get; set; } =
    [
        "Microsoft.AspNetCore.Components.",
        "Microsoft.AspNetCore.Server.Kestrel.Transport.",
        "Microsoft.AspNetCore.Server.Kestrel.Connections",
        "System.Net.Http.",
        "Microsoft.Extensions.Http.",
        "Microsoft.AspNetCore.Hosting.",
        "Microsoft.AspNetCore.Routing.",
    ];
}
