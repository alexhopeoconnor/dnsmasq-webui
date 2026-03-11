using System.Net;
using DnsmasqWebUI.Infrastructure.Services.EffectiveConfig.Metadata;
using DnsmasqWebUI.Infrastructure.Services.EffectiveConfig.Validation.Abstractions;
using DnsmasqWebUI.Infrastructure.Serialization.Parsers.EffectiveConfig;

namespace DnsmasqWebUI.Infrastructure.Services.EffectiveConfig.Validation.Handlers;

/// <summary>
/// Semantic validation for <c>host-record</c> values: &lt;name&gt;[,&lt;name&gt;...],[&lt;IPv4&gt;],[&lt;IPv6&gt;][,&lt;TTL&gt;].
/// At least one name required; optional IPv4, IPv6, TTL (positive integer).
/// </summary>
public sealed class HostRecordSemanticHandler : IOptionSemanticHandler
{
    public bool CanHandle(string optionName) => optionName == DnsmasqConfKeys.HostRecord;

    public string? ValidateSingle(object? value) => null;

    public string? ValidateMultiItem(string? value)
    {
        var s = (value ?? "").Trim();
        if (s.Length == 0)
            return "host-record value cannot be empty.";
        var tokens = s.Split(',', StringSplitOptions.TrimEntries);
        if (tokens.Length == 0 || tokens[0].Length == 0)
            return "host-record requires at least one name.";
        if (!DnsmasqScopedDomainSyntax.IsValidDnsName(tokens[0]))
            return "host-record name(s) must be valid DNS names.";
        var lastIsTtl = tokens.Length >= 2 && int.TryParse(tokens[^1], out var ttl) && ttl > 0;
        var lastIndex = lastIsTtl ? tokens.Length - 2 : tokens.Length - 1;
        for (var i = 1; i <= lastIndex; i++)
        {
            var t = tokens[i];
            if (t.Length == 0)
                return "host-record cannot have empty fields.";
            if (IPAddress.TryParse(t, out _))
                continue;
            if (int.TryParse(t, out _))
                return "host-record TTL must be last and positive; names and addresses cannot be numeric-only.";
            if (!DnsmasqScopedDomainSyntax.IsValidDnsName(t))
                return "host-record name must be a valid DNS name.";
        }
        return null;
    }
}
