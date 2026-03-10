using DnsmasqWebUI.Infrastructure.Helpers.Config;

namespace DnsmasqWebUI.Infrastructure.Services.EffectiveConfig.Validation;

/// <summary>
/// Semantic validation for <c>dhcp-option-pxe</c> values.
/// This is a narrower PXE-specific form of dhcp-option with a required numeric option selector.
/// </summary>
public sealed class DhcpOptionPxeSemanticHandler : IOptionSemanticHandler
{
    public bool CanHandle(string optionName) => optionName == DnsmasqConfKeys.DhcpOptionPxe;

    public string? ValidateSingle(object? value) => null;

    public string? ValidateMultiItem(string? value)
    {
        var s = value?.Trim() ?? "";
        if (s.Length == 0)
            return "Value cannot be empty.";

        var tokens = DnsmasqDhcpOptionSyntax.SplitTokens(s);
        if (DnsmasqDhcpOptionSyntax.HasEmptyToken(tokens))
            return "dhcp-option-pxe contains an empty comma-separated segment.";

        var index = 0;
        while (index < tokens.Length && DnsmasqDhcpOptionSyntax.IsPrefixToken(tokens[index], out var error))
        {
            if (error is not null)
                return error;
            index++;
        }

        if (index >= tokens.Length)
            return "dhcp-option-pxe must include a numeric option selector after any prefixes.";

        return int.TryParse(tokens[index], out _)
            ? null
            : $"Invalid dhcp-option-pxe selector '{tokens[index]}'.";
    }
}
