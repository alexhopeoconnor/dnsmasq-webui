using DnsmasqWebUI.Infrastructure.Client.Abstractions;
using DnsmasqWebUI.Models.Logs;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.JSInterop;

namespace DnsmasqWebUI.Components.Dnsmasq;

/// <summary>
/// App logs block: connects to LogsHub, receives AppLogsUpdate pushes, polls at interval as fallback.
/// Snapshot on connect; RequestAppLogsUpdate on poll. Log level is configurable at runtime (persisted to appsettings.Overrides.json).
/// </summary>
public partial class AppLogsSection : IAsyncDisposable
{
    private const string AppLogsPreId = "app-logs-pre";

    [Inject] private ILoggingClient LoggingClient { get; set; } = null!;

    private bool _justUpdated;
    private bool _logsContentReceived;
    private int _intervalSeconds;
    private string _logLevel = "Information";
    private bool _logLevelDisabled;
    private bool _filtersModalVisible;
    private HubConnection? _hubConnection;
    private IJSObjectReference? _logsJs;
    private Timer? _pollTimer;
    private Timer? _justUpdatedResetTimer;
    private readonly CancellationTokenSource _cts = new();

    [Parameter] public int RefreshIntervalSeconds { get; set; } = 15;

    [Parameter] public int LogsMaxLines { get; set; } = 500;

    [Parameter] public bool LogsAutoScroll { get; set; } = true;

    [Parameter] public EventCallback OnOpenSettings { get; set; }

    private string _initialPlaceholder => _hubConnection?.State == HubConnectionState.Connected ? "Waiting for logs…" : "Connecting…";

    private object LogsOptions => new { maxLines = LogsMaxLines, autoScroll = LogsAutoScroll };

    protected override void OnParametersSet()
    {
        if (RefreshIntervalSeconds == _intervalSeconds) return;
        _intervalSeconds = RefreshIntervalSeconds;
        RestartPollTimer();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender) return;
        _intervalSeconds = RefreshIntervalSeconds;
        var hubUri = new Uri(new Uri(Navigation.BaseUri), "hubs/logs").ToString();

        _hubConnection = new HubConnectionBuilder()
            .WithUrl(hubUri)
            .WithAutomaticReconnect()
            .Build();

        _hubConnection.On<LogsUpdatePayload>("AppLogsUpdate", OnAppLogsUpdate);

        try
        {
            _logsJs = await JSRuntime.InvokeAsync<IJSObjectReference>("import", "./js/logs.js");
        }
        catch (InvalidOperationException ex) { Logger.LogDebug(ex, "AppLogsSection: JS import skipped (prerender)"); return; }
        catch (JSDisconnectedException ex) { Logger.LogDebug(ex, "AppLogsSection: JS import skipped (circuit disconnected)"); return; }
        catch (JSException ex) { Logger.LogDebug(ex, "AppLogsSection: JS import failed"); return; }

        await _hubConnection.StartAsync(_cts.Token);
        RestartPollTimer();

        try
        {
            _logLevel = await LoggingClient.GetLevelAsync(_cts.Token);
        }
        catch (OperationCanceledException) { }
        catch (Exception) { /* leave default */ }

        // Request initial snapshot (replace)
        try
        {
            await _hubConnection.InvokeAsync("RequestAppLogsSnapshot", _cts.Token);
        }
        catch (OperationCanceledException) { }
        catch (Exception) { /* ignore */ }
    }

    private void OpenFiltersModal() => _filtersModalVisible = true;
    private void CloseFiltersModal() => _filtersModalVisible = false;

    private async Task OnLogLevelChanged()
    {
        if (string.IsNullOrEmpty(_logLevel)) return;
        _logLevelDisabled = true;
        try
        {
            var updated = await LoggingClient.SetLevelAsync(_logLevel, _cts.Token);
            _logLevel = updated;
        }
        catch (OperationCanceledException) { }
        catch (Exception) { /* leave binding as-is */ }
        finally
        {
            _logLevelDisabled = false;
            StateHasChanged();
        }
    }

    private void RestartPollTimer()
    {
        _pollTimer?.Dispose();
        _pollTimer = new Timer(
            _ => _ = InvokeAsync(PollTick),
            null,
            TimeSpan.FromSeconds(_intervalSeconds),
            TimeSpan.FromSeconds(_intervalSeconds));
    }

    private async Task PollTick()
    {
        if (_cts.Token.IsCancellationRequested || _hubConnection?.State != HubConnectionState.Connected)
            return;
        try
        {
            await _hubConnection.InvokeAsync("RequestAppLogsUpdate", _cts.Token);
        }
        catch (OperationCanceledException) { }
        catch (Exception) { /* ignore */ }
    }

    private async void OnAppLogsUpdate(LogsUpdatePayload payload)
    {
        try
        {
            await InvokeAsync(async () =>
            {
                if (_logsJs == null) return;
                try
                {
                    if (payload.Mode == "replace")
                        await _logsJs.InvokeVoidAsync("replaceLogs", AppLogsPreId, payload.Content, LogsOptions);
                    else
                        await _logsJs.InvokeVoidAsync("appendLogs", AppLogsPreId, payload.Content, LogsOptions);
                    _logsContentReceived = true;
                    SetJustUpdated();
                    StateHasChanged();
                }
                catch (JSDisconnectedException) { /* Circuit disconnected; ignore */ }
                catch (InvalidOperationException) { /* Prerender or circuit disposed; ignore */ }
                catch (JSException) { /* JS error; ignore */ }
            });
        }
        catch (ObjectDisposedException) { /* Component disposed; ignore */ }
        catch (InvalidOperationException) { /* Circuit disconnected; ignore */ }
    }

    private void SetJustUpdated()
    {
        _justUpdated = true;
        _justUpdatedResetTimer?.Dispose();
        _justUpdatedResetTimer = new Timer(_ =>
        {
            _justUpdatedResetTimer?.Dispose();
            _justUpdatedResetTimer = null;
            _ = InvokeAsync(() =>
            {
                _justUpdated = false;
                StateHasChanged();
            });
        }, null, TimeSpan.FromSeconds(2), Timeout.InfiniteTimeSpan);
    }

    public async ValueTask DisposeAsync()
    {
        _pollTimer?.Dispose();
        _justUpdatedResetTimer?.Dispose();
        _cts.Cancel();
        _cts.Dispose();
        if (_hubConnection != null)
        {
            await _hubConnection.DisposeAsync();
            _hubConnection = null;
        }
        if (_logsJs != null)
        {
            try { await _logsJs.DisposeAsync(); }
            catch (InvalidOperationException ex) { Logger.LogDebug(ex, "AppLogsSection: DisposeAsync skipped (prerender)"); }
            catch (JSDisconnectedException ex) { Logger.LogDebug(ex, "AppLogsSection: DisposeAsync skipped (circuit disconnected)"); }
            catch (JSException ex) { Logger.LogDebug(ex, "AppLogsSection: DisposeAsync failed"); }
            _logsJs = null;
        }
    }
}
