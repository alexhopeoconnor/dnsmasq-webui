using DnsmasqWebUI.Models;
using System.Net.Http.Json;

namespace DnsmasqWebUI.Http.Clients;

public sealed class StatusClient : IStatusClient
{
    private readonly HttpClient _http;

    public StatusClient(HttpClient http) => _http = http;

    public async Task<DnsmasqServiceStatus> GetStatusAsync(CancellationToken ct = default) =>
        await _http.GetFromJsonAsync<DnsmasqServiceStatus>("api/status", ct)
            ?? throw new InvalidOperationException("Unexpected null from api/status.");
}
