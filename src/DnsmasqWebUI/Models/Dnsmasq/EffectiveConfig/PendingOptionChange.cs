namespace DnsmasqWebUI.Models.Dnsmasq.EffectiveConfig;

/// <summary>A pending change for one config option. Written to the managed config file on save.</summary>
public sealed record PendingOptionChange(
    string SectionId,
    string OptionName,
    object? OldValue,
    object? NewValue,
    string? CurrentSourceFilePath = null)
    : PendingDnsmasqChange($"{SectionId}:{OptionName}");
