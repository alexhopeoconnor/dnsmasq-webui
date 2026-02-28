using System.Text.Json;
using DnsmasqWebUI.Extensions;
using DnsmasqWebUI.Models.Client;
using DnsmasqWebUI.Infrastructure.Services.UI.Settings.Abstractions;
using Microsoft.JSInterop;

namespace DnsmasqWebUI.Infrastructure.Services.UI.Settings;

/// <summary>
/// Loads and saves client-side settings to browser local storage via JS interop.
/// Scoped per Blazor circuit; disposes the JS module when the scope is disposed.
/// </summary>
public sealed class ClientSettingsService : IClientSettingsService, IAsyncDisposable
{
    private readonly IJSRuntime _jsRuntime;
    private readonly ILogger<ClientSettingsService> _logger;
    private IJSObjectReference? _module;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        WriteIndented = false
    };

    public ClientSettingsService(IJSRuntime jsRuntime, ILogger<ClientSettingsService> logger)
    {
        _jsRuntime = jsRuntime;
        _logger = logger;
    }

    private async Task<IJSObjectReference?> GetModuleAsync()
    {
        if (_module != null) return _module;
        try
        {
            _module = await _jsRuntime.InvokeAsync<IJSObjectReference>(
                "import", "./js/client-settings-storage.js");
        }
        catch (InvalidOperationException ex) { _logger.LogDebug(ex, "JS import skipped: prerender (no JS available)"); return null; }
        catch (JSDisconnectedException ex) { _logger.LogDebug(ex, "JS import skipped: circuit disconnected"); return null; }
        catch (JSException ex) { _logger.LogDebug(ex, "JS import failed"); return null; }
        return _module;
    }

    /// <inheritdoc />
    public async Task<ClientSettings> LoadSettingsAsync()
    {
        var module = await GetModuleAsync();
        if (module == null) return new ClientSettings();
        var json = await module.InvokeAsyncSafe<string?>("getItem");
        if (string.IsNullOrWhiteSpace(json))
        {
            var defaults = new ClientSettings();
            ClientSettingsFields.HydrateFrom(defaults);
            return defaults;
        }
        try
        {
            var settings = JsonSerializer.Deserialize<ClientSettings>(json, JsonOptions) ?? new ClientSettings();
            ClientSettingsFields.HydrateFrom(settings);
            return settings;
        }
        catch
        {
            var defaults = new ClientSettings();
            ClientSettingsFields.HydrateFrom(defaults);
            return defaults;
        }
    }

    /// <inheritdoc />
    public async Task SaveSettingsAsync(ClientSettings settings)
    {
        var module = await GetModuleAsync();
        if (module == null) return;
        var json = JsonSerializer.Serialize(settings, JsonOptions);
        await module.InvokeVoidAsyncSafe("setItem", json);
    }

    /// <summary>
    /// Disposes the JS module reference. Called when the Blazor circuit scope is disposed.
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        await _module.DisposeAsyncSafe();
        _module = null;
    }
}
