using DnsmasqWebUI.Models;
using System.Net.Http.Json;

namespace DnsmasqWebUI.Http.Clients;

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
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<SaveWithReloadResult>(ct)
            ?? throw new InvalidOperationException("Unexpected null from api/dhcp/hosts.");
    }
}
