using DnsmasqWebUI.Infrastructure.Services.Abstractions;

namespace DnsmasqWebUI.Infrastructure.Client.Abstractions;

/// <summary>Typed client for POST api/reload.</summary>
public interface IReloadClient
{
    Task<ReloadResult> ReloadAsync(CancellationToken ct = default);
}
