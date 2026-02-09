namespace DnsmasqWebUI.Infrastructure.Client.Abstractions;

/// <summary>Client for runtime logging API (log level and category filters).</summary>
public interface ILoggingClient
{
    Task<string> GetLevelAsync(CancellationToken ct = default);
    Task<string> SetLevelAsync(string logLevel, CancellationToken ct = default);
    Task<IReadOnlyList<string>> GetFiltersAsync(CancellationToken ct = default);
    Task<IReadOnlyList<string>> SetFiltersAsync(IReadOnlyList<string> excludedCategoryPrefixes, CancellationToken ct = default);
    Task<IReadOnlyList<string>> RestoreFilterDefaultsAsync(CancellationToken ct = default);
}
