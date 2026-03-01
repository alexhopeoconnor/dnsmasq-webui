using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Channels;
using DnsmasqWebUI.Infrastructure.Services.Common.Process.Abstractions;
using DnsmasqWebUI.Models.Contracts;
using Microsoft.Extensions.Logging;

namespace DnsmasqWebUI.Infrastructure.Services.Common.Process;

/// <summary>Handle to a started process; buffers output and exposes streaming and wait-for-exit.</summary>
internal sealed class ProcessHandle : IProcessHandle
{
    private const string TruncMsg = "\n\n(output truncated)\n";
    private static readonly TimeSpan StreamCloseWaitTimeout = TimeSpan.FromSeconds(2);

    private readonly System.Diagnostics.Process _process;
    private readonly Channel<ProcessOutputLine> _channel;
    private readonly StringBuilder _stdout = new();
    private readonly StringBuilder _stderr = new();
    private readonly int? _maxOutputChars;
    private readonly ILogger? _logger;
    private bool _channelCompleted;
    private readonly object _gate = new();
    private readonly TaskCompletionSource _stdoutClosed = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly TaskCompletionSource _stderrClosed = new(TaskCreationOptions.RunContinuationsAsynchronously);

    public ProcessHandle(System.Diagnostics.Process process, Channel<ProcessOutputLine> channel, int? maxOutputChars, ILogger? logger)
    {
        _process = process;
        _channel = channel;
        _maxOutputChars = maxOutputChars;
        _logger = logger;

        process.EnableRaisingEvents = true;
        process.Exited += (_, _) =>
        {
            lock (_gate)
            {
                if (!_channelCompleted)
                {
                    _channelCompleted = true;
                    _channel.Writer.Complete();
                }
            }
        };

        process.OutputDataReceived += (_, e) =>
        {
            if (e.Data is not null)
            {
                AppendCapped(_stdout, e.Data + "\n", _maxOutputChars);
                _channel.Writer.TryWrite(new ProcessOutputLine(ProcessOutputStream.StdOut, e.Data, DateTime.UtcNow));
            }
            else
                _stdoutClosed.TrySetResult();
        };

        process.ErrorDataReceived += (_, e) =>
        {
            if (e.Data is not null)
            {
                AppendCapped(_stderr, e.Data + "\n", _maxOutputChars);
                _channel.Writer.TryWrite(new ProcessOutputLine(ProcessOutputStream.StdErr, e.Data, DateTime.UtcNow));
            }
            else
                _stderrClosed.TrySetResult();
        };
    }

    public async IAsyncEnumerable<ProcessOutputLine> ReadOutputAsync([EnumeratorCancellation] CancellationToken ct = default)
    {
        await foreach (var line in _channel.Reader.ReadAllAsync(ct))
            yield return line;
    }

    public async Task<ProcessRunResult> WaitForExitAsync(TimeSpan? timeout, CancellationToken ct = default)
    {
        using var timeoutCts = timeout.HasValue ? CancellationTokenSource.CreateLinkedTokenSource(ct) : null;
        if (timeoutCts != null && timeout!.Value > TimeSpan.Zero)
            timeoutCts.CancelAfter(timeout.Value);

        var exitToken = timeoutCts?.Token ?? ct;

        // Drain channel so it does not grow unbounded when no one is calling ReadOutputAsync
        var drainTask = Task.Run(async () =>
        {
            try
            {
                await foreach (var _ in _channel.Reader.ReadAllAsync(ct)) { }
            }
            catch (OperationCanceledException) { }
        }, ct);

        try
        {
            await _process.WaitForExitAsync(exitToken);
        }
        catch (OperationCanceledException) when (timeoutCts?.IsCancellationRequested == true && !ct.IsCancellationRequested)
        {
            var timeoutSeconds = timeout?.TotalSeconds ?? 0;
            _logger?.LogWarning("Command timed out after {Timeout}s", timeoutSeconds);
            try { _process.Kill(); } catch { /* best effort */ }
            lock (_gate)
            {
                if (!_channelCompleted)
                {
                    _channelCompleted = true;
                    _channel.Writer.Complete();
                }
            }
            try { await drainTask; } catch (OperationCanceledException) { }
            await WaitForStreamsClosedAsync();
            var err = _stderr.ToString();
            if (!string.IsNullOrEmpty(err)) err += "\n";
            err += $"Command timed out after {timeoutSeconds} seconds.";
            return new ProcessRunResult(null, _stdout.ToString(), err, true);
        }

        try { await drainTask; } catch (OperationCanceledException) { }
        await WaitForStreamsClosedAsync();
        var exitCode = _process.HasExited ? _process.ExitCode : -1;
        _logger?.LogDebug("Command completed, exit code={ExitCode}", exitCode);

        return new ProcessRunResult(exitCode, _stdout.ToString(), _stderr.ToString(), false);
    }

    public Task KillAsync(CancellationToken ct = default)
    {
        try { _process.Kill(); } catch { /* best effort */ }
        return Task.CompletedTask;
    }

    public async ValueTask DisposeAsync()
    {
        lock (_gate)
        {
            if (!_channelCompleted)
            {
                _channelCompleted = true;
                _channel.Writer.Complete();
            }
        }

        if (!_process.HasExited)
        {
            try { _process.Kill(); } catch { /* best effort */ }
        }

        _process.Dispose();
        await Task.CompletedTask;
    }

    /// <summary>Wait for stdout/stderr async read streams to close so buffers are complete before we read them. Best practice: wait for both stream-close signals (e.Data == null) after process exit to avoid lost output (see e.g. dotnet/runtime#18789).</summary>
    private async Task WaitForStreamsClosedAsync()
    {
        try
        {
            await Task.WhenAll(_stdoutClosed.Task, _stderrClosed.Task)
                .WaitAsync(StreamCloseWaitTimeout);
        }
        catch (TimeoutException)
        {
            _logger?.LogDebug("Stream close wait timed out after {Seconds}s", StreamCloseWaitTimeout.TotalSeconds);
        }
    }

    private static void AppendCapped(StringBuilder sb, string line, int? maxChars)
    {
        if (maxChars.HasValue && sb.Length >= maxChars.Value) return;
        sb.Append(line);
        if (maxChars.HasValue && sb.Length > maxChars.Value)
        {
            var keep = Math.Max(0, maxChars.Value - TruncMsg.Length);
            sb.Length = keep;
            sb.Append(TruncMsg);
        }
    }
}
