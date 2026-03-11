using DnsmasqWebUI.Infrastructure.Services.EffectiveConfig.Metadata;
using DnsmasqWebUI.Infrastructure.Services.EffectiveConfig.Validation.Abstractions;
using DnsmasqWebUI.Infrastructure.Serialization.Parsers.EffectiveConfig;

namespace DnsmasqWebUI.Infrastructure.Services.EffectiveConfig.Validation.Handlers;

/// <summary>
/// Semantic validation for <c>dhcp-name-match</c> values.
/// </summary>
public sealed class DhcpNameMatchSemanticHandler : IOptionSemanticHandler
{
    public bool CanHandle(string optionName) => optionName == DnsmasqConfKeys.DhcpNameMatch;

    public string? ValidateSingle(object? value) => null;

    public string? ValidateMultiItem(string? value)
    {
        var s = value?.Trim() ?? "";
        if (s.Length == 0)
            return "Value cannot be empty.";

        var tokens = s.Split(',', StringSplitOptions.TrimEntries);
        if (tokens.Length != 2 || tokens.Any(t => t.Length == 0))
            return "dhcp-name-match must be set:<tag>,<name>[*].";

        if (!DnsmasqDhcpTagSyntax.IsSetToken(tokens[0]))
            return "dhcp-name-match must start with set:<tag>.";

        var pattern = tokens[1];
        var starCount = pattern.Count(c => c == '*');
        if (starCount > 1 || (starCount == 1 && !pattern.EndsWith('*')))
            return "dhcp-name-match allows at most one trailing '*' wildcard.";

        return pattern.TrimEnd('*').Length > 0
            ? null
            : "dhcp-name-match name pattern cannot be empty.";
    }
}
