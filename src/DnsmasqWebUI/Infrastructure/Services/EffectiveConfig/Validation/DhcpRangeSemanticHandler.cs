using DnsmasqWebUI.Infrastructure.Helpers.Config;

namespace DnsmasqWebUI.Infrastructure.Services.EffectiveConfig.Validation;

/// <summary>
/// Specialized semantic behavior for <c>dhcp-range</c> values.
/// Uses conservative validation: optional leading tag/set tokens, then a required start address,
/// followed by a required second token which may be an end address or a mode keyword.
/// </summary>
public sealed class DhcpRangeSemanticHandler : IOptionSemanticHandler
{
    public bool CanHandle(string optionName) =>
        optionName == DnsmasqConfKeys.DhcpRange;

    public string? ValidateSingle(object? value) => null;

    public string? ValidateMultiItem(string? value)
    {
        return DnsmasqDhcpRangeValueParser.TryParse(value ?? "", out _, out var error)
            ? null
            : error;
    }
}
