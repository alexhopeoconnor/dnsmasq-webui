using System.Globalization;
using DnsmasqWebUI.Infrastructure.Services.EffectiveConfig.Metadata;
using DnsmasqWebUI.Infrastructure.Services.EffectiveConfig.Validation.Abstractions;
using DnsmasqWebUI.Infrastructure.Serialization.Parsers.EffectiveConfig;

namespace DnsmasqWebUI.Infrastructure.Services.EffectiveConfig.Validation.Handlers;

/// <summary>
/// Semantic validation for <c>auth-soa</c> values: &lt;serial&gt;[,&lt;hostmaster&gt;[,&lt;refresh&gt;[,&lt;retry&gt;[,&lt;expiry&gt;]]]].
/// Serial required; optional hostmaster (DNS name), refresh, retry, expiry (numbers).
/// An empty hostmaster field is allowed when later fields are present (positional commas).
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
        if (string.IsNullOrEmpty(tokens[0]))
            return "auth-soa serial cannot be empty.";
        if (!uint.TryParse(tokens[0], NumberStyles.None, CultureInfo.InvariantCulture, out _))
            return "auth-soa serial must be a number.";
        // Hostmaster (slot 1) may be empty to preserve positions when later timers are set (e.g. 1,,3600).
        if (tokens.Length >= 2 && tokens[1].Length > 0 && !DnsmasqScopedDomainSyntax.IsValidDnsName(tokens[1]))
            return "auth-soa hostmaster must be a valid DNS name.";
        for (var i = 2; i < tokens.Length; i++)
        {
            if (tokens[i].Length == 0)
                continue;
            if (!uint.TryParse(tokens[i], NumberStyles.None, CultureInfo.InvariantCulture, out _))
                return "auth-soa refresh, retry and expiry must be numbers.";
        }
        return null;
    }
}
