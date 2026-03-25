using DnsmasqWebUI.Models.Dnsmasq;

namespace DnsmasqWebUI.Models.Dnsmasq.PageStates;

/// <summary>
/// Page state for the DHCP specialized page. Derived from DnsmasqServiceStatus to keep Razor markup clean.
/// </summary>
public sealed record DhcpPageState(
    bool DhcpAvailable,
    string? DhcpUnavailableReason,
    bool TftpAvailable,
    string? TftpUnavailableReason,
    bool LeasesAvailable,
    string? LeasesUnavailableReason,
    bool ReadEthersEnabled)
{
    public static DhcpPageState FromStatus(DnsmasqServiceStatus? status)
    {
        if (status == null)
        {
            return new DhcpPageState(
                DhcpAvailable: false,
                DhcpUnavailableReason: "Status not available.",
                TftpAvailable: false,
                TftpUnavailableReason: "Status not available.",
                LeasesAvailable: false,
                LeasesUnavailableReason: "Status not available.",
                ReadEthersEnabled: false);
        }

        var dhcpAvailable = status.DnsmasqSupportsDhcp;
        var tftpAvailable = status.DnsmasqSupportsTftp && dhcpAvailable; // TFTP requires DHCP

        return new DhcpPageState(
            DhcpAvailable: dhcpAvailable,
            DhcpUnavailableReason: dhcpAvailable
                ? null
                : "Your dnsmasq was built without DHCP support. Install or build dnsmasq with DHCP enabled to use DHCP features.",
            TftpAvailable: tftpAvailable,
            TftpUnavailableReason: dhcpAvailable && !tftpAvailable
                ? "Your dnsmasq was built without TFTP support. Install or build dnsmasq with TFTP enabled to use PXE/TFTP features."
                : !dhcpAvailable
                    ? "TFTP requires DHCP support."
                    : null,
            LeasesAvailable: dhcpAvailable && status.LeasesPathConfigured,
            LeasesUnavailableReason: !dhcpAvailable
                ? "DHCP support is required for leases."
                : !status.LeasesPathConfigured
                    ? "Leases are not configured (no dhcp-leasefile in config)."
                    : null,
            ReadEthersEnabled: status.EffectiveConfig?.ReadEthers ?? false);
    }
}
