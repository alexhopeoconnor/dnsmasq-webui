using DnsmasqWebUI.Infrastructure.Services.EffectiveConfig.Metadata;
using DnsmasqWebUI.Infrastructure.Services.EffectiveConfig.Validation.Abstractions;
using DnsmasqWebUI.Infrastructure.Serialization.Parsers.EffectiveConfig;

namespace DnsmasqWebUI.Infrastructure.Services.EffectiveConfig.Validation.Handlers;

/// <summary>
/// Specialized semantic behavior for <c>nftset</c> values.
/// Validates scoped domain syntax followed by one or more nftables set specifications.
/// </summary>
public sealed class NftsetSemanticHandler : IOptionSemanticHandler
{
    public bool CanHandle(string optionName) =>
        optionName == DnsmasqConfKeys.Nftset;

    public string? ValidateSingle(object? value) => null;

    public string? ValidateMultiItem(string? value)
    {
        var s = value?.Trim() ?? "";
        if (s.Length == 0)
            return "Value cannot be empty.";

        if (!DnsmasqScopedDomainSyntax.TrySplitScopedValue(s, out var domains, out var specList, out var error))
            return error == "Value must start with '/'."
                ? "nftset must start with '/'."
                : "nftset must use /domain[/domain...]/set-spec[,set-spec...] syntax.";

        error = DnsmasqScopedDomainSyntax.ValidateDomainPatterns(domains);
        if (error is not null)
            return error.Replace("Value", "nftset", StringComparison.Ordinal);

        var specs = specList.Split(',').Select(t => t.Trim()).ToArray();
        if (specs.Length == 0 || specs.Any(string.IsNullOrWhiteSpace))
            return "nftset must include at least one non-empty set specification.";

        foreach (var spec in specs)
        {
            if (!IsValidSetSpec(spec))
                return $"Invalid nftset specification '{spec}'.";
        }

        return null;
    }

    private static bool IsValidSetSpec(string spec)
    {
        var parts = spec.Split('#');
        if (parts.Any(p => p.Length == 0))
            return false;

        if (parts[0] is "4" or "6")
            return parts.Length is 3 or 4;

        return parts.Length == 3;
    }
}
