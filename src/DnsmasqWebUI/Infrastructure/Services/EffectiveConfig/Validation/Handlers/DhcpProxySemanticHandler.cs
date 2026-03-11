using DnsmasqWebUI.Infrastructure.Services.EffectiveConfig.Metadata;
using DnsmasqWebUI.Infrastructure.Services.EffectiveConfig.Validation.Abstractions;
using DnsmasqWebUI.Infrastructure.Serialization.Parsers.EffectiveConfig;

namespace DnsmasqWebUI.Infrastructure.Services.EffectiveConfig.Validation.Handlers;

/// <summary>
/// Specialized semantic behavior for <c>dhcp-proxy</c> values.
/// The UI currently only supports explicit values, so validation focuses on IP-literal lists.
/// </summary>
public sealed class DhcpProxySemanticHandler : IOptionSemanticHandler
{
    public bool CanHandle(string optionName) =>
        optionName == DnsmasqConfKeys.DhcpProxy;

    public string? ValidateSingle(object? value) => null;

    public string? ValidateMultiItem(string? value)
    {
        var s = value?.Trim() ?? "";
        if (s.Length == 0)
            return "Value cannot be empty.";

        var tokens = DnsmasqRelaySyntax.SplitTokens(s);
        if (DnsmasqRelaySyntax.HasEmptyToken(tokens))
            return "dhcp-proxy contains an empty comma-separated segment.";

        return tokens.All(DnsmasqRelaySyntax.IsIpLiteral)
            ? null
            : "dhcp-proxy must contain one or more IP literal addresses.";
    }
}
