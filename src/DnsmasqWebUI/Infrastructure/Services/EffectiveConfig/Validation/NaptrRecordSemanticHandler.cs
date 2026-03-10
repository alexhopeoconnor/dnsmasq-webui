using DnsmasqWebUI.Infrastructure.Helpers.Config;

namespace DnsmasqWebUI.Infrastructure.Services.EffectiveConfig.Validation;

/// <summary>
/// Semantic validation for <c>naptr-record</c> values: &lt;name&gt;,&lt;order&gt;,&lt;preference&gt;,&lt;flags&gt;,&lt;service&gt;,&lt;regexp&gt;[,&lt;replacement&gt;] (RFC3403).
/// Six required fields; optional replacement.
/// </summary>
public sealed class NaptrRecordSemanticHandler : IOptionSemanticHandler
{
    public bool CanHandle(string optionName) => optionName == DnsmasqConfKeys.NaptrRecord;

    public string? ValidateSingle(object? value) => null;

    public string? ValidateMultiItem(string? value)
    {
        var s = (value ?? "").Trim();
        if (s.Length == 0)
            return "naptr-record value cannot be empty.";
        var tokens = s.Split(',', StringSplitOptions.TrimEntries);
        if (tokens.Length is < 6 or > 7)
            return "naptr-record must be <name>,<order>,<preference>,<flags>,<service>,<regexp>[,<replacement>].";
        if (tokens.Take(6).Any(t => t.Length == 0))
            return "naptr-record name, order, preference, flags, service and regexp cannot be empty.";
        if (!DnsmasqScopedDomainSyntax.IsValidDnsName(tokens[0]))
            return "naptr-record name must be a valid DNS name.";
        if (!ushort.TryParse(tokens[1], out _))
            return "naptr-record order must be 0-65535.";
        if (!ushort.TryParse(tokens[2], out _))
            return "naptr-record preference must be 0-65535.";
        return null;
    }
}
