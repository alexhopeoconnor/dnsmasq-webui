using System.Net;

namespace DnsmasqWebUI.Infrastructure.Helpers.Config;

/// <summary>
/// Shared structural parser for <c>dhcp-range</c> option values.
/// This is intentionally conservative and captures the core structure needed by
/// validation and simple range extraction without re-implementing all dnsmasq semantics.
/// </summary>
public sealed record ParsedDhcpRange(
    IReadOnlyList<string> Tags,
    string StartToken,
    string SecondToken,
    IReadOnlyList<string> RemainingTokens);

/// <summary>
/// Parser for a single <c>dhcp-range</c> value.
/// </summary>
public static class DnsmasqDhcpRangeValueParser
{
    private static readonly HashSet<string> ModeKeywords = new(StringComparer.OrdinalIgnoreCase)
    {
        "static",
        "proxy",
        "ra-only",
        "ra-stateless",
        "ra-names",
        "slaac",
        "off-link",
    };

    public static bool TryParse(string raw, out ParsedDhcpRange? parsed, out string? error)
    {
        parsed = null;
        error = null;

        if (string.IsNullOrWhiteSpace(raw))
        {
            error = "Value cannot be empty.";
            return false;
        }

        var tokens = raw.Split(',').Select(t => t.Trim()).ToArray();
        if (tokens.Any(t => t.Length == 0))
        {
            error = "dhcp-range contains an empty comma-separated segment.";
            return false;
        }

        var index = 0;
        var tags = new List<string>();
        while (index < tokens.Length && IsTagToken(tokens[index]))
        {
            tags.Add(tokens[index]);
            index++;
        }

        if (index >= tokens.Length || !IsRangeStartToken(tokens[index]))
        {
            error = "dhcp-range must include a valid start address or constructor:<interface> after any tag/set prefixes.";
            return false;
        }

        var startToken = tokens[index];
        index++;

        if (index >= tokens.Length)
        {
            error = "dhcp-range must include an end address or mode after the start address.";
            return false;
        }

        var secondToken = tokens[index];
        if (!IsIpAddress(secondToken) && !IsModeToken(secondToken))
        {
            error = "dhcp-range second value must be an end address or a valid mode.";
            return false;
        }

        parsed = new ParsedDhcpRange(
            tags,
            startToken,
            secondToken,
            tokens.Skip(index + 1).ToArray());
        return true;
    }

    public static (string? Start, string? End) GetIPv4StartEnd(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return (null, null);

        if (!TryParse(raw, out var parsed, out _))
            return (null, null);

        if (!IPAddress.TryParse(parsed!.StartToken, out var startIp) ||
            startIp.AddressFamily != System.Net.Sockets.AddressFamily.InterNetwork)
        {
            return (null, null);
        }

        if (!IPAddress.TryParse(parsed.SecondToken, out var endIp) ||
            endIp.AddressFamily != System.Net.Sockets.AddressFamily.InterNetwork)
        {
            return (parsed.StartToken, null);
        }

        return (parsed.StartToken, parsed.SecondToken);
    }

    private static bool IsTagToken(string value) =>
        value.StartsWith("tag:", StringComparison.OrdinalIgnoreCase) ||
        value.StartsWith("set:", StringComparison.OrdinalIgnoreCase);

    private static bool IsModeToken(string value) =>
        ModeKeywords.Contains(value) ||
        value.StartsWith("constructor:", StringComparison.OrdinalIgnoreCase);

    private static bool IsRangeStartToken(string value) =>
        IsIpAddress(value) || value.StartsWith("constructor:", StringComparison.OrdinalIgnoreCase);

    private static bool IsIpAddress(string value) =>
        IPAddress.TryParse(value, out _);
}
