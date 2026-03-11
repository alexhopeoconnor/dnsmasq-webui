using DnsmasqWebUI.Infrastructure.Services.EffectiveConfig.Metadata;
using DnsmasqWebUI.Infrastructure.Services.EffectiveConfig.Validation.Abstractions;
using DnsmasqWebUI.Infrastructure.Serialization.Parsers.EffectiveConfig;

namespace DnsmasqWebUI.Infrastructure.Services.EffectiveConfig.Validation.Handlers;

/// <summary>
/// Semantic validation for <c>mx-host</c> values: &lt;mx name&gt;[[,&lt;hostname&gt;],&lt;preference&gt;].
/// MX name required; optional hostname and/or preference (integer).
/// </summary>
public sealed class MxHostSemanticHandler : IOptionSemanticHandler
{
    public bool CanHandle(string optionName) => optionName == DnsmasqConfKeys.MxHost;

    public string? ValidateSingle(object? value) => null;

    public string? ValidateMultiItem(string? value)
    {
        var s = (value ?? "").Trim();
        if (s.Length == 0)
            return "mx-host value cannot be empty.";
        var tokens = s.Split(',', StringSplitOptions.TrimEntries);
        if (tokens.Length is 0 or > 3)
            return "mx-host must be <mx name> or <mx name>,<hostname> or <mx name>[,<hostname>],<preference>.";
        if (tokens.Any(t => t.Length == 0))
            return "mx-host list cannot contain empty parts.";
        if (!DnsmasqScopedDomainSyntax.IsValidDnsName(tokens[0]))
            return "mx-host MX name must be a valid DNS name.";
        if (tokens.Length >= 2)
        {
            var lastIsPreference = int.TryParse(tokens[^1], out var pref) && pref >= 0 && pref <= 65535;
            if (tokens.Length == 2)
            {
                if (!lastIsPreference && !DnsmasqScopedDomainSyntax.IsValidDnsName(tokens[1]))
                    return "mx-host hostname or preference must be a valid DNS name or 0-65535.";
            }
            else
            {
                if (!lastIsPreference)
                    return "mx-host preference must be 0-65535.";
                if (!DnsmasqScopedDomainSyntax.IsValidDnsName(tokens[1]))
                    return "mx-host hostname must be a valid DNS name.";
            }
        }
        return null;
    }
}
