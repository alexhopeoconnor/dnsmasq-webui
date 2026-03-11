using DnsmasqWebUI.Infrastructure.Services.EffectiveConfig.Metadata;
using DnsmasqWebUI.Infrastructure.Services.EffectiveConfig.Validation.Abstractions;
using DnsmasqWebUI.Infrastructure.Serialization.Parsers.EffectiveConfig;

namespace DnsmasqWebUI.Infrastructure.Services.EffectiveConfig.Validation.Handlers;

/// <summary>
/// Semantic validation for <c>rebind-domain-ok</c> values.
/// Accepts either a single domain or the scoped /domain/domain/ syntax.
/// </summary>
public sealed class RebindDomainOkSemanticHandler : IOptionSemanticHandler
{
    public bool CanHandle(string optionName) => optionName == DnsmasqConfKeys.RebindDomainOk;

    public string? ValidateSingle(object? value) => null;

    public string? ValidateMultiItem(string? value)
    {
        var s = value?.Trim() ?? "";
        if (s.Length == 0)
            return "Value cannot be empty.";

        if (s.StartsWith("/", StringComparison.Ordinal))
        {
            if (!DnsmasqScopedDomainSyntax.TrySplitScopedValue(s, out var domains, out var tail, out var error))
                return "rebind-domain-ok must use /domain[/domain...]/ syntax.";
            if (tail.Length != 0)
                return "rebind-domain-ok must not include a value after the final '/'.";
            error = DnsmasqScopedDomainSyntax.ValidateDomainPatterns(domains, allowUnqualifiedMarker: false, allowHash: false);
            return error?.Replace("Value", "rebind-domain-ok", StringComparison.Ordinal);
        }

        return DnsmasqScopedDomainSyntax.ValidateDomainPatterns([s], allowUnqualifiedMarker: false, allowHash: false)
            ?.Replace("Value", "rebind-domain-ok", StringComparison.Ordinal);
    }
}
