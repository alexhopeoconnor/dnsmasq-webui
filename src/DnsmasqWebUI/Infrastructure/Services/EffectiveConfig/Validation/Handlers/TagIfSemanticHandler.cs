using DnsmasqWebUI.Infrastructure.Services.EffectiveConfig.Metadata;
using DnsmasqWebUI.Infrastructure.Services.EffectiveConfig.Validation.Abstractions;
using DnsmasqWebUI.Infrastructure.Serialization.Parsers.EffectiveConfig;

namespace DnsmasqWebUI.Infrastructure.Services.EffectiveConfig.Validation.Handlers;

/// <summary>
/// Semantic validation for <c>tag-if</c> values.
/// </summary>
public sealed class TagIfSemanticHandler : IOptionSemanticHandler
{
    public bool CanHandle(string optionName) => optionName == DnsmasqConfKeys.TagIf;

    public string? ValidateSingle(object? value) => null;

    public string? ValidateMultiItem(string? value)
    {
        var s = value?.Trim() ?? "";
        if (s.Length == 0)
            return "Value cannot be empty.";

        var tokens = s.Split(',', StringSplitOptions.TrimEntries);
        if (tokens.Length == 0 || tokens.Any(t => t.Length == 0))
            return "tag-if must contain set:<tag> and optional tag:<tag> clauses.";

        var hasSet = false;
        foreach (var token in tokens)
        {
            if (DnsmasqDhcpTagSyntax.IsSetToken(token))
            {
                hasSet = true;
                continue;
            }

            if (!DnsmasqDhcpTagSyntax.IsTagToken(token, allowNegation: true))
                return $"Invalid tag-if segment '{token}'.";
        }

        return hasSet
            ? null
            : "tag-if must contain at least one set:<tag> clause.";
    }
}
