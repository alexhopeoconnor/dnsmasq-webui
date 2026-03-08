namespace DnsmasqWebUI.Models.Dnsmasq.EffectiveConfig;

/// <summary>
/// Tri-state for inverse-pair options (e.g. do-0x20-encode / no-0x20-encode).
/// Default = not set (remove both lines); Enabled = write the positive key; Disabled = write the negative key.
/// </summary>
public enum ExplicitToggleState
{
    Default,
    Enabled,
    Disabled,
}
