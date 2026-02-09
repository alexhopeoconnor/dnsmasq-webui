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
        var module = await GetModuleAsync();
        if (module == null) return;
        var json = JsonSerializer.Serialize(settings, JsonOptions);
        await module.InvokeVoidAsync("setItem", json);
    }

    /// <summary>
    /// Disposes the JS module reference. Called when the Blazor circuit scope is disposed.
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        if (_module == null) return;
        try { await _module.DisposeAsync(); }
        catch (InvalidOperationException ex) { _logger.LogDebug(ex, "JS module DisposeAsync skipped (prerender)"); }
        catch (JSDisconnectedException ex) { _logger.LogDebug(ex, "JS module DisposeAsync skipped: circuit disconnected"); }
        catch (JSException ex) { _logger.LogDebug(ex, "JS module DisposeAsync failed"); }
        finally { _module = null; }
    }
}
