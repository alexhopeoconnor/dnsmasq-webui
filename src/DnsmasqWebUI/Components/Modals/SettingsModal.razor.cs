using DnsmasqWebUI.Client.Models;
using DnsmasqWebUI.Client.Services.Abstractions;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;

namespace DnsmasqWebUI.Components.Modals;

/// <summary>
/// Modal for editing client-side settings. Uses native HTML &lt;dialog&gt; via JS interop.
/// Implements IAsyncDisposable: disposes <see cref="IJSObjectReference"/> and <see cref="DotNetObjectReference{T}"/> when the component is removed.
/// </summary>
public partial class SettingsModal : IAsyncDisposable
{
    private ElementReference _dialogRef;
    private IJSObjectReference? _jsModule;
    private DotNetObjectReference<SettingsModal>? _dotNetRef;
    private ClientSettings _editingSettings = new();
    private string _searchTerm = string.Empty;
    private bool _moduleLoaded;
    private bool _dialogInitialized;
    private bool _wasVisible;

    [Parameter] public bool IsVisible { get; set; }

    [Parameter] public string Title { get; set; } = "Client settings";

    [Parameter] public SettingsModalContext SettingsContext { get; set; } = SettingsModalContext.All;

    [Parameter] public EventCallback OnClose { get; set; }

    protected override async Task OnParametersSetAsync()
    {
        var justOpened = IsVisible && !_wasVisible;
        if (justOpened)
            _dialogInitialized = false;
        _wasVisible = IsVisible;
        // Only load from storage when the modal opens; parent re-renders (e.g. refresh timer) must not overwrite in-progress edits
        if (justOpened)
        {
            _editingSettings = await ClientSettingsService.LoadSettingsAsync();
            _editingSettings = new ClientSettings
            {
                ServiceStatusPollingIntervalSeconds = _editingSettings.ServiceStatusPollingIntervalSeconds,
                RecentLogsPollingIntervalSeconds = _editingSettings.RecentLogsPollingIntervalSeconds
            };
        }
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            _jsModule = await JSRuntime.InvokeAsync<IJSObjectReference>("import", "./js/settings-modal.js");
            _dotNetRef = DotNetObjectReference.Create(this);
            _moduleLoaded = true;
        }

        if (IsVisible && _moduleLoaded && _jsModule != null)
        {
            if (!_dialogInitialized)
            {
                await _jsModule.InvokeVoidAsync("initDialog", _dialogRef, _dotNetRef);
                _dialogInitialized = true;
            }
            await _jsModule.InvokeVoidAsync("showModal", _dialogRef);
        }
    }

    private bool ShouldShowSection(string key)
    {
        if (SettingsContext == SettingsModalContext.ServicePolling)
            return key == "ServiceStatus";
        if (SettingsContext == SettingsModalContext.LogsPolling)
            return key == "Logs";
        if (SettingsContext == SettingsModalContext.All)
        {
            if (string.IsNullOrWhiteSpace(_searchTerm)) return true;
            var term = _searchTerm.Trim();
            return key switch
            {
                "ServiceStatus" => "service status polling".Contains(term, StringComparison.OrdinalIgnoreCase),
                "Logs" => "recent logs polling".Contains(term, StringComparison.OrdinalIgnoreCase),
                _ => false
            };
        }
        return false;
    }

    private async Task Save()
    {
        _editingSettings.ServiceStatusPollingIntervalSeconds = Math.Clamp(
            _editingSettings.ServiceStatusPollingIntervalSeconds, 5, 300);
        _editingSettings.RecentLogsPollingIntervalSeconds = Math.Clamp(
            _editingSettings.RecentLogsPollingIntervalSeconds, 5, 300);

        await ClientSettingsService.SaveSettingsAsync(_editingSettings);
        if (_jsModule != null)
            await _jsModule.InvokeVoidAsync("closeModal", _dialogRef);
        await OnClose.InvokeAsync();
    }

    private async Task Close()
    {
        if (_jsModule != null)
            await _jsModule.InvokeVoidAsync("closeModal", _dialogRef);
        await OnClose.InvokeAsync();
    }

    [JSInvokable]
    public async Task OnDialogClosed(string returnValue)
    {
        await OnClose.InvokeAsync();
        await InvokeAsync(StateHasChanged);
    }

    /// <summary>
    /// Disposes JS module and DotNetObjectReference. Blazor calls this when the component is removed from the tree.
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        try
        {
            if (_jsModule != null)
                await _jsModule.DisposeAsync();
        }
        finally
        {
            _jsModule = null;
        }
        _dotNetRef?.Dispose();
        _dotNetRef = null;
    }
}
