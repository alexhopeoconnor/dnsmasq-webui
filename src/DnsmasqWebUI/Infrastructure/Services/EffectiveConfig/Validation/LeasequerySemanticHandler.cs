using System.Net;
using DnsmasqWebUI.Infrastructure.Helpers.Config;

namespace DnsmasqWebUI.Infrastructure.Services.EffectiveConfig.Validation;

/// <summary>
/// Specialized semantic behavior for <c>leasequery</c> values.
/// Each item is either key-only or an IP address with an optional numeric prefix.
/// </summary>
public sealed class LeasequerySemanticHandler : IOptionSemanticHandler
{
    public bool CanHandle(string optionName) =>
        optionName == DnsmasqConfKeys.Leasequery;

    public string? ValidateSingle(object? value) => null;

    public string? ValidateMultiItem(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        var parts = value.Trim().Split('/', 2);
        if (!IPAddress.TryParse(parts[0], out _))
            return "Leasequery source must be an IP address.";

        if (parts.Length == 2 && !int.TryParse(parts[1], out _))
            return "Prefix must be numeric.";

        return null;
    }
}
