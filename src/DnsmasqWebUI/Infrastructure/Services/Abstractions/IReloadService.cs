namespace DnsmasqWebUI.Infrastructure.Services.Abstractions;

public interface IReloadService : IApplicationScopedService
{
    Task<ReloadResult> ReloadAsync(CancellationToken ct = default);
}

public record ReloadResult(bool Success, int ExitCode, string? StdOut, string? StdErr);
