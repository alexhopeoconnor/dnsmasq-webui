using DnsmasqWebUI.Infrastructure.Helpers.Config;

namespace DnsmasqWebUI.Infrastructure.Services.EffectiveConfig.Validation;

/// <summary>
/// Semantic validation for <c>bogus-nxdomain</c> values.
/// </summary>
public sealed class BogusNxdomainSemanticHandler : IOptionSemanticHandler
{
    public bool CanHandle(string optionName) => optionName == DnsmasqConfKeys.BogusNxdomain;

    public string? ValidateSingle(object? value) => null;

    public string? ValidateMultiItem(string? value)
    {
        var s = value?.Trim() ?? "";
        if (s.Length == 0)
            return "Value cannot be empty.";
        return DnsmasqIpPrefixSyntax.ValidateIpWithOptionalPrefix(s, "bogus-nxdomain");
    }
}
