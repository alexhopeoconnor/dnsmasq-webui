using DnsmasqWebUI.Infrastructure.Services.Logs.Abstractions;
using Microsoft.AspNetCore.SignalR;

namespace DnsmasqWebUI.Hubs;

/// <summary>
/// SignalR hub for log streaming. Clients receive DnsmasqLogsUpdate and AppLogsUpdate.
/// Server push is done via IHubContext; this hub defines methods clients can invoke.
/// </summary>
public class LogsHub : Hub
{
    private readonly ILogsService _logsService;

    public LogsHub(ILogsService logsService)
    {
        _logsService = logsService;
    }

    /// <summary>
    /// Client requests dnsmasq logs. Server runs LogsCommand, diffs, and pushes updates.
    /// </summary>
    public async Task RequestDnsmasqLogs()
    {
        await _logsService.RunAndPushDnsmasqLogsAsync(Context.ConnectionAborted);
    }

    /// <summary>
    /// Client requests a snapshot of recent app logs (replace). Used on connect.
    /// </summary>
    public async Task RequestAppLogsSnapshot()
    {
        await _logsService.PushAppLogsSnapshotAsync(Context.ConnectionAborted);
    }

    /// <summary>
    /// Client polls for new app logs (append). Fallback for reconnect/missed pushes.
    /// </summary>
    public async Task RequestAppLogsUpdate()
    {
        await _logsService.PushAppLogsPendingAsync(Context.ConnectionAborted);
    }
}
