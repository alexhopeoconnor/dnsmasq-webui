using DnsmasqWebUI.Infrastructure.Services.EffectiveConfig.Metadata;
using DnsmasqWebUI.Infrastructure.Services.EffectiveConfig.Validation.Abstractions;
using DnsmasqWebUI.Infrastructure.Serialization.Parsers.EffectiveConfig;

namespace DnsmasqWebUI.Infrastructure.Services.EffectiveConfig.Validation.Handlers;

/// <summary>
/// Semantic validation for <c>auth-zone</c> values: &lt;domain&gt;[,&lt;subnet&gt;[/prefix]...][,exclude:&lt;subnet&gt;...].
/// Domain required; optional subnets or exclude: clauses (interface names or CIDR).
/// </summary>
public sealed class AuthZoneSemanticHandler : IOptionSemanticHandler
{
    public bool CanHandle(string optionName) => optionName == DnsmasqConfKeys.AuthZone;

    public string? ValidateSingle(object? value) => null;

    public string? ValidateMultiItem(string? value)
    {
        var s = (value ?? "").Trim();
        if (s.Length == 0)
            return "auth-zone value cannot be empty.";
        var tokens = s.Split(',', StringSplitOptions.TrimEntries);
        if (tokens.Length == 0 || tokens[0].Length == 0)
            return "auth-zone requires a domain name.";
        if (!DnsmasqScopedDomainSyntax.IsValidDnsName(tokens[0]))
            return "auth-zone domain must be a valid DNS name.";
        for (var i = 1; i < tokens.Length; i++)
        {
            if (tokens[i].Length == 0)
                return "auth-zone cannot have empty subnet or exclude fields.";
        }
        return null;
    }
}
