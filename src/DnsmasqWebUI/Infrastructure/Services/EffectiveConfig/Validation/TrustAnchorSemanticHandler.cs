using DnsmasqWebUI.Infrastructure.Helpers.Config;

namespace DnsmasqWebUI.Infrastructure.Services.EffectiveConfig.Validation;

/// <summary>
/// Specialized semantic behavior for <c>trust-anchor</c> values.
/// Supports negative trust anchors (domain[,class]) and DS-record forms.
/// </summary>
public sealed class TrustAnchorSemanticHandler : IOptionSemanticHandler
{
    private static readonly HashSet<string> AllowedClasses = new(StringComparer.OrdinalIgnoreCase)
    {
        "IN",
        "CH",
        "HS",
    };

    public bool CanHandle(string optionName) =>
        optionName == DnsmasqConfKeys.TrustAnchor;

    public string? ValidateSingle(object? value) => null;

    public string? ValidateMultiItem(string? value)
    {
        var s = value?.Trim() ?? "";
        if (s.Length == 0)
            return "Value cannot be empty.";

        var tokens = s.Split(',').Select(t => t.Trim()).ToArray();
        if (tokens.Any(t => t.Length == 0))
            return "trust-anchor contains an empty comma-separated segment.";

        if (!IsValidAnchorDomain(tokens[0]))
            return "trust-anchor must start with a valid domain name or '.'.";

        return tokens.Length switch
        {
            1 => null,
            2 => IsValidClass(tokens[1]) ? null : "trust-anchor class must be IN, CH, HS, or a numeric DNS class.",
            5 => ValidateDsTuple(tokens, hasClass: false),
            6 => IsValidClass(tokens[1])
                ? ValidateDsTuple(tokens, hasClass: true)
                : "trust-anchor class must be IN, CH, HS, or a numeric DNS class.",
            _ => "trust-anchor must be domain[,class] or domain[,class],key-tag,algorithm,digest-type,digest.",
        };
    }

    private static string? ValidateDsTuple(string[] tokens, bool hasClass)
    {
        var offset = hasClass ? 2 : 1;
        if (!ushort.TryParse(tokens[offset], out _))
            return "trust-anchor key-tag must be numeric.";
        if (!byte.TryParse(tokens[offset + 1], out _))
            return "trust-anchor algorithm must be numeric.";
        if (!byte.TryParse(tokens[offset + 2], out _))
            return "trust-anchor digest-type must be numeric.";
        return tokens[offset + 3].Length > 0
            ? null
            : "trust-anchor digest cannot be empty.";
    }

    private static bool IsValidClass(string value) =>
        AllowedClasses.Contains(value) || ushort.TryParse(value, out _);

    private static bool IsValidAnchorDomain(string value)
    {
        if (value == ".")
            return true;

        var normalized = value.EndsWith(".", StringComparison.Ordinal) ? value[..^1] : value;
        return DnsmasqScopedDomainSyntax.ValidateDomainPatterns([normalized], allowUnqualifiedMarker: false, allowHash: false) is null;
    }
}
