using System.Text;
using DnsmasqWebUI.Models;
using DnsmasqWebUI.Options;
using DnsmasqWebUI.Parsers;
using DnsmasqWebUI.Services.Abstractions;
using Microsoft.Extensions.Options;

namespace DnsmasqWebUI.Services;

public class DnsmasqConfigService : IDnsmasqConfigService
{
    private readonly string _path;
    private readonly ILogger<DnsmasqConfigService> _logger;

    public DnsmasqConfigService(IOptions<DnsmasqOptions> options, ILogger<DnsmasqConfigService> logger)
    {
        _path = options.Value.ConfigPath;
        _logger = logger;
    }

    public async Task<IReadOnlyList<DhcpHostEntry>> ReadDhcpHostsAsync(CancellationToken ct = default)
    {
        if (!File.Exists(_path))
        {
            _logger.LogWarning("Config file not found: {Path}", _path);
            return Array.Empty<DhcpHostEntry>();
        }
        var lines = await File.ReadAllLinesAsync(_path, Encoding.UTF8, ct);
        var configLines = DnsmasqConfigParser.ParseFile(lines);
        var entries = configLines.Where(c => c.DhcpHost != null).Select(c => c.DhcpHost!).ToList();
        AssignStableIds(entries);
        return entries;
    }

    /// <summary>Assign stable Ids so we can match by Id on write (reorder-safe). Content-based; ":LineNumber" appended for uniqueness when needed.</summary>
    private static void AssignStableIds(List<DhcpHostEntry> entries)
    {
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var e in entries)
        {
            var baseId = MakeBaseId(e);
            e.Id = seen.Add(baseId) ? baseId : baseId + ":" + e.LineNumber;
        }
    }

    private static string MakeBaseId(DhcpHostEntry e)
    {
        var macs = string.Join(",", e.MacAddresses.OrderBy(x => x, StringComparer.Ordinal));
        var address = e.Address ?? "";
        var name = e.Name ?? "";
        if (macs.Length > 0 || address.Length > 0 || name.Length > 0)
            return macs + "|" + address + "|" + name;
        return "line:" + e.LineNumber;
    }

    public async Task WriteDhcpHostsAsync(IReadOnlyList<DhcpHostEntry> entries, CancellationToken ct = default)
    {
        var dir = Path.GetDirectoryName(_path);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            Directory.CreateDirectory(dir);

        IReadOnlyList<string> rawLines;
        if (File.Exists(_path))
            rawLines = await File.ReadAllLinesAsync(_path, Encoding.UTF8, ct);
        else
            rawLines = Array.Empty<string>();

        var configLines = DnsmasqConfigParser.ParseFile(rawLines).ToList();
        var fileEntries = configLines.Where(c => c.DhcpHost != null).Select(c => c.DhcpHost!).ToList();
        AssignStableIds(fileEntries);

        var byId = entries.Where(e => !string.IsNullOrEmpty(e.Id)).GroupBy(e => e.Id, StringComparer.Ordinal).ToDictionary(g => g.Key, g => g.First(), StringComparer.Ordinal);
        var matchedIds = new HashSet<string>(StringComparer.Ordinal);

        for (var i = 0; i < configLines.Count; i++)
        {
            var cl = configLines[i];
            if (cl.Kind != ConfigLineKind.DhcpHost || cl.DhcpHost == null) continue;
            if (!string.IsNullOrEmpty(cl.DhcpHost.Id) && byId.TryGetValue(cl.DhcpHost.Id, out var replacement))
            {
                matchedIds.Add(cl.DhcpHost.Id);
                configLines[i] = new ConfigLine { Kind = ConfigLineKind.DhcpHost, LineNumber = cl.LineNumber, RawLine = cl.RawLine, DhcpHost = replacement };
            }
        }

        var appended = entries.Where(e => (string.IsNullOrEmpty(e.Id) || !matchedIds.Contains(e.Id)) && !e.IsDeleted).ToList();
        var output = configLines.Select(DnsmasqConfigParser.ToLine).ToList();
        foreach (var entry in appended)
            output.Add(DhcpHostParser.ToLine(entry));

        var tmpPath = _path + ".tmp";
        await File.WriteAllLinesAsync(tmpPath, output, Encoding.UTF8, ct);
        File.Move(tmpPath, _path, overwrite: true);
        _logger.LogInformation("Wrote config file: {Path}", _path);
    }
}
