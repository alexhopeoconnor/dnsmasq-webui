using System.Net;
using System.Linq;
using System.Text.RegularExpressions;
using DnsmasqWebUI.Infrastructure.Helpers.Config;

namespace DnsmasqWebUI.Infrastructure.Services.EffectiveConfig.Validation;

/// <summary>
/// Specialized semantic behavior for <c>address</c> values.
/// Validates the <c>/domain[/domain...]/ip</c> structure and accepts empty or <c>#</c> address forms.
/// </summary>
public sealed partial class AddressSemanticHandler : IOptionSemanticHandler
{
    [GeneratedRegex(@"^[A-Za-z0-9]([A-Za-z0-9-]*[A-Za-z0-9])?(\.[A-Za-z0-9]([A-Za-z0-9-]*[A-Za-z0-9])?)*$", RegexOptions.CultureInvariant)]
    private static partial Regex DomainPattern();

    public bool CanHandle(string optionName) =>
        optionName == DnsmasqConfKeys.Address;

    public string? ValidateSingle(object? value) => null;

    public string? ValidateMultiItem(string? value)
    {
        var s = value?.Trim() ?? "";
        if (s.Length == 0)
            return "Value cannot be empty.";

        if (!s.StartsWith("/", StringComparison.Ordinal))
            return "Address must start with '/'.";

        var parts = s.Split('/');
        if (parts.Length < 3)
            return "Address must use /domain[/domain...]/ip syntax.";

        var domainParts = parts.Skip(1).Take(parts.Length - 2).ToArray();
        if (domainParts.Length == 0 || domainParts.Any(string.IsNullOrWhiteSpace))
            return "Address must include at least one domain pattern.";

        foreach (var domain in domainParts)
        {
            if (!IsValidDomainPattern(domain))
                return $"Invalid domain pattern '{domain}'.";
        }

        var addressPart = parts[^1].Trim();
        if (addressPart.Length == 0 || addressPart == "#")
            return null;

        return IPAddress.TryParse(addressPart, out _)
            ? null
            : "Address target must be empty, '#', or a valid IP address.";
    }

    private static bool IsValidDomainPattern(string domain)
    {
        if (domain == "#")
            return true;

        var normalized = domain;
        if (normalized.StartsWith('*'))
            normalized = normalized[1..];
        if (normalized.StartsWith('.'))
            normalized = normalized[1..];

        return normalized.Length > 0 &&
               normalized.Length <= 253 &&
               DomainPattern().IsMatch(normalized);
    }
}
