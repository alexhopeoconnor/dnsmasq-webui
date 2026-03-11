using DnsmasqWebUI.Infrastructure.Services.EffectiveConfig.Metadata;
using DnsmasqWebUI.Infrastructure.Services.EffectiveConfig.Validation.Abstractions;
using DnsmasqWebUI.Infrastructure.Serialization.Parsers.EffectiveConfig;

namespace DnsmasqWebUI.Infrastructure.Services.EffectiveConfig.Validation.Handlers;

/// <summary>
/// Specialized semantic behavior for <c>ipset</c> values.
/// Validates scoped domain syntax followed by one or more ipset names.
/// </summary>
public sealed class IpsetSemanticHandler : IOptionSemanticHandler
{
    public bool CanHandle(string optionName) =>
        optionName == DnsmasqConfKeys.Ipset;

    public string? ValidateSingle(object? value) => null;

    public string? ValidateMultiItem(string? value)
    {
        var s = value?.Trim() ?? "";
        if (s.Length == 0)
            return "Value cannot be empty.";

        if (!DnsmasqScopedDomainSyntax.TrySplitScopedValue(s, out var domains, out var setList, out var error))
            return error == "Value must start with '/'."
                ? "ipset must start with '/'."
                : "ipset must use /domain[/domain...]/set[,set...] syntax.";

        error = DnsmasqScopedDomainSyntax.ValidateDomainPatterns(domains);
        if (error is not null)
            return error.Replace("Value", "ipset", StringComparison.Ordinal);

        var sets = setList.Split(',').Select(t => t.Trim()).ToArray();
        if (sets.Length == 0 || sets.Any(string.IsNullOrWhiteSpace))
            return "ipset must include at least one non-empty set name.";

        return null;
    }
}
