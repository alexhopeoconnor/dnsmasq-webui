using DnsmasqWebUI.Infrastructure.Helpers.Config;

namespace DnsmasqWebUI.Infrastructure.Services.EffectiveConfig.Validation;

/// <summary>
/// Semantic validation for <c>dhcp-vendorclass</c> values.
/// </summary>
public sealed class DhcpVendorclassSemanticHandler : IOptionSemanticHandler
{
    public bool CanHandle(string optionName) => optionName == DnsmasqConfKeys.DhcpVendorclass;

    public string? ValidateSingle(object? value) => null;

    public string? ValidateMultiItem(string? value)
    {
        var s = value?.Trim() ?? "";
        if (s.Length == 0)
            return "Value cannot be empty.";

        var tokens = s.Split(',', StringSplitOptions.TrimEntries);
        if (tokens.Length < 2 || tokens.Any(t => t.Length == 0))
            return "dhcp-vendorclass must be set:<tag>,[enterprise:<num>,]<vendor-class>.";

        if (!DnsmasqDhcpTagSyntax.IsSetToken(tokens[0], prefixOptional: true))
            return "dhcp-vendorclass must start with a tag (optionally prefixed by set:).";

        var index = 1;
        if (tokens[index].StartsWith("enterprise:", StringComparison.OrdinalIgnoreCase))
        {
            var enterprise = tokens[index]["enterprise:".Length..];
            if (!uint.TryParse(enterprise, out _))
                return "dhcp-vendorclass enterprise: value must be numeric.";
            index++;
        }

        if (index >= tokens.Length)
            return "dhcp-vendorclass must include a vendor-class string.";

        return null;
    }
}
