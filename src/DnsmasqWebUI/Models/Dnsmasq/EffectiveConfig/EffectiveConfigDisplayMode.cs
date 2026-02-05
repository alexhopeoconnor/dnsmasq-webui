namespace DnsmasqWebUI.Models.Dnsmasq.EffectiveConfig;

/// <summary>
/// Display mode for effective-config fields. Cascaded from the parent so that
/// OptionItem, MultiValueRow, and custom value displays can render view vs edit appropriately.
/// </summary>
public enum EffectiveConfigDisplayMode
{
    /// <summary>Show values as read-only text.</summary>
    View,

    /// <summary>Show editable controls (inputs, toggles). Not yet implemented; plumbing only.</summary>
    Edit,
}
