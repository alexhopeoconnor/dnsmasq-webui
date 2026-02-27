namespace DnsmasqWebUI.Models.Dnsmasq.EffectiveConfig;

/// <summary>
/// Raised when the user leaves the active edit field (blur) and the value was valid and changed.
/// Section adds to pending changes and clears the active field.
/// </summary>
public record EffectiveConfigEditCommittedArgs(
    string SectionId,
    string OptionName,
    object? OldValue,
    object? NewValue,
    string? CurrentSourceFilePath);
