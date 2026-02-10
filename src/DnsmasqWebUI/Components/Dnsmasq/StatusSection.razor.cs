using DnsmasqWebUI.Infrastructure.Client.Abstractions;
using DnsmasqWebUI.Models.Dnsmasq;
using Microsoft.AspNetCore.Components;

namespace DnsmasqWebUI.Components.Dnsmasq;

/// <summary>
/// Service status block: polls StatusShowCommand output at its own interval and re-renders only itself.
/// </summary>
public partial class StatusSection : IDisposable
{
    private DnsmasqServiceStatus? _status;
    private bool _refreshing;
    private int _intervalSeconds;
    private readonly CancellationTokenSource _cts = new();
    private Timer? _timer;

    [Parameter] public int RefreshIntervalSeconds { get; set; } = 15;

    [Parameter] public bool IsReloading { get; set; }

    [Parameter] public EventCallback OnOpenSettings { get; set; }

    [Inject] private IStatusClient StatusClient { get; set; } = null!;

    protected override async Task OnInitializedAsync()
    {
        _intervalSeconds = RefreshIntervalSeconds;
        await RefreshAsync();
        _timer = new Timer(
            _ => _ = InvokeAsync(OnRefreshTick),
            null,
            TimeSpan.FromSeconds(_intervalSeconds),
            TimeSpan.FromSeconds(_intervalSeconds));
    }

    protected override void OnParametersSet()
    {
        if (RefreshIntervalSeconds == _intervalSeconds) return;
        _intervalSeconds = RefreshIntervalSeconds;
        _timer?.Dispose();
        _timer = new Timer(
            _ => _ = InvokeAsync(OnRefreshTick),
            null,
            TimeSpan.FromSeconds(_intervalSeconds),
            TimeSpan.FromSeconds(_intervalSeconds));
    }

    private async Task OnRefreshTick()
    {
        if (_cts.Token.IsCancellationRequested) return;
        await RefreshAsync();
        StateHasChanged();
    }

    private static readonly TimeSpan MinUpdatingDuration = TimeSpan.FromSeconds(1);

    private async Task RefreshAsync()
    {
        var started = DateTime.UtcNow;
        _refreshing = true;
        StateHasChanged();
        try
        {
            var token = _cts.Token;
            _status = await StatusClient.GetStatusAsync(token);
        }
        catch (OperationCanceledException) { }
        catch
        {
            // Don't overwrite on background refresh
        }
        finally
        {
            var elapsed = DateTime.UtcNow - started;
            var remaining = MinUpdatingDuration - elapsed;
            if (remaining > TimeSpan.Zero)
            {
                try { await Task.Delay(remaining, _cts.Token).ConfigureAwait(false); }
                catch (OperationCanceledException) { }
            }
            _refreshing = false;
        }
    }

    public void Dispose()
    {
        _timer?.Dispose();
        _timer = null;
        _cts.Cancel();
        _cts.Dispose();
    }
}
