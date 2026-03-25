using DnsmasqWebUI.Infrastructure.Client.Abstractions;
using DnsmasqWebUI.Infrastructure.Helpers.Http;
using DnsmasqWebUI.Models.Hosts;
using System.Net.Http.Json;

namespace DnsmasqWebUI.Infrastructure.Client;

public sealed class HostsClient : IHostsClient
{
    private readonly HttpClient _http;

    public HostsClient(HttpClient http) => _http = http;

    public async Task<IReadOnlyList<HostEntry>> GetHostsAsync(CancellationToken ct = default)
    {
        var list = await _http.GetFromJsonAsync<List<HostEntry>>("api/hosts", ApiJsonOptions.ClientOptions, ct);
        return list ?? new List<HostEntry>();
    }

    public async Task<IReadOnlyList<ReadOnlyHostsFile>> GetReadOnlyHostsAsync(CancellationToken ct = default)
    {
        var list = await _http.GetFromJsonAsync<List<ReadOnlyHostsFile>>("api/hosts/readonly", ApiJsonOptions.ClientOptions, ct);
        return list ?? new List<ReadOnlyHostsFile>();
    }

    public async Task<IReadOnlyList<HostsPageRow>> GetUnifiedRowsAsync(
        bool expandHosts,
        string? domain,
        bool noHosts,
        string? managedHostsPath,
        CancellationToken ct = default)
    {
        var queryParams = $"?expandHosts={expandHosts}&noHosts={noHosts}";
        if (!string.IsNullOrEmpty(domain))
            queryParams += $"&domain={Uri.EscapeDataString(domain)}";
        if (!string.IsNullOrEmpty(managedHostsPath))
            queryParams += $"&managedHostsPath={Uri.EscapeDataString(managedHostsPath)}";
        var list = await _http.GetFromJsonAsync<List<HostsPageRow>>($"api/hosts/unified{queryParams}", ApiJsonOptions.ClientOptions, ct);
        return list ?? new List<HostsPageRow>();
    }
}
