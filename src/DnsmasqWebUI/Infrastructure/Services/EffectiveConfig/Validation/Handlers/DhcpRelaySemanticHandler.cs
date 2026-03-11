using DnsmasqWebUI.Infrastructure.Services.EffectiveConfig.Metadata;
using DnsmasqWebUI.Infrastructure.Services.EffectiveConfig.Validation.Abstractions;
using DnsmasqWebUI.Infrastructure.Serialization.Parsers.EffectiveConfig;

namespace DnsmasqWebUI.Infrastructure.Services.EffectiveConfig.Validation.Handlers;

/// <summary>
/// Specialized semantic behavior for <c>dhcp-relay</c> values.
/// </summary>
public sealed class DhcpRelaySemanticHandler : IOptionSemanticHandler
{
    public bool CanHandle(string optionName) =>
        optionName == DnsmasqConfKeys.DhcpRelay;

    public string? ValidateSingle(object? value) => null;

    public string? ValidateMultiItem(string? value)
    {
        var s = value?.Trim() ?? "";
        if (s.Length == 0)
            return "Value cannot be empty.";

        var tokens = DnsmasqRelaySyntax.SplitTokens(s);
        if (tokens.Length is < 1 or > 3 || DnsmasqRelaySyntax.HasEmptyToken(tokens))
            return "dhcp-relay must be local-address[,server-address[#port]][,interface].";

        if (!DnsmasqRelaySyntax.IsIpLiteral(tokens[0]))
            return "dhcp-relay must start with a local IP address.";

        if (tokens.Length == 1)
            return null;

        if (tokens.Length == 2)
        {
            return DnsmasqRelaySyntax.IsServerAddress(tokens[1]) || DnsmasqRelaySyntax.IsInterfaceName(tokens[1])
                ? null
                : "dhcp-relay second value must be a server IP[#port] or interface name.";
        }

        if (!DnsmasqRelaySyntax.IsServerAddress(tokens[1]))
            return "dhcp-relay server value must be an IP address with optional #port.";
        if (!DnsmasqRelaySyntax.IsInterfaceName(tokens[2]))
            return "dhcp-relay interface value must be a valid interface name.";
        return null;
    }
}
