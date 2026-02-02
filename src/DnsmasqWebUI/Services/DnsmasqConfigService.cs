using System.Text;
using DnsmasqWebUI.Models;
using DnsmasqWebUI.Models.Config;
using DnsmasqWebUI.Parsers;
using DnsmasqWebUI.Services.Abstractions;

namespace DnsmasqWebUI.Services;

public class DnsmasqConfigService : IDnsmasqConfigService
{
    private readonly IDnsmasqConfigSetService _configSetService;
    private readonly ILogger<DnsmasqConfigService> _logger;

    public DnsmasqConfigService(IDnsmasqConfigSetService configSetService, ILogger<DnsmasqConfigService> logger)
    {
        _configSetService = configSetService;
        _logger = logger;
    }

    private async Task<string?> GetManagedFilePathAsync(CancellationToken ct)
    {
        var set = await _configSetService.GetConfigSetAsync(ct);
        return string.IsNullOrEmpty(set.ManagedFilePath) ? null : set.ManagedFilePath;
    }

    public async Task<IReadOnlyList<DhcpHostEntry>> ReadDhcpHostsAsync(CancellationToken ct = default)
    {
        var set = await _configSetService.GetConfigSetAsync(ct);
        if (string.IsNullOrEmpty(set.ManagedFilePath))
        {
            _logger.LogDebug("No managed file path (no conf-dir in main config); returning empty dhcp hosts");
            return Array.Empty<DhcpHostEntry>();
        }
        var allEntries = new List<DhcpHostEntry>();
        foreach (var file in set.Files)
        {
            if (!File.Exists(file.Path))
                continue;
            var lines = await File.ReadAllLinesAsync(file.Path, Encoding.UTF8, ct);
            var configLines = DnsmasqConfFileLineParser.ParseFile(lines);
            var entries = configLines.OfType<DhcpHostLine>().Select(c => c.DhcpHost).ToList();
            foreach (var e in entries)
            {
                e.SourcePath = file.Path;
                e.IsEditable = file.IsManaged;
            }
            allEntries.AddRange(entries);
        }
        AssignStableIds(allEntries);
        return allEntries;
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

    /// <summary>When managedHostsPath is set, ensures the managed config has exactly one addn-hosts line pointing to it (replaces the first AddnHosts line or inserts at start). So dnsmasq loads our managed hosts file last.</summary>
    private static void EnsureOneAddnHostsLine(List<DnsmasqConfLine> configLines, string? managedHostsPath)
    {
        if (string.IsNullOrEmpty(managedHostsPath))
            return;
        var idx = configLines.FindIndex(c => c.Kind == DnsmasqConfLineKind.AddnHosts);
        var lineNumber = idx >= 0 ? configLines[idx].LineNumber : 1;
        var line = new AddnHostsLine { LineNumber = lineNumber, AddnHostsPath = managedHostsPath };
        if (idx >= 0)
            configLines[idx] = line;
        else
            configLines.Insert(0, line);
    }

    /// <summary>Creates the managed hosts file empty if it does not exist, so dnsmasq does not error when we add addn-hosts=&lt;path&gt; to the managed config.</summary>
    private static void EnsureManagedHostsFileExists(string? managedHostsPath)
    {
        if (string.IsNullOrEmpty(managedHostsPath) || File.Exists(managedHostsPath))
            return;
        var dir = Path.GetDirectoryName(managedHostsPath);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            Directory.CreateDirectory(dir);
        File.WriteAllText(managedHostsPath, "");
    }

    public async Task WriteDhcpHostsAsync(IReadOnlyList<DhcpHostEntry> entries, CancellationToken ct = default)
    {
        var set = await _configSetService.GetConfigSetAsync(ct);
        var path = set.ManagedFilePath;
        if (string.IsNullOrEmpty(path))
            throw new InvalidOperationException("No managed file path (main config has no conf-dir). Cannot write dhcp hosts.");

        var managedEntries = entries.Where(e => e.IsEditable).ToList();
        var externalMacs = await GetMacsFromNonManagedFilesAsync(set, ct);
        foreach (var e in managedEntries.Where(e => !e.IsDeleted))
        {
            foreach (var mac in e.MacAddresses.Where(m => !string.IsNullOrWhiteSpace(m)))
            {
                var normalized = mac.Trim();
                if (externalMacs.TryGetValue(normalized, out var sourcePath))
                    throw new ArgumentException($"MAC {mac} is already defined in {sourcePath}. Remove it from that file or use a different MAC.");
            }
        }

        var dir = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            Directory.CreateDirectory(dir);

        IReadOnlyList<string> rawLines;
        if (File.Exists(path))
            rawLines = await File.ReadAllLinesAsync(path, Encoding.UTF8, ct);
        else
            rawLines = Array.Empty<string>();

        var configLines = DnsmasqConfFileLineParser.ParseFile(rawLines).ToList();
        EnsureOneAddnHostsLine(configLines, set.ManagedHostsFilePath);
        var fileEntries = configLines.OfType<DhcpHostLine>().Select(c => c.DhcpHost).ToList();
        AssignStableIds(fileEntries);

        var byId = managedEntries.Where(e => !string.IsNullOrEmpty(e.Id)).GroupBy(e => e.Id, StringComparer.Ordinal).ToDictionary(g => g.Key, g => g.First(), StringComparer.Ordinal);
        var matchedIds = new HashSet<string>(StringComparer.Ordinal);

        for (var i = 0; i < configLines.Count; i++)
        {
            if (configLines[i] is not DhcpHostLine dhcpLine) continue;
            if (string.IsNullOrEmpty(dhcpLine.DhcpHost.Id) || !byId.TryGetValue(dhcpLine.DhcpHost.Id, out var replacement)) continue;
            matchedIds.Add(dhcpLine.DhcpHost.Id);
            configLines[i] = new DhcpHostLine { LineNumber = dhcpLine.LineNumber, DhcpHost = replacement };
        }

        var appended = managedEntries.Where(e => (string.IsNullOrEmpty(e.Id) || !matchedIds.Contains(e.Id)) && !e.IsDeleted).ToList();
        var output = configLines.Select(DnsmasqConfFileLineParser.ToLine).ToList();
        foreach (var entry in appended)
            output.Add(DnsmasqConfDhcpHostLineParser.ToLine(entry));

        var tmpPath = path + ".tmp";
        await File.WriteAllLinesAsync(tmpPath, output, Encoding.UTF8, ct);
        File.Move(tmpPath, path, overwrite: true);
        EnsureManagedHostsFileExists(set.ManagedHostsFilePath);
        _logger.LogInformation("Wrote managed config file: {Path}", path);
    }

    /// <summary>Returns MAC -> source file path for all dhcp-host MACs in non-managed config files (for duplicate validation).</summary>
    private static async Task<Dictionary<string, string>> GetMacsFromNonManagedFilesAsync(DnsmasqConfigSet set, CancellationToken ct)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var managedPath = set.ManagedFilePath ?? "";
        foreach (var file in set.Files.Where(f => !f.IsManaged && !string.Equals(f.Path, managedPath, StringComparison.Ordinal)))
        {
            if (!File.Exists(file.Path)) continue;
            var lines = await File.ReadAllLinesAsync(file.Path, Encoding.UTF8, ct);
            var configLines = DnsmasqConfFileLineParser.ParseFile(lines);
            foreach (var dhcp in configLines.OfType<DhcpHostLine>().Select(c => c.DhcpHost))
            {
                foreach (var mac in dhcp.MacAddresses.Where(m => !string.IsNullOrWhiteSpace(m)))
                {
                    var normalized = mac.Trim();
                    if (normalized.Length > 0 && !result.ContainsKey(normalized))
                        result[normalized] = file.Path;
                }
            }
        }
        return result;
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
        var set = await _configSetService.GetConfigSetAsync(ct);
        var path = set.ManagedFilePath;
        if (string.IsNullOrEmpty(path))
            throw new InvalidOperationException("No managed file path (main config has no conf-dir). Cannot write managed config.");

        var dir = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            Directory.CreateDirectory(dir);

        var list = lines.ToList();
        EnsureOneAddnHostsLine(list, set.ManagedHostsFilePath);
        var output = list.Select(DnsmasqConfFileLineParser.ToLine).ToList();

        var tmpPath = path + ".tmp";
        await File.WriteAllLinesAsync(tmpPath, output, Encoding.UTF8, ct);
        File.Move(tmpPath, path, overwrite: true);
        EnsureManagedHostsFileExists(set.ManagedHostsFilePath);
        _logger.LogInformation("Wrote managed config file: {Path}", path);
    }
}
