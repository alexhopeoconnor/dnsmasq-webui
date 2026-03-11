using System.Net;
using DnsmasqWebUI.Infrastructure.Services.EffectiveConfig.Metadata;
using DnsmasqWebUI.Infrastructure.Services.EffectiveConfig.Validation.Abstractions;
using DnsmasqWebUI.Infrastructure.Serialization.Parsers.EffectiveConfig;

namespace DnsmasqWebUI.Infrastructure.Services.EffectiveConfig.Validation.Handlers;

/// <summary>
/// Semantic validation for <c>auth-server</c> values: &lt;domain&gt;,[&lt;interface&gt;|&lt;ip-address&gt;...].
/// Domain (glue record) is required; optional comma-separated list of interfaces or IPs; interface may have /4 or /6.
/// </summary>
public sealed class AuthServerSemanticHandler : IOptionSemanticHandler
{
    public bool CanHandle(string optionName) => optionName == DnsmasqConfKeys.AuthServer;

    public string? ValidateSingle(object? value) => null;

    public string? ValidateMultiItem(string? value)
    {
        var s = (value ?? "").Trim();
        if (s.Length == 0)
            return "auth-server value cannot be empty.";
        var tokens = s.Split(',', StringSplitOptions.TrimEntries);
        if (tokens.Length == 0 || tokens[0].Length == 0)
            return "auth-server requires a domain (glue record).";
        for (var i = 1; i < tokens.Length; i++)
        {
            var token = tokens[i];
            if (token.Length == 0)
                return "auth-server list cannot contain empty interface or address.";
            if (IsIpOrInterface(token))
                continue;
            return "auth-server: each item after the domain must be an IP address or interface name (optional /4 or /6).";
        }
        return null;
    }

    private static bool IsIpOrInterface(string token)
    {
        if (IPAddress.TryParse(token, out _))
            return true;
        if (token.EndsWith("/4", StringComparison.Ordinal) || token.EndsWith("/6", StringComparison.Ordinal))
            return DnsmasqRelaySyntax.IsInterfaceName(token[..^2]);
        return DnsmasqRelaySyntax.IsInterfaceName(token);
    }
}
