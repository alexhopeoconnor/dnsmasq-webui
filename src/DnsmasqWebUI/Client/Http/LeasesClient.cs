using DnsmasqWebUI.Client.Http.Abstractions;
using DnsmasqWebUI.Models.Dhcp;
using System.Net.Http.Json;

namespace DnsmasqWebUI.Client.Http;

public sealed class LeasesClient : ILeasesClient
{
    private readonly HttpClient _http;

    public LeasesClient(HttpClient http) => _http = http;

    public async Task<LeasesResult> GetLeasesAsync(bool forceRefresh = false, CancellationToken ct = default)
    {
        var url = forceRefresh ? "api/leases?refresh=true" : "api/leases";
        return await _http.GetFromJsonAsync<LeasesResult>(url, ct)
            ?? throw new InvalidOperationException("Unexpected null from api/leases.");
    }
}
