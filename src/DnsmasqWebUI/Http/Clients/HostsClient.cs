using DnsmasqWebUI.Models;
using System.Net.Http.Json;

namespace DnsmasqWebUI.Http.Clients;

public sealed class HostsClient : IHostsClient
{
    private readonly HttpClient _http;

    public HostsClient(HttpClient http) => _http = http;

    public async Task<IReadOnlyList<HostEntry>> GetHostsAsync(CancellationToken ct = default)
    {
        var list = await _http.GetFromJsonAsync<List<HostEntry>>("api/hosts", ct);
        return list ?? new List<HostEntry>();
    }

    public async Task<SaveWithReloadResult> SaveHostsAsync(IReadOnlyList<HostEntry> entries, CancellationToken ct = default)
    {
        var response = await _http.PutAsJsonAsync("api/hosts", entries, ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<SaveWithReloadResult>(ct)
            ?? throw new InvalidOperationException("Unexpected null from api/hosts.");
    }
}
