namespace DnsmasqWebUI.Infrastructure.Serialization.Parsers.EffectiveConfig;

/// <summary>
/// Shared token parsing helpers for DHCP option-style values such as <c>dhcp-option</c>,
/// <c>dhcp-option-force</c>, and <c>dhcp-match</c>.
/// </summary>
public static class DnsmasqDhcpOptionSyntax
{
    public static string[] SplitTokens(string raw) =>
        raw.Split(',').Select(t => t.Trim()).ToArray();

    public static bool HasEmptyToken(IEnumerable<string> tokens) =>
        tokens.Any(t => t.Length == 0);

    public static bool IsPrefixToken(string token, out string? error)
    {
        error = null;
        if (token.StartsWith("tag:", StringComparison.OrdinalIgnoreCase) ||
            token.StartsWith("encap:", StringComparison.OrdinalIgnoreCase) ||
            token.StartsWith("vi-encap:", StringComparison.OrdinalIgnoreCase) ||
            token.StartsWith("vendor:", StringComparison.OrdinalIgnoreCase))
        {
            var colon = token.IndexOf(':');
            if (colon < 0 || colon == token.Length - 1)
                error = $"Prefix token '{token}' cannot be empty after ':'.";
            return true;
        }

        return false;
    }

    public static bool IsOptionSelector(string token)
    {
        if (int.TryParse(token, out _))
            return true;

        if (token.StartsWith("option:", StringComparison.OrdinalIgnoreCase) && token.Length > "option:".Length)
            return true;

        if (token.StartsWith("option6:", StringComparison.OrdinalIgnoreCase) && token.Length > "option6:".Length)
            return true;

        if (token.StartsWith("vi-encap:", StringComparison.OrdinalIgnoreCase) && token.Length > "vi-encap:".Length)
            return true;

        return false;
    }
}
