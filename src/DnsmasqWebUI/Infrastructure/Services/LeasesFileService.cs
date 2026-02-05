using DnsmasqWebUI.Models.Dhcp;
using DnsmasqWebUI.Infrastructure.Services.Abstractions;

namespace DnsmasqWebUI.Infrastructure.Services;

public class LeasesFileService : ILeasesFileService
{
    private readonly ILeasesCache _cache;

    public LeasesFileService(ILeasesCache cache)
    {
        _cache = cache;
    }

    public async Task<IReadOnlyList<LeaseEntry>> ReadAsync(CancellationToken ct = default)
    {
        var (_, entries) = await _cache.GetOrRefreshAsync(ct);
        return entries ?? Array.Empty<LeaseEntry>();
    }

    public async Task<(bool Available, IReadOnlyList<LeaseEntry>? Entries)> TryReadAsync(CancellationToken ct = default)
        => await _cache.GetOrRefreshAsync(ct);
}
