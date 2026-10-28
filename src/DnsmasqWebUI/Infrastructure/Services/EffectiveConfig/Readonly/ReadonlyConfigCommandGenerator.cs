using DnsmasqWebUI.Infrastructure.Services.EffectiveConfig.Metadata;
using DnsmasqWebUI.Models.Dnsmasq.EffectiveConfig;

namespace DnsmasqWebUI.Infrastructure.Services.EffectiveConfig.Readonly;

/// <summary>
/// Generates copyable commands or hints for readonly effective-config values:
/// remove or edit the line in the source file, or (only when it is genuinely safe) override by
/// adding to managed config. Flags, inverse pairs, and multi-value directives deliberately do not
/// receive a copyable override line: adding one cannot safely remove/replace the external state.
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
        if (writeBehavior == EffectiveConfigWriteBehavior.KeyOnlyOrValue)
            return $"sed -i -e '|^{escaped}$|d' -e '|^{escaped}=.*$|d' {pathEscaped}";
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
    /// The line to add to managed config to override a last-wins single-value/key-only directive
    /// (for example <c>port=53</c>). Empty when a managed override cannot safely express the
    /// requested effect, or when no managed path is configured.
    /// </summary>
    public static string GetOverrideLine(string optionName, object? value, string? managedFilePath)
    {
        if (string.IsNullOrEmpty(managedFilePath)) return "";
        var writeBehavior = EffectiveConfigWriteSemantics.GetBehavior(optionName);
        if (writeBehavior is not (EffectiveConfigWriteBehavior.SingleValue or EffectiveConfigWriteBehavior.KeyOnlyOrValue))
            return "";

        if (writeBehavior == EffectiveConfigWriteBehavior.KeyOnlyOrValue)
        {
            if (value is null) return "";
            var str = value.ToString() ?? "";
            return str.Length == 0 ? optionName : $"{optionName}={str}";
        }
        if (value is null)
            return "";
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
