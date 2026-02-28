using DnsmasqWebUI.Infrastructure.Services.Dnsmasq.Reload.Abstractions;

namespace DnsmasqWebUI.Infrastructure.Client.Abstractions;

/// <summary>Typed client for POST api/reload.</summary>
public interface IReloadClient
{
    /// <summary>Triggers dnsmasq reload via POST api/reload.</summary>
    Task<ReloadResult> ReloadAsync(CancellationToken ct = default);
}
