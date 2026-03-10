using DnsmasqWebUI.Infrastructure.Helpers.Config;

namespace DnsmasqWebUI.Infrastructure.Services.EffectiveConfig.Validation;

/// <summary>
/// Conservative semantic behavior for <c>dhcp-match</c> values.
/// Requires a leading set:&lt;tag&gt; and a non-empty option selector, with optional match value.
/// </summary>
public sealed class DhcpMatchSemanticHandler : IOptionSemanticHandler
{
    public bool CanHandle(string optionName) =>
        optionName == DnsmasqConfKeys.DhcpMatch;

    public string? ValidateSingle(object? value) => null;

    public string? ValidateMultiItem(string? value)
    {
        var s = value?.Trim() ?? "";
        if (s.Length == 0)
            return "Value cannot be empty.";

        var tokens = DnsmasqDhcpOptionSyntax.SplitTokens(s);
        if (tokens.Length < 2 || DnsmasqDhcpOptionSyntax.HasEmptyToken(tokens))
            return "dhcp-match must be set:<tag>,option-spec[,value].";

        if (!tokens[0].StartsWith("set:", StringComparison.OrdinalIgnoreCase) || tokens[0].Length <= 4)
            return "dhcp-match must start with set:<tag>.";

        return DnsmasqDhcpOptionSyntax.IsOptionSelector(tokens[1])
            ? null
            : $"Invalid dhcp-match option spec '{tokens[1]}'.";
    }
}
