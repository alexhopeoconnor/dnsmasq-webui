using DnsmasqWebUI.Infrastructure.Services.Abstractions;

namespace DnsmasqWebUI.Models.Dnsmasq;

/// <summary>Result of a save operation that triggers a dnsmasq reload (e.g. PUT api/hosts, PUT api/dhcp/hosts).</summary>
public record SaveWithReloadResult(bool Saved, ReloadResult Reload);
