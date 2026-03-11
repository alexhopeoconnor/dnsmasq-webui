namespace DnsmasqWebUI.Infrastructure.Serialization.Parsers.EffectiveConfig;

/// <summary>
/// Shared helpers for DNS RR-type values used by <c>filter-rr</c> and <c>cache-rr</c>.
/// RR-type can be a name (e.g. TXT, MX, A, AAAA, ANY) or a decimal number.
/// </summary>
internal static class DnsmasqRrTypeSyntax
{
    /// <summary>Returns true if the token is a valid RR-type: non-empty, letters/digits/hyphen or decimal number.</summary>
    public static bool IsValidRrType(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
            return false;
        var t = token.Trim();
        if (t.Length == 0)
            return false;
        return t.All(c => char.IsLetterOrDigit(c) || c == '-');
    }
}
