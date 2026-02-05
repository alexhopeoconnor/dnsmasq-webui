using DnsmasqWebUI.Infrastructure.Client.Abstractions;
using DnsmasqWebUI.Infrastructure.Helpers.Http;
using DnsmasqWebUI.Models.Dnsmasq;
using DnsmasqWebUI.Models.Dnsmasq.EffectiveConfig;
using System.Net.Http.Json;

namespace DnsmasqWebUI.Infrastructure.Client;

public sealed class StatusClient : IStatusClient
{
    private readonly HttpClient _http;

    public StatusClient(HttpClient http) => _http = http;

    public async Task<DnsmasqServiceStatus> GetStatusAsync(CancellationToken ct = default) =>
        await _http.GetFromJsonAsync<DnsmasqServiceStatus>("api/status", ApiJsonOptions.ClientOptions, ct)
            ?? throw new InvalidOperationException("Unexpected null from api/status.");
}
