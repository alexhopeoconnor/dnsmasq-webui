namespace DnsmasqWebUI.Models.Filters;

/// <summary>Tracks an in-progress edit of an existing multi-value resolver line.</summary>
public sealed record FilterPolicyEditorState(
    FilterPolicyKind Kind,
    string? OriginalRawValue);
