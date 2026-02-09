using System.Text;
using DnsmasqWebUI.Infrastructure.Logging;
using DnsmasqWebUI.Infrastructure.Services.Abstractions;
using DnsmasqWebUI.Models.Config;
using DnsmasqWebUI.Models.Logs;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DnsmasqWebUI.Infrastructure.Services;

/// <summary>
/// Runs LogsCommand, diffs against cache, chunks by lines, and pushes via SignalR.
/// </summary>
public sealed class LogsService : ILogsService
{
    /// <summary>Max chars from command output before truncation.</summary>
    private const int MaxCommandOutputChars = 512 * 1024;

    /// <summary>Max bytes per SignalR message (under 32KB default).</summary>
    private const int MaxMessageContentBytes = 28 * 1024;

    private readonly IHubContext<Hubs.LogsHub> _hubContext;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IOptions<DnsmasqOptions> _options;
    private readonly IAppLogsBuffer _appLogsBuffer;
    private readonly ILogger<LogsService> _logger;
    private readonly object _dnsmasqLock = new();
    private string _lastDnsmasqOutput = "";

    public LogsService(
        IHubContext<Hubs.LogsHub> hubContext,
        IServiceScopeFactory scopeFactory,
        IOptions<DnsmasqOptions> options,
        IAppLogsBuffer appLogsBuffer,
        ILogger<LogsService> logger)
    {
        _hubContext = hubContext;
        _scopeFactory = scopeFactory;
        _options = options;
        _appLogsBuffer = appLogsBuffer;
        _logger = logger;
    }

    public async Task RunAndPushDnsmasqLogsAsync(CancellationToken ct = default)
    {
        var cmd = _options.Value.LogsCommand?.Trim();
        if (string.IsNullOrEmpty(cmd))
            return;

        try
        {
            using var scope = _scopeFactory.CreateScope();
            var processRunner = scope.ServiceProvider.GetRequiredService<IProcessRunner>();
            var result = await processRunner.RunAsync(cmd, TimeSpan.FromSeconds(10), MaxCommandOutputChars, ct);
            var raw = result.Stdout + (result.TimedOut ? "\n(Command timed out.)" : "");

            string mode;
            string content;
            lock (_dnsmasqLock)
            {
                var (m, c) = ComputeDelta(_lastDnsmasqOutput, raw);
                _lastDnsmasqOutput = raw;
                mode = m;
                content = c;
            }

            if (string.IsNullOrEmpty(content) && mode == "append")
                return;

            await PushChunkedAsync("DnsmasqLogsUpdate", mode, content, ct);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(LogEvents.DnsmasqLogsPushFailed, ex, "Failed to run or push dnsmasq logs");
            await PushChunkedAsync("DnsmasqLogsUpdate", "replace", $"(Error: {ex.Message})\n", ct);
        }
    }

    public async Task PushAppLogsSnapshotAsync(CancellationToken ct = default)
    {
        var lines = _appLogsBuffer.GetRecent(maxLines: 1000);
        if (lines.Count == 0)
            return;

        var content = string.Join("\n", lines);
        if (!string.IsNullOrEmpty(content) && !content.EndsWith('\n'))
            content += "\n";

        await PushChunkedAsync("AppLogsUpdate", "replace", content, ct);
    }

    public async Task PushAppLogsPendingAsync(CancellationToken ct = default)
    {
        var lines = _appLogsBuffer.DrainPending();
        if (lines.Count == 0)
            return;

        var content = string.Join("\n", lines);
        if (!string.IsNullOrEmpty(content) && !content.EndsWith('\n'))
            content += "\n";

        await PushChunkedAsync("AppLogsUpdate", "append", content, ct);
    }

    private static (string mode, string content) ComputeDelta(string oldOutput, string newOutput)
    {
        if (string.IsNullOrEmpty(oldOutput))
            return ("replace", newOutput);

        var oldLines = oldOutput.Split('\n');
        var newLines = newOutput.Split('\n');

        // Find longest suffix of old that matches prefix of new (tail-style overlap)
        var maxOverlap = Math.Min(oldLines.Length, newLines.Length);
        var overlap = 0;
        for (var k = 1; k <= maxOverlap; k++)
        {
            var match = true;
            for (var i = 0; i < k && match; i++)
            {
                if (oldLines[oldLines.Length - k + i] != newLines[i])
                    match = false;
            }
            if (match)
                overlap = k;
        }

        if (overlap == 0 || overlap >= newLines.Length)
            return ("replace", newOutput);

        var newOnly = newLines[overlap..];
        var content = string.Join("\n", newOnly);
        if (!string.IsNullOrEmpty(content) && !content.EndsWith('\n'))
            content += "\n";

        return ("append", content);
    }

    private async Task PushChunkedAsync(string method, string mode, string content, CancellationToken ct)
    {
        var chunks = ChunkByLines(content, MaxMessageContentBytes);
        for (var i = 0; i < chunks.Count; i++)
        {
            var payload = new LogsUpdatePayload(i == 0 ? mode : "append", chunks[i]);
            await _hubContext.Clients.All.SendAsync(method, payload, ct);
        }
    }

    private static List<string> ChunkByLines(string content, int maxBytesPerChunk)
    {
        if (string.IsNullOrEmpty(content))
            return [];

        var lines = content.Split('\n');
        var chunks = new List<string>();
        var current = new StringBuilder();
        var enc = Encoding.UTF8;

        for (var i = 0; i < lines.Length; i++)
        {
            var line = lines[i];
            var lineWithNewline = i < lines.Length - 1 ? line + "\n" : (line.Length > 0 ? line + "\n" : line);
            var lineBytes = enc.GetByteCount(lineWithNewline);

            if (lineBytes > maxBytesPerChunk)
            {
                if (current.Length > 0)
                {
                    chunks.Add(current.ToString());
                    current.Clear();
                }
                var trunc = line.Length;
                while (trunc > 0 && enc.GetByteCount(line[..trunc] + "... (line truncated)\n") > maxBytesPerChunk)
                    trunc--;
                chunks.Add((trunc > 0 ? line[..trunc] : line) + "... (line truncated)\n");
                continue;
            }

            if (current.Length > 0 && enc.GetByteCount(current.ToString()) + lineBytes > maxBytesPerChunk)
            {
                chunks.Add(current.ToString());
                current.Clear();
            }

            if (i < lines.Length - 1)
                current.Append(line).Append('\n');
            else if (line.Length > 0)
                current.Append(line).Append('\n');
        }

        if (current.Length > 0)
            chunks.Add(current.ToString());

        return chunks;
    }
}
