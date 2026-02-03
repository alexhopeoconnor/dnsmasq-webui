using System.Text;
using DnsmasqWebUI.Models.Hosts;
using DnsmasqWebUI.Parsers;
using DnsmasqWebUI.Services.Abstractions;

namespace DnsmasqWebUI.Services;

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
        var set = await _configSetService.GetConfigSetAsync(ct);
        var path = set.ManagedHostsFilePath;
        if (string.IsNullOrEmpty(path))
            throw new InvalidOperationException("No managed hosts path configured. Set Dnsmasq:MainConfigPath and Dnsmasq:ManagedHostsFileName to enable hosts editing.");
        var dir = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            Directory.CreateDirectory(dir);

        var tmpPath = path + ".tmp";
        var lines = entries.Select(HostsFileLineParser.ToLine).ToList();
        await File.WriteAllLinesAsync(tmpPath, lines, Encoding.UTF8, ct);
        File.Move(tmpPath, path, overwrite: true);
        _hostsCache.NotifyWeWroteManagedHosts(entries);
        _logger.LogInformation("Wrote managed hosts file: {Path}", path);
    }
}
