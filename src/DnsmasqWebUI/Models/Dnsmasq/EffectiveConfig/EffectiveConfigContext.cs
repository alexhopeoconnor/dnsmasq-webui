namespace DnsmasqWebUI.Models.Dnsmasq.EffectiveConfig;

/// <summary>
/// Where the effective config component is shown. Drives which sections and (optionally) which fields are visible.
/// </summary>
public enum EffectiveConfigContext
{
    /// <summary>Full config: all sections, all fields (e.g. Dnsmasq page).</summary>
    All,

    /// <summary>Hosts page: only hosts-related section and fields.</summary>
    Hosts,

    /// <summary>DHCP page: DHCP and TFTP/PXE sections.</summary>
    Dhcp,
}
