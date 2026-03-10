using System.Net;
using DnsmasqWebUI.Infrastructure.Helpers.Config;

namespace DnsmasqWebUI.Infrastructure.Services.EffectiveConfig.Validation;

/// <summary>
/// Specialized semantic behavior for <c>alias</c> values.
/// Supports IPv4 single-address or IPv4 range mapping forms.
/// </summary>
public sealed class AliasSemanticHandler : IOptionSemanticHandler
{
    public bool CanHandle(string optionName) =>
        optionName == DnsmasqConfKeys.Alias;

    public string? ValidateSingle(object? value) => null;

    public string? ValidateMultiItem(string? value)
    {
        var s = value?.Trim() ?? "";
        if (s.Length == 0)
            return "Value cannot be empty.";

        var tokens = s.Split(',').Select(t => t.Trim()).ToArray();
        if (tokens.Length is < 2 or > 3 || tokens.Any(t => t.Length == 0))
            return "alias must be old-ip,new-ip[,mask] or start-ip-end-ip,new-ip[,mask].";

        if (!IsValidOldIpOrRange(tokens[0]))
            return "alias first value must be an IPv4 address or IPv4 range.";
        if (!IsIPv4(tokens[1]))
            return "alias replacement address must be a valid IPv4 address.";
        if (tokens.Length == 3 && !IsIPv4(tokens[2]))
            return "alias mask must be a valid IPv4 address.";

        return null;
    }

    private static bool IsValidOldIpOrRange(string value)
    {
        if (IsIPv4(value))
            return true;

        var parts = value.Split('-', 2);
        return parts.Length == 2 && IsIPv4(parts[0]) && IsIPv4(parts[1]);
    }

    private static bool IsIPv4(string value) =>
        IPAddress.TryParse(value, out var ip) &&
        ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork;
}
