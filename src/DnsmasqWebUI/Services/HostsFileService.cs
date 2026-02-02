using System.Text;
using DnsmasqWebUI.Models.EffectiveConfig;
using DnsmasqWebUI.Models.Hosts;
using DnsmasqWebUI.Parsers;
using DnsmasqWebUI.Services.Abstractions;

namespace DnsmasqWebUI.Services;

public class HostsFileService : IHostsFileService
{
    private readonly IDnsmasqConfigSetService _configSetService;
    private readonly ILogger<HostsFileService> _logger;

    public HostsFileService(IDnsmasqConfigSetService configSetService, ILogger<HostsFileService> logger)
    {
        _configSetService = configSetService;
        _logger = logger;
    }

    public async Task<IReadOnlyList<HostEntry>> ReadAsync(CancellationToken ct = default)
    {
        var set = await _configSetService.GetConfigSetAsync(ct);
        var path = set.ManagedHostsFilePath;
        if (string.IsNullOrEmpty(path))
            return Array.Empty<HostEntry>();
        if (!File.Exists(path))
        {
            _logger.LogDebug("Managed hosts file not found: {Path}", path);
            return Array.Empty<HostEntry>();
        }
        var lines = await File.ReadAllLinesAsync(path, Encoding.UTF8, ct);
        var entries = new List<HostEntry>();
        var seenContentIds = new HashSet<string>(StringComparer.Ordinal);
        for (var i = 0; i < lines.Length; i++)
        {
            var entry = HostsFileLineParser.ParseLine(lines[i], i + 1);
            if (entry == null) continue;
            if (entry.IsPassthrough || string.IsNullOrEmpty(entry.Address))
                entry.Id = "line:" + entry.LineNumber;
            else
            {
                var contentId = entry.Address + "|" + string.Join(",", entry.Names.OrderBy(x => x, StringComparer.Ordinal));
                entry.Id = seenContentIds.Add(contentId) ? contentId : contentId + ":" + entry.LineNumber;
            }
            entries.Add(entry);
        }
        return entries;
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
        _logger.LogInformation("Wrote managed hosts file: {Path}", path);
    }
}
