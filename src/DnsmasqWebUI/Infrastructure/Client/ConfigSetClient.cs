using DnsmasqWebUI.Infrastructure.Client.Abstractions;
using DnsmasqWebUI.Infrastructure.Helpers.Http;
using DnsmasqWebUI.Models.Dnsmasq;
using DnsmasqWebUI.Models.Dnsmasq.EffectiveConfig;
using System.Net.Http.Json;

namespace DnsmasqWebUI.Infrastructure.Client;

public sealed class ConfigSetClient : IConfigSetClient
{
    private readonly HttpClient _http;

    public ConfigSetClient(HttpClient http) => _http = http;

    public async Task<DnsmasqConfigSet> GetConfigSetAsync(CancellationToken ct = default) =>
        await _http.GetFromJsonAsync<DnsmasqConfigSet>("api/config/set", ApiJsonOptions.ClientOptions, ct)
            ?? throw new InvalidOperationException("Unexpected null from api/config/set.");
}
