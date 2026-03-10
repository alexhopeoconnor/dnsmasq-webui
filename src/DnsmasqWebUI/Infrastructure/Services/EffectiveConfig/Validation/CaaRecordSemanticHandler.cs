using DnsmasqWebUI.Infrastructure.Helpers.Config;

namespace DnsmasqWebUI.Infrastructure.Services.EffectiveConfig.Validation;

/// <summary>
/// Semantic validation for <c>caa-record</c> values: &lt;name&gt;,&lt;flags&gt;,&lt;tag&gt;,&lt;value&gt; (RFC6844).
/// All four parts required.
/// </summary>
public sealed class CaaRecordSemanticHandler : IOptionSemanticHandler
{
    public bool CanHandle(string optionName) => optionName == DnsmasqConfKeys.CaaRecord;

    public string? ValidateSingle(object? value) => null;

    public string? ValidateMultiItem(string? value)
    {
        var s = (value ?? "").Trim();
        if (s.Length == 0)
            return "caa-record value cannot be empty.";
        var tokens = s.Split(',', StringSplitOptions.TrimEntries);
        if (tokens.Length != 4)
            return "caa-record must be <name>,<flags>,<tag>,<value>.";
        if (tokens.Any(t => t.Length == 0))
            return "caa-record name, flags, tag and value cannot be empty.";
        if (!DnsmasqScopedDomainSyntax.IsValidDnsName(tokens[0]))
            return "caa-record name must be a valid DNS name.";
        if (!byte.TryParse(tokens[1], out _))
            return "caa-record flags must be 0-255.";
        return null;
    }
}
