using DnsmasqWebUI.Infrastructure.Logging;
using DnsmasqWebUI.Infrastructure.Services.Abstractions;
using DnsmasqWebUI.Models.Dhcp;
using Microsoft.Extensions.Logging;

namespace DnsmasqWebUI.Infrastructure.Services;

public class LeasesFileService : ILeasesFileService
{
    private readonly ILeasesCache _cache;
    private readonly ILogger<LeasesFileService> _logger;

    public LeasesFileService(ILeasesCache cache, ILogger<LeasesFileService> logger)
    {
        _cache = cache;
        _logger = logger;
    }

    public async Task<IReadOnlyList<LeaseEntry>> ReadAsync(CancellationToken ct = default)
    {
        var (_, entries) = await _cache.GetOrRefreshAsync(ct);
        return entries ?? Array.Empty<LeaseEntry>();
    }

    public async Task<(bool Available, IReadOnlyList<LeaseEntry>? Entries)> TryReadAsync(CancellationToken ct = default)
    {
        var (available, entries) = await _cache.GetOrRefreshAsync(ct);
        if (available && entries != null)
            _logger.LogInformation(LogEvents.LeasesReadSuccess, "Leases read, count={Count}", entries.Count);
        return (available, entries);
    }
}
