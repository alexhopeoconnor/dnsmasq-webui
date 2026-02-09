namespace DnsmasqWebUI.Infrastructure.Services.Abstractions;

/// <summary>
/// Runs LogsCommand, diffs output, chunks by lines, and pushes via SignalR.
/// Triggered by client (RequestDnsmasqLogs) or file watcher when LogsPath changes.
/// App logs: event-driven push when provider writes; snapshot on connect; poll fallback.
/// </summary>
public interface ILogsService
{
    /// <summary>
    /// Runs LogsCommand, diffs against cache, and pushes DnsmasqLogsUpdate to all connected clients.
    /// </summary>
    Task RunAndPushDnsmasqLogsAsync(CancellationToken ct = default);

    /// <summary>
    /// Pushes recent app logs from buffer to all clients (replace). Called when client connects.
    /// </summary>
    Task PushAppLogsSnapshotAsync(CancellationToken ct = default);

    /// <summary>
    /// Pushes pending app log lines as append to all clients. Event-driven from provider; poll fallback.
    /// </summary>
    Task PushAppLogsPendingAsync(CancellationToken ct = default);
}
