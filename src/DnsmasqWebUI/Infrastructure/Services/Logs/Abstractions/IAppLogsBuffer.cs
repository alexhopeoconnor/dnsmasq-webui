namespace DnsmasqWebUI.Infrastructure.Services.Logs.Abstractions;

/// <summary>
/// Ring buffer of recent app log lines. Filled by ILoggerProvider, drained for SignalR push.
/// </summary>
public interface IAppLogsBuffer
{
    /// <summary>Returns up to maxLines most recent lines (oldest first).</summary>
    IReadOnlyList<string> GetRecent(int maxLines = 500);

    /// <summary>Enqueues a formatted log line. Called by the logging provider.</summary>
    void Enqueue(string line);

    /// <summary>Returns and clears pending lines for streaming push. Called by pusher.</summary>
    IReadOnlyList<string> DrainPending();
}
