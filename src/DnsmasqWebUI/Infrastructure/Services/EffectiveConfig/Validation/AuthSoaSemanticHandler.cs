using System.Globalization;
using DnsmasqWebUI.Infrastructure.Helpers.Config;

namespace DnsmasqWebUI.Infrastructure.Services.EffectiveConfig.Validation;

/// <summary>
/// Semantic validation for <c>auth-soa</c> values: &lt;serial&gt;[,&lt;hostmaster&gt;[,&lt;refresh&gt;[,&lt;retry&gt;[,&lt;expiry&gt;]]]].
/// Serial required; optional hostmaster (DNS name), refresh, retry, expiry (numbers).
/// </summary>
public sealed class AuthSoaSemanticHandler : IOptionSemanticHandler
{
    public bool CanHandle(string optionName) => optionName == DnsmasqConfKeys.AuthSoa;

    public string? ValidateSingle(object? value) => null;

    public string? ValidateMultiItem(string? value)
    {
        var s = (value ?? "").Trim();
        if (s.Length == 0)
            return "auth-soa value cannot be empty.";
        var tokens = s.Split(',', StringSplitOptions.TrimEntries);
        if (tokens.Length is 0 or > 5)
            return "auth-soa must be <serial>[,<hostmaster>[,<refresh>[,<retry>[,<expiry>]]]].";
        if (tokens.Any(t => t.Length == 0))
            return "auth-soa fields cannot be empty.";
        if (!uint.TryParse(tokens[0], NumberStyles.None, CultureInfo.InvariantCulture, out _))
            return "auth-soa serial must be a number.";
        if (tokens.Length >= 2 && !DnsmasqScopedDomainSyntax.IsValidDnsName(tokens[1]))
            return "auth-soa hostmaster must be a valid DNS name.";
        for (var i = 2; i < tokens.Length; i++)
        {
            if (!uint.TryParse(tokens[i], NumberStyles.None, CultureInfo.InvariantCulture, out _))
                return "auth-soa refresh, retry and expiry must be numbers.";
        }
        return null;
    }
}
