using System.Net;
using DnsmasqWebUI.Infrastructure.Services.EffectiveConfig.Metadata;
using DnsmasqWebUI.Infrastructure.Services.EffectiveConfig.Validation.Abstractions;
using DnsmasqWebUI.Infrastructure.Serialization.Parsers.EffectiveConfig;

namespace DnsmasqWebUI.Infrastructure.Services.EffectiveConfig.Validation.Handlers;

/// <summary>
/// Specialized semantic behavior for <c>address</c> values.
/// Validates the <c>/domain[/domain...]/ip</c> structure and accepts empty or <c>#</c> address forms.
/// </summary>
public sealed class AddressSemanticHandler : IOptionSemanticHandler
{
    public bool CanHandle(string optionName) =>
        optionName == DnsmasqConfKeys.Address;

    public string? ValidateSingle(object? value) => null;

    public string? ValidateMultiItem(string? value)
    {
        var s = value?.Trim() ?? "";
        if (s.Length == 0)
            return "Value cannot be empty.";

        if (!DnsmasqScopedDomainSyntax.TrySplitScopedValue(s, out var domainParts, out var addressPart, out var error))
            return error == "Value must start with '/'."
                ? "Address must start with '/'."
                : "Address must use /domain[/domain...]/ip syntax.";

        error = DnsmasqScopedDomainSyntax.ValidateDomainPatterns(domainParts);
        if (error is not null)
            return error.Replace("Value", "Address", StringComparison.Ordinal);

        addressPart = addressPart.Trim();
        if (addressPart.Length == 0 || addressPart == "#")
            return null;

        return IPAddress.TryParse(addressPart, out _)
            ? null
            : "Address target must be empty, '#', or a valid IP address.";
    }
}
