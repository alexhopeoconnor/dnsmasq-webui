namespace DnsmasqWebUI.Models.Dnsmasq;

/// <summary>
/// Compile-time capabilities parsed from dnsmasq --version (e.g. "Compile time options: ... DHCP ... DNSSEC ...").
/// Used to disable/hide options in the UI when the running dnsmasq binary does not support them.
/// </summary>
public record DnsmasqCompileCapabilities(
    bool Dhcp,
    bool Tftp,
    bool Dnssec,
    bool Dbus,
    IReadOnlySet<string> RawTokens
);
