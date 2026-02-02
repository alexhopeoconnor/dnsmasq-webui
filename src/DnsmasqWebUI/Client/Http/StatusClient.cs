using System.Text.Json;
using DnsmasqWebUI.Client.Http.Abstractions;
using DnsmasqWebUI.Models.Status;
using System.Net.Http.Json;

namespace DnsmasqWebUI.Client.Http;

public sealed class StatusClient : IStatusClient
{
    private readonly HttpClient _http;

    /// <summary>Matches API serialization (camelCase) so nested EffectiveConfigSources and tuple (value, source) deserialize correctly.</summary>
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    public StatusClient(HttpClient http) => _http = http;

    public async Task<DnsmasqServiceStatus> GetStatusAsync(CancellationToken ct = default) =>
        await _http.GetFromJsonAsync<DnsmasqServiceStatus>("api/status", JsonOptions, ct)
            ?? throw new InvalidOperationException("Unexpected null from api/status.");
}
