using DnsmasqWebUI.Infrastructure.Helpers.Config;

namespace DnsmasqWebUI.Infrastructure.Services.EffectiveConfig.Validation;

/// <summary>
/// Semantic validation for <c>ptr-record</c> values: &lt;name&gt;[,&lt;target&gt;].
/// Name required; optional target (both must be valid DNS names).
/// </summary>
public sealed class PtrRecordSemanticHandler : IOptionSemanticHandler
{
    public bool CanHandle(string optionName) => optionName == DnsmasqConfKeys.PtrRecord;

    public string? ValidateSingle(object? value) => null;

    public string? ValidateMultiItem(string? value)
    {
        var s = (value ?? "").Trim();
        if (s.Length == 0)
            return "ptr-record value cannot be empty.";
        var tokens = s.Split(',', StringSplitOptions.TrimEntries);
        if (tokens.Length is 0 or > 2)
            return "ptr-record must be <name> or <name>,<target>.";
        if (tokens.Any(t => t.Length == 0))
            return "ptr-record name and target cannot be empty.";
        if (!DnsmasqScopedDomainSyntax.IsValidDnsName(tokens[0]))
            return "ptr-record name must be a valid DNS name.";
        if (tokens.Length == 2 && !DnsmasqScopedDomainSyntax.IsValidDnsName(tokens[1]))
            return "ptr-record target must be a valid DNS name.";
        return null;
    }
}
