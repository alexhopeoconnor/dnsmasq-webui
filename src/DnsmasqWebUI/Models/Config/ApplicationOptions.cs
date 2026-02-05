namespace DnsmasqWebUI.Models.Config;

/// <summary>
/// Application-level settings (title, etc.). Bound from "Application" config section
/// (appsettings.json or Application__* environment variables).
/// </summary>
public class ApplicationOptions
{
    /// <summary>Configuration section name (e.g. "Application" for appsettings and Application__* env vars).</summary>
    public const string SectionName = "Application";

    /// <summary>Default application title when not configured.</summary>
    public const string DefaultTitle = "Local DNS";

    /// <summary>Application title shown in the navbar brand, page titles, and browser tab.</summary>
    public string ApplicationTitle { get; set; } = DefaultTitle;

    /// <summary>Title to use in UI (never null/empty).</summary>
    public string EffectiveTitle => string.IsNullOrWhiteSpace(ApplicationTitle) ? DefaultTitle : ApplicationTitle;
}
