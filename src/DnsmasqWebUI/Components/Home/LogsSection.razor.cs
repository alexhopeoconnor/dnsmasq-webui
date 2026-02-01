using DnsmasqWebUI.Client.Http.Abstractions;
using DnsmasqWebUI.Models;
using Microsoft.AspNetCore.Components;

namespace DnsmasqWebUI.Components.Home;

/// <summary>
/// Recent logs block: polls LogsCommand output at its own interval and re-renders only itself.
/// </summary>
public partial class LogsSection : IDisposable
{
    private DnsmasqServiceStatus? _status;
    private bool _refreshing;
    private int _intervalSeconds;
    private readonly CancellationTokenSource _cts = new();
    private Timer? _timer;

    [Parameter] public int RefreshIntervalSeconds { get; set; } = 15;

    [Parameter] public EventCallback OnOpenSettings { get; set; }

    [Inject] private IStatusClient StatusClient { get; set; } = null!;

    protected override async Task OnInitializedAsync()
    {
        _intervalSeconds = Math.Clamp(RefreshIntervalSeconds, 5, 300);
        await RefreshAsync();
        _timer = new Timer(
            _ => _ = InvokeAsync(OnRefreshTick),
            null,
            TimeSpan.FromSeconds(_intervalSeconds),
            TimeSpan.FromSeconds(_intervalSeconds));
    }

    protected override void OnParametersSet()
    {
        var next = Math.Clamp(RefreshIntervalSeconds, 5, 300);
        if (next == _intervalSeconds) return;
        _intervalSeconds = next;
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

    private async Task RefreshAsync()
    {
        _refreshing = true;
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
