using System.Net.Http.Json;
using DnsmasqWebUI.Infrastructure.Client.Abstractions;
using DnsmasqWebUI.Infrastructure.Helpers.Http;
using DnsmasqWebUI.Models.Dhcp;

namespace DnsmasqWebUI.Infrastructure.Client;

public sealed class DhcpHostsClient : IDhcpHostsClient
{
    private readonly HttpClient _http;

    public DhcpHostsClient(HttpClient http) => _http = http;

    public async Task<IReadOnlyList<DhcpHostEntry>> GetDhcpHostsAsync(CancellationToken ct = default)
    {
        var list = await _http.GetFromJsonAsync<List<DhcpHostEntry>>("api/dhcp/hosts", ApiJsonOptions.ClientOptions, ct);
        return list ?? new List<DhcpHostEntry>();
    }
}
