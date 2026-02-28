using DnsmasqWebUI.Models.Client;
using DnsmasqWebUI.Extensions.Interop;
using DnsmasqWebUI.Infrastructure.Services.UI.Settings.Abstractions;
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
    private List<string> _validationErrors = new();
    private string _searchTerm = string.Empty;
    private HashSet<string> _expandedGroupIds = new(StringComparer.OrdinalIgnoreCase);
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
            _expandedGroupIds.Clear();
            var visible = SettingsModalSections.Groups.Where(g => SettingsModalSections.GroupMatchesContextOrSearch(g, SettingsContext, _searchTerm)).ToList();
            if (visible.Count > 0)
                _expandedGroupIds.Add(visible[0].Id);
            _validationErrors.Clear();
            await ClientSettingsService.LoadSettingsAsync();
        }
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            try
            {
                _jsModule = await JSRuntime.InvokeAsync<IJSObjectReference>("import", "./js/dialog.js");
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
                await _jsModule.InvokeVoidAsyncSafe("initDialog", _dialogRef, _dotNetRef);
                _dialogInitialized = true;
            }
            await _jsModule.InvokeVoidAsyncSafe("showModal", _dialogRef);
        }
    }

    private void FilterSettings() => StateHasChanged();

    private IEnumerable<SettingsModalSections.CollapsibleGroup> GetVisibleGroups() =>
        SettingsModalSections.Groups.Where(g => SettingsModalSections.GroupMatchesContextOrSearch(g, SettingsContext, _searchTerm));

    private bool IsGroupExpanded(SettingsModalSections.CollapsibleGroup group)
    {
        var hasActiveFilter = SettingsContext == SettingsModalContext.All && !string.IsNullOrWhiteSpace(_searchTerm);
        if (hasActiveFilter)
            return true;
        return _expandedGroupIds.Contains(group.Id);
    }

    private void ToggleGroup(string groupId)
    {
        if (_expandedGroupIds.Contains(groupId))
            _expandedGroupIds.Remove(groupId);
        else
            _expandedGroupIds.Add(groupId);
        StateHasChanged();
    }

    private void ExpandAllGroups()
    {
        foreach (var g in GetVisibleGroups())
            _expandedGroupIds.Add(g.Id);
        StateHasChanged();
    }

    private void CollapseAllGroups()
    {
        _expandedGroupIds.Clear();
        StateHasChanged();
    }

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
        foreach (var (section, field) in ClientSettingsFields.ValidationChecks)
        {
            if (ShouldShowSection(section) && field.Validate() is { } err)
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

        await ClientSettingsService.SaveSettingsAsync(ClientSettingsFields.ToDto());
        if (_jsModule != null)
            await _jsModule.InvokeVoidAsyncSafe("closeModal", _dialogRef);
        await OnClose.InvokeAsync();
    }

    private async Task Close()
    {
        if (_jsModule != null)
            await _jsModule.InvokeVoidAsyncSafe("closeModal", _dialogRef);
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
        await _jsModule.DisposeAsyncSafe();
        _jsModule = null;
        _dotNetRef?.Dispose();
        _dotNetRef = null;
    }
}
