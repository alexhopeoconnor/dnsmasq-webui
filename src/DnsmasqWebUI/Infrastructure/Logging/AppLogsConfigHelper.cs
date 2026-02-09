using Microsoft.Extensions.Configuration;

namespace DnsmasqWebUI.Infrastructure.Logging;

/// <summary>
/// Helper for resolving app logs config. Uses ExcludedCategoryPrefixesOverrides when present (avoids .NET config array merge);
/// otherwise falls back to ExcludedCategoryPrefixes from appsettings.json.
/// Uses a sentinel value when user explicitly clears all filters, since .NET config returns null for empty arrays.
/// </summary>
public static class AppLogsConfigHelper
{
    private const string OverridesKey = "AppLogs:ExcludedCategoryPrefixesOverrides";
    private const string DefaultsKey = "AppLogs:ExcludedCategoryPrefixes";

    /// <summary>Sentinel value stored when user clears all filters. Filtered out from results; extremely unlikely to match real categories.</summary>
    public const string ExplicitlyEmptySentinel = "__AppLogsExplicitlyEmpty__";

    /// <summary>Gets effective excluded category prefixes from configuration.</summary>
    public static List<string> GetEffectiveExcludedPrefixes(IConfiguration configuration)
    {
        var overrides = configuration.GetSection(OverridesKey).Get<List<string>>();
        if (overrides != null)
            return overrides.Where(s => s != ExplicitlyEmptySentinel).ToList();
        return GetDefaultExcludedPrefixes(configuration);
    }

    /// <summary>Gets default excluded category prefixes from appsettings.json (ignores overrides).</summary>
    public static List<string> GetDefaultExcludedPrefixes(IConfiguration configuration) =>
        configuration.GetSection(DefaultsKey).Get<List<string>>() ?? [];
}
