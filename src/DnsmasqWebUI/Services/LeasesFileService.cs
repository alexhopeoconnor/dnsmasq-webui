using System.Text;
using DnsmasqWebUI.Models;
using DnsmasqWebUI.Options;
using DnsmasqWebUI.Parsers;
using DnsmasqWebUI.Services.Abstractions;
using Microsoft.Extensions.Options;

namespace DnsmasqWebUI.Services;

public class LeasesFileService : ILeasesFileService
{
    private readonly string? _path;
    private readonly ILogger<LeasesFileService> _logger;

    public LeasesFileService(IOptions<DnsmasqOptions> options, ILogger<LeasesFileService> logger)
    {
        _path = options.Value.LeasesPath;
        _logger = logger;
    }

    public async Task<IReadOnlyList<LeaseEntry>> ReadAsync(CancellationToken ct = default)
    {
        var (available, entries) = await TryReadAsync(ct);
        return entries ?? Array.Empty<LeaseEntry>();
    }

    public async Task<(bool Available, IReadOnlyList<LeaseEntry>? Entries)> TryReadAsync(CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(_path))
        {
            _logger.LogDebug("Leases path not configured");
            return (false, null);
        }
        if (!File.Exists(_path))
        {
            _logger.LogDebug("Leases file not found: {Path}", _path);
            return (true, Array.Empty<LeaseEntry>());
        }
        try
        {
            var lines = await File.ReadAllLinesAsync(_path, Encoding.UTF8, ct);
            var entries = new List<LeaseEntry>();
            foreach (var line in lines)
            {
                var entry = LeasesParser.ParseLine(line);
                if (entry != null)
                    entries.Add(entry);
            }
            return (true, entries);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to read leases file: {Path}", _path);
            return (true, null);
        }
    }
}
