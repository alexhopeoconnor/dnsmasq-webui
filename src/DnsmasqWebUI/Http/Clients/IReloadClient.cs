using DnsmasqWebUI.Services.Abstractions;

namespace DnsmasqWebUI.Http.Clients;

/// <summary>Typed client for POST api/reload.</summary>
public interface IReloadClient
{
    Task<ReloadResult> ReloadAsync(CancellationToken ct = default);
}
