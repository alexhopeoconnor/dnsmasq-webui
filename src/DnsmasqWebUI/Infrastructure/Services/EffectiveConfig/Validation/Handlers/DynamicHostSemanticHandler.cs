using System.Net;
using DnsmasqWebUI.Infrastructure.Services.EffectiveConfig.Metadata;
using DnsmasqWebUI.Infrastructure.Services.EffectiveConfig.Validation.Abstractions;
using DnsmasqWebUI.Infrastructure.Serialization.Parsers.EffectiveConfig;

namespace DnsmasqWebUI.Infrastructure.Services.EffectiveConfig.Validation.Handlers;

/// <summary>
/// Semantic validation for <c>dynamic-host</c> values: &lt;name&gt;,[IPv4],[IPv6],&lt;interface&gt; (dnsmasq man).
/// Name and interface required. Doc example uses 3 tokens (name,IPv4,interface); we allow 3 or 4.
/// </summary>
public sealed class DynamicHostSemanticHandler : IOptionSemanticHandler
{
    public bool CanHandle(string optionName) => optionName == DnsmasqConfKeys.DynamicHost;

    public string? ValidateSingle(object? value) => null;

    public string? ValidateMultiItem(string? value)
    {
        var s = (value ?? "").Trim();
        if (s.Length == 0)
            return "dynamic-host value cannot be empty.";
        var tokens = s.Split(',', StringSplitOptions.TrimEntries);
        if (tokens.Length is not 3 and not 4)
            return "dynamic-host must be <name>,[IPv4],[IPv6],<interface> (3 or 4 comma-separated fields).";
        if (!DnsmasqScopedDomainSyntax.IsValidDnsName(tokens[0]))
            return "dynamic-host name must be a valid DNS name.";
        var interfaceIndex = tokens.Length - 1;
        if (tokens[1].Length > 0 && !IPAddress.TryParse(tokens[1], out _))
            return "dynamic-host IPv4 must be a valid address or empty.";
        if (tokens.Length == 4 && tokens[2].Length > 0 && !IPAddress.TryParse(tokens[2], out _))
            return "dynamic-host IPv6 must be a valid address or empty.";
        if (tokens[interfaceIndex].Length == 0)
            return "dynamic-host interface cannot be empty.";
        if (!DnsmasqRelaySyntax.IsInterfaceName(tokens[interfaceIndex]))
            return "dynamic-host interface must be a valid interface name.";
        return null;
    }
}
