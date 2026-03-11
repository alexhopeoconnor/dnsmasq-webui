using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text.RegularExpressions;
using DnsmasqWebUI.Infrastructure.Services.EffectiveConfig.Metadata;
using DnsmasqWebUI.Infrastructure.Services.EffectiveConfig.Validation.Abstractions;
using DnsmasqWebUI.Infrastructure.Serialization.Parsers.EffectiveConfig;

namespace DnsmasqWebUI.Infrastructure.Services.EffectiveConfig.Validation.Handlers;

/// <summary>
/// Specialized semantic behavior for <c>server</c> values.
/// Supports plain upstream server values and domain-scoped <c>/domain/.../server</c> forms.
/// </summary>
public sealed partial class ServerSemanticHandler : IOptionSemanticHandler
{
    [GeneratedRegex(@"^[A-Za-z0-9]([A-Za-z0-9.-]*[A-Za-z0-9])?$", RegexOptions.CultureInvariant)]
    private static partial Regex HostnamePattern();

    public bool CanHandle(string optionName) =>
        optionName == DnsmasqConfKeys.Server;

    public string? ValidateSingle(object? value) => null;

    public string? ValidateMultiItem(string? value)
    {
        var s = value?.Trim() ?? "";
        if (s.Length == 0)
            return "Value cannot be empty.";

        return s.StartsWith("/", StringComparison.Ordinal)
            ? ValidateScopedServer(s)
            : ValidateServerTarget(s);
    }

    private static string? ValidateScopedServer(string value)
    {
        if (!DnsmasqScopedDomainSyntax.TrySplitScopedValue(value, out var domains, out var targetPart, out var error))
            return "Scoped server must use /domain/.../server syntax, for example /example.local/192.168.1.1.";

        error = DnsmasqScopedDomainSyntax.ValidateDomainPatterns(domains);
        if (error is not null)
            return error.Replace("Value", "Scoped server", StringComparison.Ordinal) +
                   " Use server=/domain/server and keep domain labels to letters, digits, '-', '.', or a leading '*'.";

        if (targetPart.Length == 0)
            return null; // local-only form

        return ValidateServerTarget(targetPart);
    }

    private static string? ValidateServerTarget(string value)
    {
        var parts = value.Split('@');
        if (parts.Any(string.IsNullOrWhiteSpace))
            return "Server target contains an empty '@' segment.";

        var upstream = parts[0];
        if (upstream != "#" && !IsValidServerHostPort(upstream))
            return "Enter a valid upstream server: IP, hostname, '#', or host#port.";

        for (var i = 1; i < parts.Length; i++)
        {
            if (!IsValidSourceOrInterface(parts[i]))
                return $"Invalid source/interface segment '{parts[i]}'. Use an interface name, source IP, or source IP#port.";
        }

        return null;
    }

    private static bool IsValidServerHostPort(string value)
    {
        var hashIndex = value.LastIndexOf('#');
        var hostPart = hashIndex >= 0 ? value[..hashIndex] : value;
        if (!IsValidHostOrIp(hostPart))
            return false;

        if (hashIndex < 0)
            return true;

        var portPart = value[(hashIndex + 1)..];
        return int.TryParse(portPart, out var port) && port is >= 1 and <= 65535;
    }

    private static bool IsValidSourceOrInterface(string value)
    {
        var hashIndex = value.LastIndexOf('#');
        var targetPart = hashIndex >= 0 ? value[..hashIndex] : value;
        var portPart = hashIndex >= 0 ? value[(hashIndex + 1)..] : null;

        var ok = IsValidHostOrIp(targetPart) || IsValidInterfaceName(targetPart);
        if (!ok)
            return false;

        return portPart is null || (int.TryParse(portPart, out var port) && port is >= 1 and <= 65535);
    }

    private static bool IsValidHostOrIp(string value)
    {
        if (IPAddress.TryParse(value, out var ip))
            return ip.AddressFamily is AddressFamily.InterNetwork or AddressFamily.InterNetworkV6;

        return value.Length <= 253 && HostnamePattern().IsMatch(value);
    }

    private static bool IsValidInterfaceName(string value) =>
        value.Length > 0 &&
        value.Length <= 64 &&
        value.All(c => char.IsLetterOrDigit(c) || c is '-' or '_' or '.');
}
