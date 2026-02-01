using DnsmasqWebUI.Client.Models;
using DnsmasqWebUI.Services.Abstractions;

namespace DnsmasqWebUI.Client.Services.Abstractions;

/// <summary>
/// Loads and saves client-side preferences (e.g. polling intervals) to browser local storage.
/// Registered as scoped via assembly scanning (<see cref="IApplicationScopedService"/>).
/// </summary>
public interface IClientSettingsService : IApplicationScopedService
{
    /// <summary>Loads settings from local storage; returns defaults if missing or invalid.</summary>
    Task<ClientSettings> LoadSettingsAsync();

    /// <summary>Persists settings to local storage.</summary>
    Task SaveSettingsAsync(ClientSettings settings);
}
