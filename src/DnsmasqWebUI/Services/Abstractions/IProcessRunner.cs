namespace DnsmasqWebUI.Services.Abstractions;

/// <summary>Runs a shell command with timeout and cancellation. Used for status, reload, status show, and logs commands.</summary>
public interface IProcessRunner : IApplicationScopedService
{
    /// <summary>Runs a shell command with timeout and cancellation. Returns exit code (null if timed out or failed to start), stdout, stderr, and whether the run timed out.</summary>
    Task<ProcessRunResult> RunAsync(string? command, TimeSpan timeout, CancellationToken ct = default);
}
