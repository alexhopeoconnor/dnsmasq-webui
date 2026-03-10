using DnsmasqWebUI.Infrastructure.Helpers.Config;

namespace DnsmasqWebUI.Infrastructure.Services.EffectiveConfig.Validation;

/// <summary>
/// Semantic validation for <c>cname</c> values: &lt;cname&gt;[,&lt;cname&gt;,]&lt;target&gt;[,&lt;TTL&gt;].
/// At least one cname and target required; optional TTL as positive integer.
/// </summary>
public sealed class CnameSemanticHandler : IOptionSemanticHandler
{
    public bool CanHandle(string optionName) => optionName == DnsmasqConfKeys.Cname;

    public string? ValidateSingle(object? value) => null;

    public string? ValidateMultiItem(string? value)
    {
        var s = (value ?? "").Trim();
        if (s.Length == 0)
            return "cname value cannot be empty.";
        var tokens = s.Split(',', StringSplitOptions.TrimEntries);
        if (tokens.Length < 2)
            return "cname must be <cname>,<target> or <cname>[,<cname>...],<target>[,<TTL>].";
        if (tokens.Any(t => t.Length == 0))
            return "cname list cannot contain empty parts.";
        var lastIsTtl = tokens.Length >= 3 && int.TryParse(tokens[^1], out var ttl) && ttl > 0;
        var targetIndex = lastIsTtl ? tokens.Length - 2 : tokens.Length - 1;
        if (targetIndex < 1)
            return "cname requires at least one cname and a target.";
        for (var i = 0; i <= targetIndex; i++)
        {
            if (!DnsmasqScopedDomainSyntax.IsValidDnsName(tokens[i]))
                return "cname and target must be valid DNS names (e.g. host.example.com).";
        }
        return null;
    }
}
