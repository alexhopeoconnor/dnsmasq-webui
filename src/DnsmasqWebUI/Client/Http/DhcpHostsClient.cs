using System.Net.Http.Json;
using System.Text.Json.Serialization;
using DnsmasqWebUI.Client.Http.Abstractions;
using DnsmasqWebUI.Models;

namespace DnsmasqWebUI.Client.Http;

public sealed class DhcpHostsClient : IDhcpHostsClient
{
    private readonly HttpClient _http;

    public DhcpHostsClient(HttpClient http) => _http = http;

    public async Task<IReadOnlyList<DhcpHostEntry>> GetDhcpHostsAsync(CancellationToken ct = default)
    {
        var list = await _http.GetFromJsonAsync<List<DhcpHostEntry>>("api/dhcp/hosts", ct);
        return list ?? new List<DhcpHostEntry>();
    }

    public async Task<SaveWithReloadResult> SaveDhcpHostsAsync(IReadOnlyList<DhcpHostEntry> entries, CancellationToken ct = default)
    {
        var response = await _http.PutAsJsonAsync("api/dhcp/hosts", entries, ct);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            var msg = response.ReasonPhrase ?? response.StatusCode.ToString();
            if (!string.IsNullOrEmpty(body) && body.TrimStart().StartsWith("{"))
            {
                try
                {
                    var err = System.Text.Json.JsonSerializer.Deserialize<JsonError>(body);
                    if (!string.IsNullOrEmpty(err?.Error))
                        msg = err.Error;
                }
                catch { /* use msg as-is */ }
            }
            throw new HttpRequestException(msg);
        }
        return await response.Content.ReadFromJsonAsync<SaveWithReloadResult>(ct)
            ?? throw new InvalidOperationException("Unexpected null from api/dhcp/hosts.");
    }

    private sealed class JsonError { [JsonPropertyName("error")] public string? Error { get; set; } }
}
