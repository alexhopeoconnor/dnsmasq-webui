using DnsmasqWebUI.Infrastructure.Helpers.Config;
using DnsmasqWebUI.Infrastructure.Services.EffectiveConfig.Metadata;
using DnsmasqWebUI.Infrastructure.Serialization.Parsers.DnsmasqConfig;
using DnsmasqWebUI.Infrastructure.Services.Dnsmasq.Config.Abstractions;
using DnsmasqWebUI.Infrastructure.Services.Dnsmasq.Hosts.Abstractions;
using DnsmasqWebUI.Models.Hosts;
using Microsoft.Extensions.Logging;

namespace DnsmasqWebUI.Infrastructure.Services.Dnsmasq.Hosts;

public class HostsFileService : IHostsFileService
{
    private readonly IDnsmasqConfigSetService _configSetService;
    private readonly IHostsCache _hostsCache;
    private readonly ILogger<HostsFileService> _logger;

    public HostsFileService(IDnsmasqConfigSetService configSetService, IHostsCache hostsCache, ILogger<HostsFileService> logger)
    {
        _configSetService = configSetService;
        _hostsCache = hostsCache;
        _logger = logger;
    }

    public async Task<IReadOnlyList<HostEntry>> ReadAsync(CancellationToken ct = default)
    {
        var snapshot = await _hostsCache.GetSnapshotAsync(ct);
        return snapshot.ManagedEntries;
    }

    public async Task WriteAsync(IReadOnlyList<HostEntry> entries, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(entries);
        var validationErrors = HostsEntryEditValidator.GetEntryErrors(entries);
        if (validationErrors.Count > 0)
            throw new ArgumentException("Managed hosts file contains invalid entries: " + string.Join(" ", validationErrors), nameof(entries));

        var set = await _configSetService.GetConfigSetAsync(ct);
        var path = set.ManagedHostsFilePath;
        if (string.IsNullOrEmpty(path))
            throw new InvalidOperationException("No managed hosts path configured. Set Dnsmasq:MainConfigPath and optionally Dnsmasq:ManagedFilesDirectory or Dnsmasq:ManagedHostsFileName to enable hosts editing.");
        var dir = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            Directory.CreateDirectory(dir);

        var tmpPath = Path.Combine(
            string.IsNullOrEmpty(dir) ? Directory.GetCurrentDirectory() : dir,
            $".{Path.GetFileName(path)}.{Guid.NewGuid():N}.tmp");
        var lines = entries.Select(HostsFileLineParser.ToLine).ToList();
        try
        {
            await File.WriteAllLinesAsync(tmpPath, lines, DnsmasqFileEncoding.Utf8NoBom, ct);
            // Same-directory replacement avoids readers observing a partially written hosts file.
            File.Move(tmpPath, path, overwrite: true);
        }
        finally
        {
            if (File.Exists(tmpPath))
                File.Delete(tmpPath);
        }

        _hostsCache.NotifyWeWroteManagedHosts(entries);
        _logger.LogInformation("Wrote managed hosts file: {Path}", path);
    }
}
