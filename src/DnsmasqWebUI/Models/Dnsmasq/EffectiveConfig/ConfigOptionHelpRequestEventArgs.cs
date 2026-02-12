namespace DnsmasqWebUI.Models.Dnsmasq.EffectiveConfig;

/// <summary>
/// Event args when a child requests to show or dismiss the config option help popover.
/// Parent (EffectiveConfigSection) shows ConfigOptionHelpModal at the anchor; when
/// <see cref="IsLabelMouseLeave"/> is true, parent schedules close if mouse does not enter the modal.
/// </summary>
public sealed class ConfigOptionHelpRequestEventArgs
{
    /// <summary>Option-help file key (e.g. "server", "no-hosts").</summary>
    public string HelpKey { get; init; } = "";

    /// <summary>Display label for the modal title (e.g. "server / local").</summary>
    public string OptionLabel { get; init; } = "";

    /// <summary>Id of the label element that triggered the request; used to position the modal and match leave events.</summary>
    public string AnchorId { get; init; } = "";

    /// <summary>When true, the label lost mouse focus; parent should schedule close unless mouse enters the modal.</summary>
    public bool IsLabelMouseLeave { get; init; }
}
