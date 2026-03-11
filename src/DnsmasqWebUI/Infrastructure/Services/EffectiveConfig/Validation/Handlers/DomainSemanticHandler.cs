using System.Net;
using DnsmasqWebUI.Infrastructure.Services.EffectiveConfig.Metadata;
using DnsmasqWebUI.Infrastructure.Services.EffectiveConfig.Validation.Abstractions;
using DnsmasqWebUI.Infrastructure.Serialization.Parsers.EffectiveConfig;

namespace DnsmasqWebUI.Infrastructure.Services.EffectiveConfig.Validation.Handlers;

/// <summary>
/// Semantic validation for <c>domain</c> (DHCP/DNS) values: &lt;domain&gt;[[,&lt;address range&gt;[,local]]|&lt;interface&gt;].
/// Domain required (# or DNS name); optional address range (IP or CIDR), interface, or "local".
/// </summary>
public sealed class DomainSemanticHandler : IOptionSemanticHandler
{
    public bool CanHandle(string optionName) => optionName == DnsmasqConfKeys.Domain;

    public string? ValidateSingle(object? value) => null;

    public string? ValidateMultiItem(string? value)
    {
        var s = (value ?? "").Trim();
        if (s.Length == 0)
            return "domain value cannot be empty.";
        var tokens = s.Split(',', StringSplitOptions.TrimEntries);
        if (tokens.Length == 0 || tokens[0].Length == 0)
            return "domain requires a domain name or #.";
        var domain = tokens[0];
        if (domain != "#" && !DnsmasqScopedDomainSyntax.IsValidDnsName(domain))
            return "domain must be a valid DNS name or #.";
        if (tokens.Length >= 2 && tokens[1].Length == 0)
            return "domain cannot have empty fields.";
        if (tokens.Length >= 2 && !IsValidSecondToken(tokens[1]))
            return "domain second field must be interface name, IP, CIDR (e.g. 192.168.0.0/24), or 'local'.";
        return null;
    }

    private static bool IsValidSecondToken(string token)
    {
        if (token == "local")
            return true;
        if (DnsmasqRelaySyntax.IsInterfaceName(token))
            return true;
        if (token.Contains('/'))
            return DnsmasqIpPrefixSyntax.ValidateIpWithOptionalPrefix(token, "domain") is null;
        return IPAddress.TryParse(token, out _);
    }
}
