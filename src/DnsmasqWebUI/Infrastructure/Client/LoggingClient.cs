using System.Net.Http.Json;
using DnsmasqWebUI.Infrastructure.Client.Abstractions;
using DnsmasqWebUI.Infrastructure.Helpers.Http;

namespace DnsmasqWebUI.Infrastructure.Client;

public sealed class LoggingClient : ILoggingClient
{
    private readonly HttpClient _http;

    public LoggingClient(HttpClient http) => _http = http;

    public async Task<string> GetLevelAsync(CancellationToken ct = default)
    {
        var response = await _http.GetFromJsonAsync<LogLevelResponse>("api/logging/level", ApiJsonOptions.ClientOptions, ct);
        return response?.LogLevel ?? "Information";
    }

    public async Task<string> SetLevelAsync(string logLevel, CancellationToken ct = default)
    {
        var response = await _http.PostAsJsonAsync("api/logging/level", new { logLevel }, ct);
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<LogLevelResponse>(ApiJsonOptions.ClientOptions, ct);
        return body?.LogLevel ?? "Information";
    }

    public async Task<IReadOnlyList<string>> GetFiltersAsync(CancellationToken ct = default)
    {
        var response = await _http.GetFromJsonAsync<FiltersResponse>("api/logging/filters", ApiJsonOptions.ClientOptions, ct);
        return response?.ExcludedCategoryPrefixes ?? [];
    }

    public async Task<IReadOnlyList<string>> SetFiltersAsync(IReadOnlyList<string> excludedCategoryPrefixes, CancellationToken ct = default)
    {
        var response = await _http.PostAsJsonAsync("api/logging/filters", new { excludedCategoryPrefixes }, ct);
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<FiltersResponse>(ApiJsonOptions.ClientOptions, ct);
        return body?.ExcludedCategoryPrefixes ?? [];
    }

    public async Task<IReadOnlyList<string>> RestoreFilterDefaultsAsync(CancellationToken ct = default)
    {
        var response = await _http.PostAsync("api/logging/filters/restore-defaults", null, ct);
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<FiltersResponse>(ApiJsonOptions.ClientOptions, ct);
        return body?.ExcludedCategoryPrefixes ?? [];
    }

    private record LogLevelResponse(string LogLevel);
    private record FiltersResponse(List<string> ExcludedCategoryPrefixes);
}
