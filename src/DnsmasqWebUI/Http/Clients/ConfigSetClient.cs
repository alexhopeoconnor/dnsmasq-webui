using DnsmasqWebUI.Models;
using System.Net.Http.Json;

namespace DnsmasqWebUI.Http.Clients;

public sealed class ConfigSetClient : IConfigSetClient
{
    private readonly HttpClient _http;

    public ConfigSetClient(HttpClient http) => _http = http;

    public async Task<DnsmasqConfigSet> GetConfigSetAsync(CancellationToken ct = default) =>
        await _http.GetFromJsonAsync<DnsmasqConfigSet>("api/config/set", ct)
            ?? throw new InvalidOperationException("Unexpected null from api/config/set.");
}
