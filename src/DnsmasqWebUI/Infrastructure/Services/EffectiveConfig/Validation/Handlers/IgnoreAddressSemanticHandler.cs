using System.Net;
using DnsmasqWebUI.Infrastructure.Services.EffectiveConfig.Metadata;
using DnsmasqWebUI.Infrastructure.Services.EffectiveConfig.Validation.Abstractions;
using DnsmasqWebUI.Infrastructure.Serialization.Parsers.EffectiveConfig;

namespace DnsmasqWebUI.Infrastructure.Services.EffectiveConfig.Validation.Handlers;

/// <summary>
/// Specialized semantic behavior for <c>ignore-address</c> values.
/// Accepts an IP literal with an optional numeric prefix length.
/// </summary>
public sealed class IgnoreAddressSemanticHandler : IOptionSemanticHandler
{
    public bool CanHandle(string optionName) =>
        optionName == DnsmasqConfKeys.IgnoreAddress;

    public string? ValidateSingle(object? value) => null;

    public string? ValidateMultiItem(string? value)
    {
        var s = value?.Trim() ?? "";
        if (s.Length == 0)
            return "Value cannot be empty.";
        return DnsmasqIpPrefixSyntax.ValidateIpWithOptionalPrefix(s, "ignore-address");
    }
}
