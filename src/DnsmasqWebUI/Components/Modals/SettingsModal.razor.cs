using DnsmasqWebUI.Models.Client;
using DnsmasqWebUI.Infrastructure.Services.Abstractions;
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
    private List<string> _validationErrors = new();
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
            _searchTerm = string.Empty;
            _validationErrors.Clear();
            _editingSettings = await ClientSettingsService.LoadSettingsAsync();
            _editingSettings = new ClientSettings
            {
                ServiceStatusPollingIntervalSeconds = _editingSettings.ServiceStatusPollingIntervalSeconds,
                RecentLogsPollingIntervalSeconds = _editingSettings.RecentLogsPollingIntervalSeconds,
                AppLogsPollingIntervalSeconds = _editingSettings.AppLogsPollingIntervalSeconds,
                LeasesPollingIntervalSeconds = _editingSettings.LeasesPollingIntervalSeconds,
                RecentLogsMaxLines = _editingSettings.RecentLogsMaxLines,
                RecentLogsAutoScroll = _editingSettings.RecentLogsAutoScroll,
                AppLogsMaxLines = _editingSettings.AppLogsMaxLines,
                AppLogsAutoScroll = _editingSettings.AppLogsAutoScroll
            };
        }
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            try
            {
                _jsModule = await JSRuntime.InvokeAsync<IJSObjectReference>("import", "./js/settings-modal.js");
                _dotNetRef = DotNetObjectReference.Create(this);
                _moduleLoaded = true;
            }
            catch (InvalidOperationException ex) { Logger.LogDebug(ex, "SettingsModal: JS import skipped (prerender)"); }
            catch (JSDisconnectedException ex) { Logger.LogDebug(ex, "SettingsModal: JS import skipped (circuit disconnected)"); }
            catch (JSException ex) { Logger.LogDebug(ex, "SettingsModal: JS import failed"); }
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

    private void FilterSettings() => StateHasChanged();

    private bool ShouldShowSection(string key)
    {
        var keys = SettingsModalSections.GetSectionKeysForContext(SettingsContext);
        if (keys != null)
            return keys.Any(k => string.Equals(key, k, StringComparison.OrdinalIgnoreCase));
        return SettingsModalSections.MatchesSearch(key, _searchTerm);
    }

    private void RunValidation()
    {
        _validationErrors.Clear();
        var checks = new (string Section, int Value, ClientSettingsFields.FieldBounds Bounds)[]
        {
            (SettingsModalSections.ServiceStatus, _editingSettings.ServiceStatusPollingIntervalSeconds, ClientSettingsFields.ServiceStatusPollingInterval),
            (SettingsModalSections.Logs, _editingSettings.RecentLogsPollingIntervalSeconds, ClientSettingsFields.RecentLogsPollingInterval),
            (SettingsModalSections.AppLogs, _editingSettings.AppLogsPollingIntervalSeconds, ClientSettingsFields.AppLogsPollingInterval),
            (SettingsModalSections.Leases, _editingSettings.LeasesPollingIntervalSeconds, ClientSettingsFields.LeasesPollingInterval),
            (SettingsModalSections.RecentLogsDisplay, _editingSettings.RecentLogsMaxLines, ClientSettingsFields.RecentLogsMaxLines),
            (SettingsModalSections.AppLogsDisplay, _editingSettings.AppLogsMaxLines, ClientSettingsFields.AppLogsMaxLines),
        };
        foreach (var (section, value, bounds) in checks)
        {
            if (ShouldShowSection(section) && bounds.Validate(value) is { } err)
                _validationErrors.Add(err);
        }
    }

    private async Task Save()
    {
        RunValidation();
        if (_validationErrors.Count > 0)
        {
            await InvokeAsync(StateHasChanged);
            return;
        }

        await ClientSettingsService.SaveSettingsAsync(_editingSettings);
        await (_jsModule?.InvokeVoidAsync("closeModal", _dialogRef) ?? ValueTask.CompletedTask);
        await OnClose.InvokeAsync();
    }

    private async Task Close()
    {
        await (_jsModule?.InvokeVoidAsync("closeModal", _dialogRef) ?? ValueTask.CompletedTask);
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
        if (_jsModule != null)
        {
            try { await _jsModule.DisposeAsync(); }
            catch (InvalidOperationException ex) { Logger.LogDebug(ex, "SettingsModal: DisposeAsync skipped (prerender)"); }
            catch (JSDisconnectedException ex) { Logger.LogDebug(ex, "SettingsModal: DisposeAsync skipped (circuit disconnected)"); }
            catch (JSException ex) { Logger.LogDebug(ex, "SettingsModal: DisposeAsync failed"); }
            _jsModule = null;
        }
        _dotNetRef?.Dispose();
        _dotNetRef = null;
    }
}
