using System.Text.RegularExpressions;

namespace DnsmasqWebUI.Infrastructure.Services.EffectiveConfig.Validation;

/// <summary>
/// Shared parsing/validation helpers for dnsmasq option values that use the
/// <c>/domain[/domain...]/tail</c> syntax shared by server/local/address/ipset/nftset.
/// </summary>
internal static partial class DnsmasqScopedDomainSyntax
{
    [GeneratedRegex(@"^[A-Za-z0-9]([A-Za-z0-9-]*[A-Za-z0-9])?(\.[A-Za-z0-9]([A-Za-z0-9-]*[A-Za-z0-9])?)*$", RegexOptions.CultureInvariant)]
    private static partial Regex DomainPattern();

    public static bool TrySplitScopedValue(string value, out string[] domains, out string tail, out string? error)
    {
        domains = Array.Empty<string>();
        tail = "";
        error = null;

        if (!value.StartsWith("/", StringComparison.Ordinal))
        {
            error = "Value must start with '/'.";
            return false;
        }

        var lastSlash = value.LastIndexOf('/');
        if (lastSlash <= 0)
        {
            error = "Value must use /domain[/domain...]/... syntax.";
            return false;
        }

        domains = value[1..lastSlash].Split('/');
        tail = value[(lastSlash + 1)..];
        return true;
    }

    public static string? ValidateDomainPatterns(IEnumerable<string> domains, bool allowUnqualifiedMarker = true, bool allowHash = true)
    {
        var list = domains.ToArray();
        if (list.Length == 0)
            return "Value must include at least one domain pattern.";

        if (allowUnqualifiedMarker && list.Length == 1 && list[0].Length == 0)
            return null; // "//" means unqualified names only

        foreach (var domain in list)
        {
            if (string.IsNullOrWhiteSpace(domain))
                return "Value contains an empty domain pattern.";

            if (allowHash && domain == "#")
                continue;

            var normalized = domain;
            if (normalized.StartsWith("*", StringComparison.Ordinal))
                normalized = normalized[1..];
            if (normalized.StartsWith(".", StringComparison.Ordinal))
                normalized = normalized[1..];

            if (normalized.Length == 0 || normalized.Length > 253 || !DomainPattern().IsMatch(normalized))
                return $"Invalid domain pattern '{domain}'.";
        }

        return null;
    }
}
