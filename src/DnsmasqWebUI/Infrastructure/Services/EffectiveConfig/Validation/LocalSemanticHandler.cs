using DnsmasqWebUI.Infrastructure.Helpers.Config;

namespace DnsmasqWebUI.Infrastructure.Services.EffectiveConfig.Validation;

/// <summary>
/// Specialized semantic behavior for <c>local</c> values.
/// This is the local-only form of server domain matching, so values must use scoped
/// <c>/domain[/domain...]/</c> syntax or <c>//</c> for unqualified names.
/// </summary>
public sealed class LocalSemanticHandler : IOptionSemanticHandler
{
    public bool CanHandle(string optionName) =>
        optionName == DnsmasqConfKeys.Local;

    public string? ValidateSingle(object? value) => null;

    public string? ValidateMultiItem(string? value)
    {
        var s = value?.Trim() ?? "";
        if (s.Length == 0)
            return "Value cannot be empty.";

        if (!DnsmasqScopedDomainSyntax.TrySplitScopedValue(s, out var domains, out var tail, out var error))
            return error == "Value must start with '/'."
                ? "Local must start with '/'."
                : "Local must use /domain[/domain...]/ syntax.";

        if (tail.Length != 0)
            return "Local must end with a trailing '/' and not include an upstream server.";

        error = DnsmasqScopedDomainSyntax.ValidateDomainPatterns(domains);
        return error?.Replace("Value", "Local", StringComparison.Ordinal);
    }
}
