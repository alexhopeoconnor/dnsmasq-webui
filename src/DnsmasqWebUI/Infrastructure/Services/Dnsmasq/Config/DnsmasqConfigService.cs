using System.Text;
using DnsmasqWebUI.Infrastructure.Helpers.Config;
using DnsmasqWebUI.Infrastructure.Parsers;
using DnsmasqWebUI.Infrastructure.Services.Dnsmasq.Config.Abstractions;
using DnsmasqWebUI.Models.Config;
using DnsmasqWebUI.Models.Contracts;
using DnsmasqWebUI.Models.Dhcp;
using DnsmasqWebUI.Models.Dnsmasq;
using DnsmasqWebUI.Models.Dnsmasq.EffectiveConfig;
using Microsoft.Extensions.Logging;

namespace DnsmasqWebUI.Infrastructure.Services.Dnsmasq.Config;

public class DnsmasqConfigService : IDnsmasqConfigService
{
    private readonly IDnsmasqConfigSetService _configSetService;
    private readonly IConfigSetCache _configSetCache;
    private readonly ILogger<DnsmasqConfigService> _logger;

    public DnsmasqConfigService(IDnsmasqConfigSetService configSetService, IConfigSetCache configSetCache, ILogger<DnsmasqConfigService> logger)
    {
        _configSetService = configSetService;
        _configSetCache = configSetCache;
        _logger = logger;
    }

    private async Task<string?> GetManagedFilePathAsync(CancellationToken ct)
    {
        var set = await _configSetService.GetConfigSetAsync(ct);
        return string.IsNullOrEmpty(set.ManagedFilePath) ? null : set.ManagedFilePath;
    }

    public async Task<IReadOnlyList<DhcpHostEntry>> ReadDhcpHostsAsync(CancellationToken ct = default)
    {
        var snapshot = await _configSetCache.GetSnapshotAsync(ct);
        if (string.IsNullOrEmpty(snapshot.Set.ManagedFilePath))
        {
            _logger.LogDebug("No managed file path (no conf-dir in main config); returning empty dhcp hosts");
            return Array.Empty<DhcpHostEntry>();
        }
        var allEntries = snapshot.DhcpHostEntries.ToList();
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
        File.WriteAllText(managedHostsPath, "", DnsmasqFileEncoding.Utf8NoBom);
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
        {
            var read = await File.ReadAllLinesAsync(path, DnsmasqFileEncoding.Utf8NoBom, ct);
            var list = read.ToList();
            DnsmasqFileEncoding.StripBomFromFirstLine(list);
            rawLines = list;
        }
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
        await File.WriteAllLinesAsync(tmpPath, output, DnsmasqFileEncoding.Utf8NoBom, ct);
        File.Move(tmpPath, path, overwrite: true);
        EnsureManagedHostsFileExists(set.ManagedHostsFilePath);
        var effectiveHostsPathDhcp = configLines.OfType<AddnHostsLine>().FirstOrDefault()?.AddnHostsPath ?? "";
        _configSetCache.NotifyWeWroteManagedConfig(new ManagedConfigContent(configLines, effectiveHostsPathDhcp));
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
            var lines = await File.ReadAllLinesAsync(file.Path, DnsmasqFileEncoding.Utf8NoBom, ct);
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
        var snapshot = await _configSetCache.GetSnapshotAsync(ct);
        if (string.IsNullOrEmpty(snapshot.Set.ManagedFilePath))
            return new ManagedConfigContent(Array.Empty<DnsmasqConfLine>(), "");
        return snapshot.ManagedContent;
    }

    public async Task ApplyEffectiveConfigChangesAsync(IReadOnlyList<PendingEffectiveConfigChange> changes, CancellationToken ct = default)
    {
        if (changes.Count == 0) return;
        var content = await ReadManagedConfigAsync(ct);
        var list = content.Lines.ToList();
        var maxLineNumber = list.Count > 0 ? list.Max(l => l.LineNumber) : 0;
        foreach (var c in changes)
        {
            var behavior = EffectiveConfigParserBehaviorMap.GetBehavior(c.OptionName);
            var isFlag = behavior == EffectiveConfigParserBehavior.Flag;
            bool MatchesOption(DnsmasqConfLine line)
            {
                if (line is not OtherLine o) return false;
                var raw = o.RawLine.Trim();
                return raw == c.OptionName || raw.StartsWith(c.OptionName + "=", StringComparison.Ordinal);
            }

            if (behavior == EffectiveConfigParserBehavior.Multi && TryGetMultiValues(c.NewValue, out var values))
            {
                var matchingIndices = new List<int>();
                for (var i = 0; i < list.Count; i++)
                {
                    if (MatchesOption(list[i])) matchingIndices.Add(i);
                }
                for (var i = matchingIndices.Count - 1; i >= 0; i--)
                    list.RemoveAt(matchingIndices[i]);
                var insertIdx = matchingIndices.Count > 0 ? matchingIndices[0] : list.Count;
                for (var i = 0; i < values.Count; i++)
                {
                    var lineText = string.IsNullOrEmpty(values[i]) ? c.OptionName : c.OptionName + "=" + values[i];
                    var lineObj = new OtherLine { LineNumber = maxLineNumber + 1, RawLine = lineText };
                    maxLineNumber++;
                    list.Insert(insertIdx + i, lineObj);
                }
                continue;
            }

            var idx = list.FindIndex(MatchesOption);
            if (isFlag && c.NewValue is bool b && !b)
            {
                if (idx >= 0) list.RemoveAt(idx);
                continue;
            }
            string rawLine;
            if (isFlag)
                rawLine = c.OptionName;
            else
            {
                var v = ToConfValue(c.NewValue);
                rawLine = string.IsNullOrEmpty(v) ? c.OptionName : c.OptionName + "=" + v;
            }
            var newLine = new OtherLine { LineNumber = maxLineNumber + 1, RawLine = rawLine };
            maxLineNumber++;
            if (idx >= 0)
                list[idx] = newLine;
            else
                list.Add(newLine);
        }
        await WriteManagedConfigAsync(list, ct);
    }

    private static bool TryGetMultiValues(object? value, out IReadOnlyList<string> values)
    {
        values = null!;
        if (value == null) return false;
        if (value is IReadOnlyList<string> list)
        {
            values = list;
            return true;
        }
        if (value is string[] arr)
        {
            values = arr;
            return true;
        }
        if (value is List<string> listMut)
        {
            values = listMut;
            return true;
        }
        return false;
    }

    private static string ToConfValue(object? value)
    {
        if (value == null) return "";
        if (value is bool b) return b ? "1" : "0";
        return value.ToString() ?? "";
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
        await File.WriteAllLinesAsync(tmpPath, output, DnsmasqFileEncoding.Utf8NoBom, ct);
        File.Move(tmpPath, path, overwrite: true);
        EnsureManagedHostsFileExists(set.ManagedHostsFilePath);
        var effectiveHostsPath = list.OfType<AddnHostsLine>().FirstOrDefault()?.AddnHostsPath ?? "";
        _configSetCache.NotifyWeWroteManagedConfig(new ManagedConfigContent(list, effectiveHostsPath));
        _logger.LogInformation("Wrote managed config file: {Path}", path);
    }

}
