using DnsmasqWebUI.Infrastructure.Services.Dnsmasq.Reload.Abstractions;

namespace DnsmasqWebUI.Models.Dnsmasq;

/// <summary>Result of a save operation that triggers a dnsmasq reload (e.g. PUT api/hosts, PUT api/dhcp/hosts).</summary>
/// <param name="Saved">True when the write succeeded (reload result may still indicate failure).</param>
/// <param name="Reload">Result of the reload command run after save.</param>
public record SaveWithReloadResult(bool Saved, ReloadResult Reload);
