using DnsmasqWebUI.Infrastructure.Services.EffectiveConfig.Metadata;
using DnsmasqWebUI.Infrastructure.Services.EffectiveConfig.Validation.Abstractions;
using DnsmasqWebUI.Infrastructure.Serialization.Parsers.EffectiveConfig;

namespace DnsmasqWebUI.Infrastructure.Services.EffectiveConfig.Validation.Handlers;

/// <summary>
/// Semantic validation for <c>auth-sec-servers</c> values: &lt;domain&gt;[,&lt;domain&gt;...].
/// One or more domain names (secondary servers for authoritative zones).
/// </summary>
public sealed class AuthSecServersSemanticHandler : IOptionSemanticHandler
{
    public bool CanHandle(string optionName) => optionName == DnsmasqConfKeys.AuthSecServers;

    public string? ValidateSingle(object? value) => null;

    public string? ValidateMultiItem(string? value)
    {
        var s = (value ?? "").Trim();
        if (s.Length == 0)
            return "auth-sec-servers value cannot be empty.";
        var tokens = s.Split(',', StringSplitOptions.TrimEntries);
        if (tokens.Length == 0 || tokens.Any(t => t.Length == 0))
            return "auth-sec-servers must contain at least one domain name.";
        foreach (var t in tokens)
        {
            if (!DnsmasqScopedDomainSyntax.IsValidDnsName(t))
                return "auth-sec-servers each domain must be a valid DNS name.";
        }
        return null;
    }
}
