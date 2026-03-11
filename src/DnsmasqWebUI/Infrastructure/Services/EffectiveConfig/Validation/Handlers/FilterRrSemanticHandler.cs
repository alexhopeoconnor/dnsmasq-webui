using DnsmasqWebUI.Infrastructure.Services.EffectiveConfig.Metadata;
using DnsmasqWebUI.Infrastructure.Services.EffectiveConfig.Validation.Abstractions;
using DnsmasqWebUI.Infrastructure.Serialization.Parsers.EffectiveConfig;

namespace DnsmasqWebUI.Infrastructure.Services.EffectiveConfig.Validation.Handlers;

/// <summary>
/// Semantic validation for <c>filter-rr</c> values: RR-type or comma-separated list (e.g. A,AAAA,TXT or ANY).
/// </summary>
public sealed class FilterRrSemanticHandler : IOptionSemanticHandler
{
    public bool CanHandle(string optionName) => optionName == DnsmasqConfKeys.FilterRr;

    public string? ValidateSingle(object? value) => null;

    public string? ValidateMultiItem(string? value)
    {
        var s = (value ?? "").Trim();
        if (s.Length == 0)
            return "filter-rr value cannot be empty.";
        foreach (var token in s.Split(',', StringSplitOptions.TrimEntries))
        {
            if (token.Length == 0)
                return "filter-rr list cannot contain empty RR-type.";
            if (!DnsmasqRrTypeSyntax.IsValidRrType(token))
                return "filter-rr RR-type must be a name (e.g. A, TXT, ANY) or decimal number.";
        }
        return null;
    }
}
