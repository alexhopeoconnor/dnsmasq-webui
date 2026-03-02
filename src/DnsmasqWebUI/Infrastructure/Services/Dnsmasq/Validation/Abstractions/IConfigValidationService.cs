using DnsmasqWebUI.Infrastructure.Services.Registration.Abstractions;

namespace DnsmasqWebUI.Infrastructure.Services.Dnsmasq.Validation.Abstractions;

/// <summary>Runs the configured config validation command (e.g. dnsmasq --test --conf-file=...). Used before restart in the save flow.</summary>
public interface IConfigValidationService : IApplicationScopedService
{
    /// <summary>Runs the validation command. When not configured, returns success without running.</summary>
    Task<ConfigValidationResult> ValidateAsync(CancellationToken ct = default);
}

/// <summary>Result of running the validation command.</summary>
public record ConfigValidationResult(
    bool Success,
    bool Attempted,
    int ExitCode,
    string? StdOut,
    string? StdErr,
    string? UserMessage);
