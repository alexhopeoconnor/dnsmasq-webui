using System.Net;

namespace DnsmasqWebUI.Infrastructure.Serialization.Parsers.EffectiveConfig;

/// <summary>
/// Shared helpers for option values that are an IP literal with an optional prefix length.
/// </summary>
internal static class DnsmasqIpPrefixSyntax
{
    public static string? ValidateIpWithOptionalPrefix(string value, string optionName)
    {
        var parts = value.Split('/', 2);
        if (!IPAddress.TryParse(parts[0], out var ip))
            return $"{optionName} must start with a valid IP address.";

        if (parts.Length == 1)
            return null;

        if (!int.TryParse(parts[1], out var prefix))
            return $"{optionName} prefix length must be numeric.";

        var maxPrefix = ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork ? 32 : 128;
        return prefix >= 0 && prefix <= maxPrefix
            ? null
            : $"{optionName} prefix length must be between 0 and {maxPrefix}.";
    }
}
