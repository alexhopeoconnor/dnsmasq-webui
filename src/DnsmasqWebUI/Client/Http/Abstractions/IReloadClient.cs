using DnsmasqWebUI.Services.Abstractions;

namespace DnsmasqWebUI.Client.Http.Abstractions;

/// <summary>Typed client for POST api/reload.</summary>
public interface IReloadClient
{
    Task<ReloadResult> ReloadAsync(CancellationToken ct = default);
}
