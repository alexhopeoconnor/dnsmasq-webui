namespace DnsmasqWebUI.Models.Dnsmasq;

/// <summary>Result of probing dnsmasq version and comparing to minimum required.</summary>
/// <param name="InstalledVersion">Parsed version from version command output; null if probe failed or could not parse.</param>
/// <param name="MinimumVersion">Configured minimum required version.</param>
/// <param name="ProbeSucceeded">True when the version command ran and output could be parsed.</param>
/// <param name="IsSupported">True when probe succeeded and installed version is at least minimum.</param>
/// <param name="ProbeCommand">The command that was run (e.g. "dnsmasq --version").</param>
/// <param name="Error">Error message when probe failed or version could not be parsed; null on success.</param>
/// <param name="Capabilities">Compile-time options parsed from version output (DHCP, TFTP, DNSSEC, DBus). Empty when probe failed or line not found.</param>
public record DnsmasqVersionInfo(
    Version? InstalledVersion,
    Version MinimumVersion,
    bool ProbeSucceeded,
    bool IsSupported,
    string ProbeCommand,
    string? Error,
    DnsmasqCompileCapabilities Capabilities);
