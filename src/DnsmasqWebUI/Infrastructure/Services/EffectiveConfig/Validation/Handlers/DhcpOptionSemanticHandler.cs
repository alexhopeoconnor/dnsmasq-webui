using DnsmasqWebUI.Infrastructure.Services.EffectiveConfig.Metadata;
using DnsmasqWebUI.Infrastructure.Services.EffectiveConfig.Validation.Abstractions;
using DnsmasqWebUI.Infrastructure.Serialization.Parsers.EffectiveConfig;

namespace DnsmasqWebUI.Infrastructure.Services.EffectiveConfig.Validation.Handlers;

/// <summary>
/// Conservative semantic validation for <c>dhcp-option</c> and <c>dhcp-option-force</c> values.
/// Validates the presence and basic shape of known prefix tokens and the required option selector.
/// </summary>
public sealed class DhcpOptionSemanticHandler : IOptionSemanticHandler
{
    public bool CanHandle(string optionName) =>
        optionName is DnsmasqConfKeys.DhcpOption or DnsmasqConfKeys.DhcpOptionForce;

    public string? ValidateSingle(object? value) => null;

    public string? ValidateMultiItem(string? value)
    {
        var s = value?.Trim() ?? "";
        if (s.Length == 0)
            return "Value cannot be empty.";

        var tokens = DnsmasqDhcpOptionSyntax.SplitTokens(s);
        if (DnsmasqDhcpOptionSyntax.HasEmptyToken(tokens))
            return "dhcp-option contains an empty comma-separated segment.";

        var index = 0;
        while (index < tokens.Length && DnsmasqDhcpOptionSyntax.IsPrefixToken(tokens[index], out var error))
        {
            if (error is not null)
                return error;
            index++;
        }

        if (index >= tokens.Length)
            return "dhcp-option must include an option selector after any prefixes.";

        return DnsmasqDhcpOptionSyntax.IsOptionSelector(tokens[index])
            ? null
            : $"Invalid dhcp-option selector '{tokens[index]}'.";
    }
}
