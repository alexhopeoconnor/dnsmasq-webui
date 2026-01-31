namespace DnsmasqWebUI.Services.Abstractions;

/// <summary>Result of running a shell command via <see cref="IProcessRunner"/>.</summary>
/// <param name="ExitCode">Process exit code, or null if timed out or failed to start.</param>
/// <param name="Stdout">Standard output.</param>
/// <param name="Stderr">Standard error.</param>
/// <param name="TimedOut">True if the command was killed due to timeout.</param>
/// <param name="ExceptionMessage">If run failed with an exception, the message; otherwise null.</param>
public record ProcessRunResult(
    int? ExitCode,
    string Stdout,
    string Stderr,
    bool TimedOut,
    string? ExceptionMessage = null
);
