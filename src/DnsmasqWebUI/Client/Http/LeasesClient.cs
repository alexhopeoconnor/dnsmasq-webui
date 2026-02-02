using DnsmasqWebUI.Client.Http.Abstractions;
using DnsmasqWebUI.Models.Dhcp;
using System.Net.Http.Json;

namespace DnsmasqWebUI.Client.Http;

public sealed class LeasesClient : ILeasesClient
{
    private readonly HttpClient _http;

    public LeasesClient(HttpClient http) => _http = http;

    public async Task<LeasesResult> GetLeasesAsync(CancellationToken ct = default) =>
        await _http.GetFromJsonAsync<LeasesResult>("api/leases", ct)
            ?? throw new InvalidOperationException("Unexpected null from api/leases.");
}
