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
}
