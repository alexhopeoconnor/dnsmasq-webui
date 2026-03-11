using System.Net;
using DnsmasqWebUI.Infrastructure.Services.EffectiveConfig.Metadata;
using DnsmasqWebUI.Infrastructure.Services.EffectiveConfig.Validation.Abstractions;
using DnsmasqWebUI.Infrastructure.Serialization.Parsers.EffectiveConfig;

namespace DnsmasqWebUI.Infrastructure.Services.EffectiveConfig.Validation.Handlers;

/// <summary>
/// Conservative semantic validation for <c>slaac</c> values.
/// Accepts interface-like tokens, IPv6 literals, and documented SLAAC mode keywords.
/// </summary>
public sealed class SlaacSemanticHandler : IOptionSemanticHandler
{
    private static readonly HashSet<string> ModeKeywords = new(StringComparer.OrdinalIgnoreCase)
    {
        "ra-only",
        "slaac",
        "ra-names",
        "ra-stateless",
        "ra-advrouter",
        "off-link",
    };

    public bool CanHandle(string optionName) => optionName == DnsmasqConfKeys.Slaac;

    public string? ValidateSingle(object? value) => null;

    public string? ValidateMultiItem(string? value)
    {
        var s = value?.Trim() ?? "";
        if (s.Length == 0)
            return "Value cannot be empty.";

        var tokens = s.Split(',', StringSplitOptions.TrimEntries);
        if (tokens.Any(t => t.Length == 0))
            return "slaac contains an empty comma-separated segment.";

        foreach (var token in tokens)
        {
            if (ModeKeywords.Contains(token))
                continue;
            if (DnsmasqDhcpTagSyntax.IsInterfaceLike(token, allowWildcard: true))
                continue;
            if (IPAddress.TryParse(token, out _))
                continue;
            if (token.StartsWith("[", StringComparison.Ordinal) && token.EndsWith("]", StringComparison.Ordinal))
                continue;

            return $"Invalid slaac segment '{token}'.";
        }

        return null;
    }
}
