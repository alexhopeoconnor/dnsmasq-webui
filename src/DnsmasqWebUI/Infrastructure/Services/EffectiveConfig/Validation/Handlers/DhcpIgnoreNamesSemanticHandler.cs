using DnsmasqWebUI.Infrastructure.Services.EffectiveConfig.Metadata;
using DnsmasqWebUI.Infrastructure.Services.EffectiveConfig.Validation.Abstractions;
using DnsmasqWebUI.Infrastructure.Serialization.Parsers.EffectiveConfig;

namespace DnsmasqWebUI.Infrastructure.Services.EffectiveConfig.Validation.Handlers;

/// <summary>
/// Semantic validation for <c>dhcp-ignore-names</c> values.
/// Accepts no tags (global) or one or more tag clauses.
/// </summary>
public sealed class DhcpIgnoreNamesSemanticHandler : IOptionSemanticHandler
{
    public bool CanHandle(string optionName) => optionName == DnsmasqConfKeys.DhcpIgnoreNames;

    public string? ValidateSingle(object? value) => null;

    public string? ValidateMultiItem(string? value)
    {
        var s = value?.Trim() ?? "";
        if (s.Length == 0)
            return null;

        var tokens = s.Split(',', StringSplitOptions.TrimEntries);
        if (tokens.Any(t => t.Length == 0))
            return "dhcp-ignore-names contains an empty comma-separated segment.";

        return tokens.All(t => DnsmasqDhcpTagSyntax.IsTagToken(t))
            ? null
            : "dhcp-ignore-names only supports tag:<tag> clauses.";
    }
}
