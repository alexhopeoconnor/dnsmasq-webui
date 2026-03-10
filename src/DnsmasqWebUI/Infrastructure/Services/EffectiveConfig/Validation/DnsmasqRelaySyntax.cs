using System.Net;

namespace DnsmasqWebUI.Infrastructure.Services.EffectiveConfig.Validation;

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
}
