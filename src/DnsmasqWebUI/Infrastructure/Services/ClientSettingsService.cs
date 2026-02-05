using System.Text.Json;
using DnsmasqWebUI.Models.Client;
using DnsmasqWebUI.Infrastructure.Services.Abstractions;
using Microsoft.JSInterop;

namespace DnsmasqWebUI.Infrastructure.Services;

/// <summary>
/// Loads and saves client-side settings to browser local storage via JS interop.
/// Scoped per Blazor circuit; disposes the JS module when the scope is disposed.
/// </summary>
public sealed class ClientSettingsService : IClientSettingsService, IAsyncDisposable
{
    private readonly IJSRuntime _jsRuntime;
    private IJSObjectReference? _module;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        WriteIndented = false
    };

    public ClientSettingsService(IJSRuntime jsRuntime) => _jsRuntime = jsRuntime;

    private async Task<IJSObjectReference> GetModuleAsync()
    {
        if (_module != null) return _module;
        _module = await _jsRuntime.InvokeAsync<IJSObjectReference>(
            "import", "./js/client-settings-storage.js");
        return _module;
    }

    /// <inheritdoc />
    public async Task<ClientSettings> LoadSettingsAsync()
    {
        var module = await GetModuleAsync();
        var json = await module.InvokeAsync<string?>("getItem");
        if (string.IsNullOrWhiteSpace(json)) return new ClientSettings();
        try
        {
            return JsonSerializer.Deserialize<ClientSettings>(json, JsonOptions) ?? new ClientSettings();
        }
        catch
        {
            return new ClientSettings();
        }
    }

    /// <inheritdoc />
    public async Task SaveSettingsAsync(ClientSettings settings)
    {
        var json = JsonSerializer.Serialize(settings, JsonOptions);
        var module = await GetModuleAsync();
        await module.InvokeVoidAsync("setItem", json);
    }

    /// <summary>
    /// Disposes the JS module reference. Called when the Blazor circuit scope is disposed.
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        if (_module == null) return;
        try
        {
            await _module.DisposeAsync();
        }
        finally
        {
            _module = null;
        }
    }
}
