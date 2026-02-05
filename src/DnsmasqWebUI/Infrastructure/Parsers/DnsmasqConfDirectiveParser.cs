using Superpower;
using Superpower.Model;
using Superpower.Parsers;

namespace DnsmasqWebUI.Infrastructure.Parsers;

/// <summary>
/// Key/value parsing for dnsmasq .conf lines. Used by <see cref="DnsmasqConfIncludeParser"/> to build effective config.
/// StripComment and TryParseKeyValue handle comments and key=value (or key-only) lines.
/// </summary>
public static class DnsmasqConfDirectiveParser
{
    // Key=value line using shared helper (Then/IgnoreThen for hot path). Full line.
    private static readonly TextParser<(string key, string value)> DirectiveLine =
        ConfParserHelpers.KeyValueLine.Named("key=value directive");

    /// <summary>Strip dnsmasq-style comment: from first '#' that is at word start (after whitespace) to end of line.</summary>
    public static string StripComment(string line)
    {
        var result = ConfParserHelpers.StripCommentContent.TryParse(line);
        return result.HasValue ? result.Value : line.TrimEnd();
    }

    /// <summary>Parse a non-comment line into key and value. Returns null for empty or comment-only lines.
    /// Strips dnsmasq-style comments (# and rest when # is after whitespace). Supports key-only lines (value "").</summary>
    public static (string key, string value)? TryParseKeyValue(string line)
    {
        var t = StripComment(line).TrimStart();
        if (string.IsNullOrEmpty(t))
            return null;
        if (t.StartsWith("#", StringComparison.Ordinal))
            return null;
        var result = DirectiveLine.TryParse(t);
        if (!result.HasValue)
            return null;
        var (key, value) = result.Value;
        if (string.IsNullOrWhiteSpace(key))
            return null;
        return (key, value);
    }

    /// <summary>Like <see cref="TryParseKeyValue"/> but when parsing fails returns the Superpower error message and position (line/column).</summary>
    public static bool TryParseKeyValue(string line, out (string key, string value)? kv, out string? error, out Position errorPosition)
    {
        kv = null;
        error = null;
        errorPosition = Position.Empty;
        var t = StripComment(line).TrimStart();
        if (string.IsNullOrEmpty(t) || t.StartsWith("#", StringComparison.Ordinal))
            return true; // not a directive line, no error
        var result = DirectiveLine.TryParse(t);
        if (result.HasValue)
        {
            var (key, value) = result.Value;
            if (string.IsNullOrWhiteSpace(key))
                return true;
            kv = (key, value);
            return true;
        }
        error = result.ToString();
        errorPosition = result.ErrorPosition;
        return false;
    }
}
