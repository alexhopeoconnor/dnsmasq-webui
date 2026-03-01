using DnsmasqWebUI.Infrastructure.Services.Registration.Abstractions;
using DnsmasqWebUI.Models.Contracts;

namespace DnsmasqWebUI.Infrastructure.Services.Common.Process.Abstractions;

/// <summary>Runs a shell command with timeout and cancellation. Used for status, reload, status show, and logs commands. Supports both run-to-completion and start-then-stream.</summary>
public interface IProcessRunner : IApplicationScopedService
{
    /// <summary>Runs a shell command with timeout and cancellation. Returns exit code (null if timed out or failed to start), stdout, stderr, and whether the run timed out.</summary>
    Task<ProcessRunResult> RunAsync(string? command, TimeSpan timeout, CancellationToken ct = default);

    /// <summary>Same as RunAsync but caps stdout/stderr length. When exceeded, truncates and appends a notice.</summary>
    Task<ProcessRunResult> RunAsync(string? command, TimeSpan timeout, int? maxOutputChars, CancellationToken ct = default);

    /// <summary>Starts a shell command and returns a handle for streaming output and/or waiting for exit. Command must be non-null and non-empty.</summary>
    Task<IProcessHandle> StartAsync(string command, int? maxOutputChars = null, CancellationToken ct = default);
}
