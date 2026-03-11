using DnsmasqWebUI.Infrastructure.Services.EffectiveConfig.Metadata;
using DnsmasqWebUI.Models.Dnsmasq.EffectiveConfig;

namespace DnsmasqWebUI.Infrastructure.Services.EffectiveConfig.Readonly;

/// <summary>
/// Generates copyable commands or hints for readonly effective-config values:
/// remove or edit the line in the source file, or override by adding to managed config.
/// Uses EffectiveConfigWriteSemantics so inverse-pair and key-only-or-value options are handled correctly.
/// </summary>
public static class ReadonlyConfigCommandGenerator
{
    /// <summary>
    /// Suggested command to remove the option line from the readonly file (sed). User can copy and run with appropriate privileges.
    /// For InversePair options, removes both keys (e.g. do-0x20-encode and no-0x20-encode).
    /// </summary>
    public static string GetRemoveLineCommand(ConfigValueSource source, string optionName)
    {
        if (string.IsNullOrEmpty(source.FilePath)) return "";
        var pathEscaped = source.FilePath.Replace("|", "\\|");
        var writeBehavior = EffectiveConfigWriteSemantics.GetBehavior(optionName);
        if (writeBehavior == EffectiveConfigWriteBehavior.InversePair)
        {
            var pair = EffectiveConfigWriteSemantics.GetInversePairKeys(optionName);
            if (pair is null) return "";
            var escapedA = EscapeForSedPattern(pair.Value.KeyA);
            var escapedB = EscapeForSedPattern(pair.Value.KeyB);
            return $"sed -i -e '|^{escapedA}$|d' -e '|^{escapedB}$|d' {pathEscaped}";
        }
        var escaped = EscapeForSedPattern(optionName);
        var pattern = writeBehavior == EffectiveConfigWriteBehavior.Flag
            ? $"^{escaped}$"
            : $"^{escaped}=.*$";
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
    /// Consults EffectiveConfigWriteSemantics: InversePair uses ExplicitToggleState (Enabled→KeyA, Disabled→KeyB, Default→remove);
    /// KeyOnlyOrValue uses key-only or key=value; Flag uses key-only when true; other options use key=value fallback.
    /// </summary>
    public static string GetOverrideLine(string optionName, object? value, string? managedFilePath)
    {
        if (string.IsNullOrEmpty(managedFilePath)) return "";
        var writeBehavior = EffectiveConfigWriteSemantics.GetBehavior(optionName);
        if (writeBehavior == EffectiveConfigWriteBehavior.InversePair)
        {
            var pair = EffectiveConfigWriteSemantics.GetInversePairKeys(optionName);
            if (pair is null || value is not ExplicitToggleState s) return "";
            return s switch
            {
                ExplicitToggleState.Enabled => pair.Value.KeyA,
                ExplicitToggleState.Disabled => pair.Value.KeyB,
                _ => "" // Default means remove/unset
            };
        }
        if (writeBehavior == EffectiveConfigWriteBehavior.KeyOnlyOrValue)
        {
            if (value is null) return "";
            var str = value.ToString() ?? "";
            return str.Length == 0 ? optionName : $"{optionName}={str}";
        }
        if (writeBehavior == EffectiveConfigWriteBehavior.Flag)
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
