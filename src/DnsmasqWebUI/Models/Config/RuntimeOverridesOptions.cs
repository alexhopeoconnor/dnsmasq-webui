namespace DnsmasqWebUI.Models.Config;

/// <summary>
/// Path for the app settings overrides file used to persist runtime config changes
/// (e.g. log level). Bound from "RuntimeOverrides" config section.
/// </summary>
public class RuntimeOverridesOptions
{
    public const string SectionName = "RuntimeOverrides";

    /// <summary>Default filename when FilePath is not set (resolved relative to application base directory).</summary>
    public const string DefaultFileName = "appsettings.Overrides.json";

    /// <summary>
    /// Full path to the JSON file the app reads/writes for runtime overrides.
    /// If not set, defaults to <see cref="DefaultFileName"/> in the application base directory.
    /// </summary>
    public string? FilePath { get; set; }
}
