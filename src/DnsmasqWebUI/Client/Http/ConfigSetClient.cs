using DnsmasqWebUI.Client.Http.Abstractions;
using DnsmasqWebUI.Models.EffectiveConfig;
using System.Net.Http.Json;

namespace DnsmasqWebUI.Client.Http;

public sealed class ConfigSetClient : IConfigSetClient
{
    private readonly HttpClient _http;

    public ConfigSetClient(HttpClient http) => _http = http;

    public async Task<DnsmasqConfigSet> GetConfigSetAsync(CancellationToken ct = default) =>
        await _http.GetFromJsonAsync<DnsmasqConfigSet>("api/config/set", ct)
            ?? throw new InvalidOperationException("Unexpected null from api/config/set.");
}
