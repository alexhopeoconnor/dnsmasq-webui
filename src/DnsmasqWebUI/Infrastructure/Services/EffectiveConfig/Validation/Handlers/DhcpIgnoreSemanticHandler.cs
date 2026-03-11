using DnsmasqWebUI.Infrastructure.Services.EffectiveConfig.Metadata;
using DnsmasqWebUI.Infrastructure.Services.EffectiveConfig.Validation.Abstractions;
using DnsmasqWebUI.Infrastructure.Serialization.Parsers.EffectiveConfig;

namespace DnsmasqWebUI.Infrastructure.Services.EffectiveConfig.Validation.Handlers;

/// <summary>
/// Semantic validation for <c>dhcp-ignore</c> values.
/// </summary>
public sealed class DhcpIgnoreSemanticHandler : IOptionSemanticHandler
{
    public bool CanHandle(string optionName) => optionName == DnsmasqConfKeys.DhcpIgnore;

    public string? ValidateSingle(object? value) => null;

    public string? ValidateMultiItem(string? value)
    {
        var s = value?.Trim() ?? "";
        if (s.Length == 0)
            return "Value cannot be empty.";

        var tokens = s.Split(',', StringSplitOptions.TrimEntries);
        if (tokens.Length == 0 || tokens.Any(t => t.Length == 0))
            return "dhcp-ignore must contain one or more tag:<tag> clauses.";

        return tokens.All(t => DnsmasqDhcpTagSyntax.IsTagToken(t, allowNegation: true))
            ? null
            : "dhcp-ignore only supports tag:<tag> and tag:!<tag> clauses.";
    }
}
