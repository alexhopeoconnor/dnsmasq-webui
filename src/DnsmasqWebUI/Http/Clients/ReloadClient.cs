using DnsmasqWebUI.Services.Abstractions;
using System.Net.Http.Json;

namespace DnsmasqWebUI.Http.Clients;

public sealed class ReloadClient : IReloadClient
{
    private readonly HttpClient _http;

    public ReloadClient(HttpClient http) => _http = http;

    public async Task<ReloadResult> ReloadAsync(CancellationToken ct = default)
    {
        var response = await _http.PostAsync("api/reload", null, ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<ReloadResult>(ct)
            ?? throw new InvalidOperationException("Unexpected null from api/reload.");
    }
}
