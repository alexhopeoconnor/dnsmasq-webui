using DnsmasqWebUI.Models.Dnsmasq.EffectiveConfig;

namespace DnsmasqWebUI.Infrastructure.Helpers.Config;

/// <summary>
/// Generates copyable commands or hints for readonly effective-config values:
/// remove or edit the line in the source file, or override by adding to managed config.
/// </summary>
public static class ReadonlyConfigCommandGenerator
{
    /// <summary>
    /// Suggested command to remove the option line from the readonly file (sed). User can copy and run with appropriate privileges.
    /// Escapes the option name for a basic sed pattern (^option=\?.*$ or ^option$ for flags).
    /// </summary>
    public static string GetRemoveLineCommand(ConfigValueSource source, string optionName)
    {
        if (string.IsNullOrEmpty(source.FilePath)) return "";
        var escaped = EscapeForSedPattern(optionName);
        var behavior = EffectiveConfigParserBehaviorMap.GetBehavior(optionName);
        var pattern = behavior == EffectiveConfigParserBehavior.Flag
            ? $"^{escaped}$"
            : $"^{escaped}=.*$";
        var pathEscaped = source.FilePath.Replace("|", "\\|");
        return $"sed -i '|{pattern}|d' {pathEscaped}";
    }

    /// <summary>
    /// Short hint for editing the file (path only; no command).
    /// </summary>
    public static string GetEditFileHint(ConfigValueSource source)
    {
        return string.IsNullOrEmpty(source.FilePath) ? "" : $"Edit {source.FilePath} to remove or change the line.";
    }

    /// <summary>
    /// The line to add to managed config to override (e.g. "port=53" or "expand-hosts"). Empty if managed path not set.
    /// </summary>
    public static string GetOverrideLine(string optionName, object? value, string? managedFilePath)
    {
        if (string.IsNullOrEmpty(managedFilePath)) return "";
        var behavior = EffectiveConfigParserBehaviorMap.GetBehavior(optionName);
        if (behavior == EffectiveConfigParserBehavior.Flag)
            return value is bool b && b ? optionName : "";
        var v = ValueToConfString(value);
        return string.IsNullOrEmpty(v) ? optionName : $"{optionName}={v}";
    }

    /// <summary>
    /// Display text for the override hint (includes managed path).
    /// </summary>
    public static string GetOverrideHint(string optionName, object? value, string? managedFilePath)
    {
        if (string.IsNullOrEmpty(managedFilePath)) return "";
        var line = GetOverrideLine(optionName, value, managedFilePath);
        return string.IsNullOrEmpty(line)
            ? ""
            : $"Add to {managedFilePath} to override: {line}";
    }

    private static string ValueToConfString(object? value)
    {
        if (value == null) return "";
        if (value is bool b) return b ? "1" : "0";
        return value.ToString() ?? "";
    }

    private static string EscapeForSedPattern(string optionName)
    {
        return optionName
            .Replace("\\", "\\\\")
            .Replace(".", "\\.")
            .Replace("*", "\\*")
            .Replace("[", "\\[")
            .Replace("]", "\\]")
            .Replace("^", "\\^")
            .Replace("$", "\\$");
    }
}
