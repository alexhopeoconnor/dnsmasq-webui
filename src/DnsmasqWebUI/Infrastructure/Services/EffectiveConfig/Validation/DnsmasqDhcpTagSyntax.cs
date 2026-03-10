namespace DnsmasqWebUI.Infrastructure.Services.EffectiveConfig.Validation;

/// <summary>
/// Shared helpers for DHCP option families that use <c>set:</c>, <c>tag:</c>,
/// and interface/tag-like tokens.
/// </summary>
internal static class DnsmasqDhcpTagSyntax
{
    public static bool IsSetToken(string token, bool prefixOptional = false)
    {
        if (token.StartsWith("set:", StringComparison.OrdinalIgnoreCase))
            return token.Length > 4;

        return prefixOptional && token.Length > 0;
    }

    public static bool IsTagToken(string token, bool allowNegation = false)
    {
        if (!token.StartsWith("tag:", StringComparison.OrdinalIgnoreCase))
            return false;

        var value = token["tag:".Length..];
        if (value.Length == 0)
            return false;

        if (allowNegation && value.StartsWith("!", StringComparison.Ordinal))
            value = value[1..];

        return value.Length > 0;
    }

    public static bool IsTagOrSetToken(string token, bool allowNegation = false) =>
        IsSetToken(token) || IsTagToken(token, allowNegation);

    public static bool IsInterfaceLike(string token, bool allowWildcard = false) =>
        token.Length > 0 &&
        token.Length <= 64 &&
        token.All(c => char.IsLetterOrDigit(c) || c is '-' or '_' or '.' || (allowWildcard && c == '*'));

    /// <summary>
    /// Validates <c>set:&lt;tag&gt;,&lt;value&gt;</c> form used by dhcp-circuitid, dhcp-remoteid, dhcp-subscrid.
    /// Tag and value must be non-empty; value may be colon-separated hex or a simple string.
    /// </summary>
    /// <param name="value">Trimmed option value.</param>
    /// <param name="optionLabel">Option name for error messages (e.g. "dhcp-circuitid").</param>
    /// <returns>Error message or null if valid.</returns>
    public static string? ValidateSetTagValue(string value, string optionLabel)
    {
        if (string.IsNullOrWhiteSpace(value))
            return $"{optionLabel} value cannot be empty.";
        var tokens = value.Split(',', 2, StringSplitOptions.TrimEntries);
        if (tokens.Length != 2)
            return $"{optionLabel} must be set:<tag>,<value>.";
        if (tokens[0].Length == 0 || tokens[1].Length == 0)
            return $"{optionLabel} tag and value cannot be empty.";
        if (!IsSetToken(tokens[0]))
            return $"{optionLabel} must start with set:<tag>.";
        return null;
    }
}
