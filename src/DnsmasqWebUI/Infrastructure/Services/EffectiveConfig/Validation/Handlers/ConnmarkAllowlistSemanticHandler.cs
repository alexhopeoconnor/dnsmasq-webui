using System.Text.RegularExpressions;
using DnsmasqWebUI.Infrastructure.Services.EffectiveConfig.Metadata;
using DnsmasqWebUI.Infrastructure.Services.EffectiveConfig.Validation.Abstractions;

namespace DnsmasqWebUI.Infrastructure.Services.EffectiveConfig.Validation.Handlers;

/// <summary>
/// Specialized semantic behavior for <c>connmark-allowlist</c> values.
/// Validates the connmark[/mask] prefix and one or more domain-style patterns or '*' disable form.
/// </summary>
public sealed partial class ConnmarkAllowlistSemanticHandler : IOptionSemanticHandler
{
    [GeneratedRegex(@"^[A-Za-z0-9*]([A-Za-z0-9*-]*[A-Za-z0-9*])?(\.[A-Za-z0-9*]([A-Za-z0-9*-]*[A-Za-z0-9*])?)+$", RegexOptions.CultureInvariant)]
    private static partial Regex PatternRegex();

    public bool CanHandle(string optionName) =>
        optionName == DnsmasqConfKeys.ConnmarkAllowlist;

    public string? ValidateSingle(object? value) => null;

    public string? ValidateMultiItem(string? value)
    {
        var s = value?.Trim() ?? "";
        if (s.Length == 0)
            return "Value cannot be empty.";

        var tokens = s.Split(',', StringSplitOptions.TrimEntries);
        if (tokens.Length < 2 || tokens.Any(t => t.Length == 0))
            return "connmark-allowlist must be connmark[/mask],pattern[/pattern...].";

        if (!IsValidConnmark(tokens[0]))
            return "connmark-allowlist mark must be decimal or hex, with optional /mask.";

        if (tokens.Length == 2 && tokens[1] == "*")
            return null;

        foreach (var token in tokens.Skip(1))
        {
            var patterns = token.Split('/');
            foreach (var pattern in patterns)
            {
                if (string.IsNullOrWhiteSpace(pattern))
                    return "connmark-allowlist contains an empty pattern.";
                if (!IsValidPattern(pattern))
                    return $"Invalid allowlist pattern '{pattern}'.";
            }
        }

        return null;
    }

    private static bool IsValidConnmark(string value)
    {
        var parts = value.Split('/', 2);
        return IsUInt(parts[0]) && (parts.Length == 1 || IsUInt(parts[1]));
    }

    private static bool IsUInt(string value)
    {
        if (value.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
            return uint.TryParse(value[2..], System.Globalization.NumberStyles.HexNumber, null, out _);
        return uint.TryParse(value, out _);
    }

    private static bool IsValidPattern(string value)
    {
        if (value.Equals("local", StringComparison.OrdinalIgnoreCase))
            return false;

        return PatternRegex().IsMatch(value) &&
               !value.Split('.').Last().All(char.IsDigit);
    }
}
