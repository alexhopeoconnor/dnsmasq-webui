using DnsmasqWebUI.Extensions.Interop;
using DnsmasqWebUI.Models.Dnsmasq;
using DnsmasqWebUI.Models.Logs;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.JSInterop;

namespace DnsmasqWebUI.Components.Dnsmasq.Logs;

/// <summary>
/// Recent logs block: connects to LogsHub, polls at interval via RequestDnsmasqLogs,
/// receives DnsmasqLogsUpdate pushes, updates DOM via JS interop.
/// </summary>
public partial class LogsSection : IAsyncDisposable
{
    private const string LogsPreId = "dnsmasq-logs-pre";

    private DnsmasqServiceStatus? _status;
    private bool _refreshing;
    private bool _justUpdated;
    private bool _logsContentReceived;
    private int _intervalSeconds;
    private HubConnection? _hubConnection;
    private IJSObjectReference? _logsJs;
    private Timer? _pollTimer;
    private Timer? _justUpdatedResetTimer;
    private readonly CancellationTokenSource _cts = new();

    [Parameter] public DnsmasqServiceStatus? Status { get; set; }

    [Parameter] public int RefreshIntervalSeconds { get; set; } = 15;

    [Parameter] public int LogsMaxLines { get; set; } = 500;

    [Parameter] public bool LogsAutoScroll { get; set; } = true;

    [Parameter] public EventCallback OnOpenSettings { get; set; }

    private object LogsOptions => new { maxLines = LogsMaxLines, autoScroll = LogsAutoScroll };

    private string _initialPlaceholder => _hubConnection?.State == HubConnectionState.Connected ? "Waiting for logs…" : "Connecting…";

    protected override void OnParametersSet()
    {
        _status = Status;
        if (RefreshIntervalSeconds == _intervalSeconds) return;
        _intervalSeconds = RefreshIntervalSeconds;
        RestartPollTimer();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender) return;
        _intervalSeconds = RefreshIntervalSeconds;
        _status = Status;

        var hubUri = new Uri(new Uri(Navigation.BaseUri), "hubs/logs").ToString();

        _hubConnection = new HubConnectionBuilder()
            .WithUrl(hubUri)
            .WithAutomaticReconnect()
            .Build();

        _hubConnection.On<LogsUpdatePayload>("DnsmasqLogsUpdate", OnDnsmasqLogsUpdate);

        try
        {
            _logsJs = await JSRuntime.InvokeAsync<IJSObjectReference>("import", "./js/logs.js");
        }
        catch (InvalidOperationException ex) { Logger.LogDebug(ex, "LogsSection: JS import skipped (prerender)"); return; }
        catch (JSDisconnectedException ex) { Logger.LogDebug(ex, "LogsSection: JS import skipped (circuit disconnected)"); return; }
        catch (JSException ex) { Logger.LogDebug(ex, "LogsSection: JS import failed"); return; }

        await _hubConnection.StartAsync(_cts.Token);
        RestartPollTimer();

        // Initial request
        await RequestRefreshAsync();
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
        await RequestRefreshAsync();
    }

    private async Task RequestRefreshAsync()
    {
        if (_hubConnection?.State != HubConnectionState.Connected)
            return;
        _refreshing = true;
        StateHasChanged();
        try
        {
            await _hubConnection.InvokeAsync("RequestDnsmasqLogs", _cts.Token);
        }
        catch (OperationCanceledException) { }
        catch (Exception)
        {
            // Don't overwrite on background refresh
        }
        finally
        {
            _refreshing = false;
            StateHasChanged();
        }
    }

    private async void OnDnsmasqLogsUpdate(LogsUpdatePayload payload)
    {
        try
        {
            await InvokeAsync(async () =>
            {
                if (_logsJs == null) return;
                if (payload.Mode == "replace")
                    await _logsJs.InvokeVoidAsyncSafe("replaceLogs", LogsPreId, payload.Content, LogsOptions);
                else
                    await _logsJs.InvokeVoidAsyncSafe("appendLogs", LogsPreId, payload.Content, LogsOptions);
                _logsContentReceived = true;
                SetJustUpdated();
                StateHasChanged();
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
        await _logsJs.DisposeAsyncSafe();
        _logsJs = null;
    }
}
