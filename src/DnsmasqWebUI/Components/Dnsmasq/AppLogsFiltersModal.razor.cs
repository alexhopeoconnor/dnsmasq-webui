using DnsmasqWebUI.Extensions;
using DnsmasqWebUI.Infrastructure.Client.Abstractions;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;

namespace DnsmasqWebUI.Components.Dnsmasq;

public partial class AppLogsFiltersModal : IAsyncDisposable
{
    private ElementReference _dialogRef;
    private IJSObjectReference? _jsModule;
    private DotNetObjectReference<AppLogsFiltersModal>? _dotNetRef;
    private List<string> _prefixes = [];
    private List<string> _originalPrefixes = [];
    private string _newPrefix = string.Empty;
    private bool _saving;
    private bool _moduleLoaded;
    private bool _dialogInitialized;
    private bool _wasVisible;
    private readonly CancellationTokenSource _cts = new();

    [Parameter] public bool IsVisible { get; set; }

    [Parameter] public EventCallback OnClose { get; set; }

    [JSInvokable]
    public async Task OnDialogClosed(string returnValue)
    {
        await OnClose.InvokeAsync();
    }

    protected override async Task OnParametersSetAsync()
    {
        var justOpened = IsVisible && !_wasVisible;
        _wasVisible = IsVisible;
        if (justOpened)
        {
            _dialogInitialized = false;
            try
            {
                var filters = await LoggingClient.GetFiltersAsync(_cts.Token);
                _prefixes = filters.ToList();
                _originalPrefixes = filters.ToList();
            }
            catch (OperationCanceledException) { }
            catch { /* leave current */ }
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
            catch (InvalidOperationException) { }
            catch (JSDisconnectedException) { }
            catch (JSException) { }
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

    private async Task Close()
    {
        if (_jsModule != null)
            await _jsModule.InvokeVoidAsyncSafe("closeModal", _dialogRef);
        await OnClose.InvokeAsync();
    }

    private void AddPrefix()
    {
        var p = _newPrefix?.Trim();
        if (string.IsNullOrEmpty(p) || _prefixes.Contains(p, StringComparer.Ordinal)) return;
        _prefixes.Add(p);
        _newPrefix = string.Empty;
    }

    private void RemovePrefix(string prefix)
    {
        _prefixes.Remove(prefix);
    }

    private void OnNewPrefixKeydown(KeyboardEventArgs e)
    {
        if (e.Key == "Enter")
        {
            AddPrefix();
        }
    }

    private async Task Save()
    {
        _saving = true;
        StateHasChanged();
        try
        {
            await LoggingClient.SetFiltersAsync(_prefixes, _cts.Token);
            _originalPrefixes = [.. _prefixes];
            await Close();
        }
        catch (OperationCanceledException) { }
        catch { /* leave modal open */ }
        finally
        {
            _saving = false;
            StateHasChanged();
        }
    }

    private async Task RestoreDefaults()
    {
        _saving = true;
        StateHasChanged();
        try
        {
            var defaults = await LoggingClient.RestoreFilterDefaultsAsync(_cts.Token);
            _prefixes = defaults.ToList();
            _originalPrefixes = [.. _prefixes];
            await Close();
        }
        catch (OperationCanceledException) { }
        catch { /* leave modal open */ }
        finally
        {
            _saving = false;
            StateHasChanged();
        }
    }

    public async ValueTask DisposeAsync()
    {
        _cts.Cancel();
        _cts.Dispose();
        _dotNetRef?.Dispose();
        await _jsModule.DisposeAsyncSafe();
    }
}
