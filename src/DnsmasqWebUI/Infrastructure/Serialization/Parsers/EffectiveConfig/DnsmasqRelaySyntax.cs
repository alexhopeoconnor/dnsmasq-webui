using System.Net;

namespace DnsmasqWebUI.Infrastructure.Serialization.Parsers.EffectiveConfig;

/// <summary>
/// Shared parsing helpers for relay/proxy style values that work primarily with
/// IP literal addresses, optional <c>#port</c> suffixes, and interface names.
/// </summary>
internal static class DnsmasqRelaySyntax
{
    public static string[] SplitTokens(string raw) =>
        raw.Split(',').Select(t => t.Trim()).ToArray();

    public static bool HasEmptyToken(IEnumerable<string> tokens) =>
        tokens.Any(t => t.Length == 0);

    public static bool IsIpLiteral(string value) =>
        IPAddress.TryParse(value, out _);

    public static bool IsServerAddress(string value)
    {
        var hash = value.LastIndexOf('#');
        var host = hash >= 0 ? value[..hash] : value;
        if (!IsIpLiteral(host))
            return false;
        return hash < 0 || (int.TryParse(value[(hash + 1)..], out var port) && port is >= 1 and <= 65535);
    }

    public static bool IsInterfaceName(string value) =>
        value.Length > 0 &&
        value.Length <= 64 &&
        value.Count(c => c == '.') <= 1 &&
        value.All(c => char.IsLetterOrDigit(c) || c is '-' or '_' or '.');

    /// <summary>
    /// Interface name with optional trailing <c>*</c> wildcard (for --interface, --except-interface, etc.).
    /// Only a single trailing asterisk is allowed, not <c>*</c> in the middle.
    /// </summary>
    public static bool IsInterfaceNameWithOptionalTrailingWildcard(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return false;
        var v = value.Trim();
        if (v.Length == 0)
            return false;
        if (v.EndsWith('*'))
            return v.Length > 1 && IsInterfaceName(v[..^1]);
        return IsInterfaceName(v);
    }
}
