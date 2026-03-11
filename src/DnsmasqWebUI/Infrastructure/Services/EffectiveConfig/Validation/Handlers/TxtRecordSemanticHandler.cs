using DnsmasqWebUI.Infrastructure.Services.EffectiveConfig.Metadata;
using DnsmasqWebUI.Infrastructure.Services.EffectiveConfig.Validation.Abstractions;
using DnsmasqWebUI.Infrastructure.Serialization.Parsers.EffectiveConfig;

namespace DnsmasqWebUI.Infrastructure.Services.EffectiveConfig.Validation.Handlers;

/// <summary>
/// Semantic validation for <c>txt-record</c> values: &lt;name&gt;[[,&lt;text&gt;],&lt;text&gt;].
/// Name required (first comma-separated field); optional text strings (commas allowed in quoted strings).
/// </summary>
public sealed class TxtRecordSemanticHandler : IOptionSemanticHandler
{
    public bool CanHandle(string optionName) => optionName == DnsmasqConfKeys.TxtRecord;

    public string? ValidateSingle(object? value) => null;

    public string? ValidateMultiItem(string? value)
    {
        var s = (value ?? "").Trim();
        if (s.Length == 0)
            return "txt-record value cannot be empty.";
        var name = s.Contains(',') ? s.Split(',', 2, StringSplitOptions.TrimEntries)[0].Trim() : s;
        if (name.Length == 0)
            return "txt-record name cannot be empty.";
        if (!DnsmasqScopedDomainSyntax.IsValidDnsName(name))
            return "txt-record name must be a valid DNS name.";
        return null;
    }
}
