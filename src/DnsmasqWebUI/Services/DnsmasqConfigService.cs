using System.Text;
using DnsmasqWebUI.Models;
using DnsmasqWebUI.Options;
using DnsmasqWebUI.Parsers;
using DnsmasqWebUI.Services.Abstractions;
using Microsoft.Extensions.Options;

namespace DnsmasqWebUI.Services;

public class DnsmasqConfigService : IDnsmasqConfigService
{
    private readonly IDnsmasqConfigSetService _configSetService;
    private readonly string _hostsPath;
    private readonly ILogger<DnsmasqConfigService> _logger;

    public DnsmasqConfigService(IDnsmasqConfigSetService configSetService, IOptions<DnsmasqOptions> options, ILogger<DnsmasqConfigService> logger)
    {
        _configSetService = configSetService;
        _hostsPath = options.Value.HostsPath ?? "";
        _logger = logger;
    }

    private async Task<string?> GetManagedFilePathAsync(CancellationToken ct)
    {
        var set = await _configSetService.GetConfigSetAsync(ct);
        return string.IsNullOrEmpty(set.ManagedFilePath) ? null : set.ManagedFilePath;
    }

    public async Task<IReadOnlyList<DhcpHostEntry>> ReadDhcpHostsAsync(CancellationToken ct = default)
    {
        var path = await GetManagedFilePathAsync(ct);
        if (string.IsNullOrEmpty(path))
        {
            _logger.LogDebug("No managed file path (no conf-dir in main config); returning empty dhcp hosts");
            return Array.Empty<DhcpHostEntry>();
        }
        if (!File.Exists(path))
        {
            _logger.LogWarning("Managed config file not found: {Path}", path);
            return Array.Empty<DhcpHostEntry>();
        }
        var lines = await File.ReadAllLinesAsync(path, Encoding.UTF8, ct);
        var configLines = DnsmasqConfFileLineParser.ParseFile(lines);
        var entries = configLines.OfType<DhcpHostLine>().Select(c => c.DhcpHost).ToList();
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

    /// <summary>Ensures exactly one AddnHosts line with path = hostsPath. Replaces first AddnHosts or inserts at start.</summary>
    private static void EnsureOneAddnHostsLine(List<DnsmasqConfLine> configLines, string hostsPath)
    {
        if (string.IsNullOrEmpty(hostsPath))
            return;
        var idx = configLines.FindIndex(c => c.Kind == DnsmasqConfLineKind.AddnHosts);
        var lineNumber = idx >= 0 ? configLines[idx].LineNumber : 1;
        var line = new AddnHostsLine { LineNumber = lineNumber, AddnHostsPath = hostsPath };
        if (idx >= 0)
            configLines[idx] = line;
        else
            configLines.Insert(0, line);
    }

    public async Task WriteDhcpHostsAsync(IReadOnlyList<DhcpHostEntry> entries, CancellationToken ct = default)
    {
        var path = await GetManagedFilePathAsync(ct);
        if (string.IsNullOrEmpty(path))
            throw new InvalidOperationException("No managed file path (main config has no conf-dir). Cannot write dhcp hosts.");

        var dir = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            Directory.CreateDirectory(dir);

        IReadOnlyList<string> rawLines;
        if (File.Exists(path))
            rawLines = await File.ReadAllLinesAsync(path, Encoding.UTF8, ct);
        else
            rawLines = Array.Empty<string>();

        var configLines = DnsmasqConfFileLineParser.ParseFile(rawLines).ToList();
        EnsureOneAddnHostsLine(configLines, _hostsPath);
        var fileEntries = configLines.OfType<DhcpHostLine>().Select(c => c.DhcpHost).ToList();
        AssignStableIds(fileEntries);

        var byId = entries.Where(e => !string.IsNullOrEmpty(e.Id)).GroupBy(e => e.Id, StringComparer.Ordinal).ToDictionary(g => g.Key, g => g.First(), StringComparer.Ordinal);
        var matchedIds = new HashSet<string>(StringComparer.Ordinal);

        for (var i = 0; i < configLines.Count; i++)
        {
            if (configLines[i] is not DhcpHostLine dhcpLine) continue;
            if (string.IsNullOrEmpty(dhcpLine.DhcpHost.Id) || !byId.TryGetValue(dhcpLine.DhcpHost.Id, out var replacement)) continue;
            matchedIds.Add(dhcpLine.DhcpHost.Id);
            configLines[i] = new DhcpHostLine { LineNumber = dhcpLine.LineNumber, DhcpHost = replacement };
        }

        var appended = entries.Where(e => (string.IsNullOrEmpty(e.Id) || !matchedIds.Contains(e.Id)) && !e.IsDeleted).ToList();
        var output = configLines.Select(DnsmasqConfFileLineParser.ToLine).ToList();
        foreach (var entry in appended)
            output.Add(DnsmasqConfDhcpHostLineParser.ToLine(entry));

        var tmpPath = path + ".tmp";
        await File.WriteAllLinesAsync(tmpPath, output, Encoding.UTF8, ct);
        File.Move(tmpPath, path, overwrite: true);
        _logger.LogInformation("Wrote managed config file: {Path}", path);
    }

    public async Task<ManagedConfigContent> ReadManagedConfigAsync(CancellationToken ct = default)
    {
        var path = await GetManagedFilePathAsync(ct);
        if (string.IsNullOrEmpty(path))
            return new ManagedConfigContent(Array.Empty<DnsmasqConfLine>(), "");

        if (!File.Exists(path))
        {
            _logger.LogWarning("Managed config file not found: {Path}", path);
            return new ManagedConfigContent(Array.Empty<DnsmasqConfLine>(), "");
        }

        var lines = await File.ReadAllLinesAsync(path, Encoding.UTF8, ct);
        var configLines = DnsmasqConfFileLineParser.ParseFile(lines);
        var effectiveHostsPath = configLines.OfType<AddnHostsLine>().FirstOrDefault()?.AddnHostsPath ?? "";
        return new ManagedConfigContent(configLines, effectiveHostsPath);
    }

    public async Task WriteManagedConfigAsync(IReadOnlyList<DnsmasqConfLine> lines, CancellationToken ct = default)
    {
        var path = await GetManagedFilePathAsync(ct);
        if (string.IsNullOrEmpty(path))
            throw new InvalidOperationException("No managed file path (main config has no conf-dir). Cannot write managed config.");

        var dir = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            Directory.CreateDirectory(dir);

        var list = lines.ToList();
        EnsureOneAddnHostsLine(list, _hostsPath);
        var output = list.Select(DnsmasqConfFileLineParser.ToLine).ToList();

        var tmpPath = path + ".tmp";
        await File.WriteAllLinesAsync(tmpPath, output, Encoding.UTF8, ct);
        File.Move(tmpPath, path, overwrite: true);
        _logger.LogInformation("Wrote managed config file: {Path}", path);
    }
}
