namespace DnsmasqWebUI.Models.Dnsmasq.EffectiveConfig;

/// <summary>
/// Severity of a validation issue for an effective-config field.
/// </summary>
public enum FieldIssueSeverity
{
    Warning,
    Error
}

/// <summary>
/// A single validation issue for a field (inline or cross-option). Errors block save; warnings can be confirmed.
/// </summary>
/// <param name="FieldKey">Key identifying the field (e.g. sectionId:optionName).</param>
/// <param name="Message">User-facing message.</param>
/// <param name="Severity">Warning or Error.</param>
/// <param name="ItemIndex">Optional 0-based index for multi-value item-level issues.</param>
public sealed record FieldIssue(string FieldKey, string Message, FieldIssueSeverity Severity, int? ItemIndex = null);
