namespace DnsmasqWebUI.Models.Dnsmasq.EffectiveConfig;

/// <summary>
/// A single pending change from the effective-config edit session.
/// Only applied when the user confirms Save in the confirmation modal.
/// </summary>
/// <param name="SectionId">Section the option belongs to (for display).</param>
/// <param name="OptionName">Dnsmasq option name (e.g. from DnsmasqConfKeys).</param>
/// <param name="OldValue">Value before edit (effective value at commit time).</param>
/// <param name="NewValue">User-edited value (bool for flags, int? for port/ints, string? for others).</param>
/// <param name="CurrentSourceFilePath">Path of the file that currently sets this value; shown in modal as "will override X".</param>
public record PendingEffectiveConfigChange(
    string SectionId,
    string OptionName,
    object? OldValue,
    object? NewValue,
    string? CurrentSourceFilePath = null);
