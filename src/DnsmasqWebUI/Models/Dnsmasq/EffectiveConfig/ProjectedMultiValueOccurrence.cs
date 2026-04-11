namespace DnsmasqWebUI.Models.Dnsmasq.EffectiveConfig;

/// <summary>
/// Draft-aware projection of a current effective multi-value item back onto the best available baseline source metadata.
/// Used by specialized pages to stay in sync with the effective-config session without mutating the baseline status snapshot.
/// </summary>
public sealed record ProjectedMultiValueOccurrence(
    string OccurrenceId,
    string Value,
    int EffectiveIndex,
    ConfigValueSource? Source,
    bool IsDraftOnly,
    bool IsEditable,
    string? DisplaySourcePath,
    string? DisplaySourceLabel);
