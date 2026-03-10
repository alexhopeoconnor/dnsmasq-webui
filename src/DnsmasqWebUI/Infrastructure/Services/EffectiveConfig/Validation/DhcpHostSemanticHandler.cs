using System.Net;
using System.Text.RegularExpressions;
using DnsmasqWebUI.Infrastructure.Helpers.Config;

namespace DnsmasqWebUI.Infrastructure.Services.EffectiveConfig.Validation;

/// <summary>
/// Conservative semantic validation for <c>dhcp-host</c> values.
/// Validates obvious token shapes without attempting to fully model every legal dnsmasq variant.
/// </summary>
public sealed partial class DhcpHostSemanticHandler : IOptionSemanticHandler
{
    [GeneratedRegex(@"^([0-9A-Fa-f*]{2}:){5}[0-9A-Fa-f*]{2}$", RegexOptions.CultureInvariant)]
    private static partial Regex MacPattern();

    [GeneratedRegex(@"^[A-Za-z0-9]([A-Za-z0-9-]*[A-Za-z0-9])?(\.[A-Za-z0-9]([A-Za-z0-9-]*[A-Za-z0-9])?)*$", RegexOptions.CultureInvariant)]
    private static partial Regex HostPattern();

    public bool CanHandle(string optionName) =>
        optionName == DnsmasqConfKeys.DhcpHost;

    public string? ValidateSingle(object? value) => null;

    public string? ValidateMultiItem(string? value)
    {
        var s = value?.Trim() ?? "";
        if (s.Length == 0)
            return "Value cannot be empty.";

        var tokens = s.Split(',').Select(t => t.Trim()).ToArray();
        if (tokens.Any(t => t.Length == 0))
            return "dhcp-host contains an empty comma-separated segment.";

        var hasIdentity = false;
        foreach (var token in tokens)
        {
            if (token.Equals("ignore", StringComparison.OrdinalIgnoreCase) ||
                token.StartsWith("set:", StringComparison.OrdinalIgnoreCase) ||
                token.StartsWith("tag:", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (token.StartsWith("id:", StringComparison.OrdinalIgnoreCase))
            {
                hasIdentity = true;
                if (token.Length <= 3)
                    return "dhcp-host id: segment cannot be empty.";
                continue;
            }

            if (MacPattern().IsMatch(token))
            {
                hasIdentity = true;
                continue;
            }

            if (IsDhcpHostAddress(token) || IsLeaseToken(token))
                continue;

            if (HostPattern().IsMatch(token))
            {
                hasIdentity = true;
                continue;
            }

            return $"Unrecognized dhcp-host segment '{token}'.";
        }

        return hasIdentity
            ? null
            : "dhcp-host must include a MAC address, id:<client-id>, or hostname.";
    }

    private static bool IsDhcpHostAddress(string value)
    {
        if (IPAddress.TryParse(value, out _))
            return true;

        if (value.StartsWith("[", StringComparison.Ordinal) && value.EndsWith("]", StringComparison.Ordinal))
            return true;

        return false;
    }

    private static bool IsLeaseToken(string value)
    {
        if (value.Equals("infinite", StringComparison.OrdinalIgnoreCase))
            return true;

        if (value.Length == 0)
            return false;

        var suffix = value[^1];
        var number = char.IsLetter(suffix) ? value[..^1] : value;
        if (!int.TryParse(number, out _))
            return false;

        return !char.IsLetter(suffix) || suffix is 's' or 'm' or 'h' or 'd' or 'w';
    }
}
