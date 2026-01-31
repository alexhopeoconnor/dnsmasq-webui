using System.Text;
using DnsmasqWebUI.Models;
using DnsmasqWebUI.Options;
using DnsmasqWebUI.Parsers;
using DnsmasqWebUI.Services.Abstractions;
using Microsoft.Extensions.Options;

namespace DnsmasqWebUI.Services;

public class HostsFileService : IHostsFileService
{
    private readonly string _path;
    private readonly ILogger<HostsFileService> _logger;

    public HostsFileService(IOptions<DnsmasqOptions> options, ILogger<HostsFileService> logger)
    {
        _path = options.Value.SystemHostsPath?.Trim() ?? "";
        _logger = logger;
    }

    public async Task<IReadOnlyList<HostEntry>> ReadAsync(CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(_path))
            return Array.Empty<HostEntry>();
        if (!File.Exists(_path))
        {
            _logger.LogWarning("Hosts file not found: {Path}", _path);
            return Array.Empty<HostEntry>();
        }
        var lines = await File.ReadAllLinesAsync(_path, Encoding.UTF8, ct);
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
        if (string.IsNullOrEmpty(_path))
            throw new InvalidOperationException("No system hosts file configured. Set Dnsmasq:SystemHostsPath to enable hosts editing.");
        var dir = Path.GetDirectoryName(_path);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            Directory.CreateDirectory(dir);

        var tmpPath = _path + ".tmp";
        var lines = entries.Select(HostsFileLineParser.ToLine).ToList();
        await File.WriteAllLinesAsync(tmpPath, lines, Encoding.UTF8, ct);
        File.Move(tmpPath, _path, overwrite: true);
        _logger.LogInformation("Wrote hosts file: {Path}", _path);
    }
}
