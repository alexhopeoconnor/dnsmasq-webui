using DnsmasqWebUI.Models;
using DnsmasqWebUI.Options;
using DnsmasqWebUI.Parsers;
using DnsmasqWebUI.Services.Abstractions;
using Microsoft.Extensions.Options;

namespace DnsmasqWebUI.Services;

public class DnsmasqConfigSetService : IDnsmasqConfigSetService
{
    private readonly DnsmasqOptions _options;

    public DnsmasqConfigSetService(IOptions<DnsmasqOptions> options)
    {
        _options = options.Value;
    }

    public Task<DnsmasqConfigSet> GetConfigSetAsync(CancellationToken ct = default) =>
        Task.FromResult(GetConfigSet());

    /// <summary>Leases path discovered from the config set (dhcp-leasefile= or dhcp-lease-file=; last wins). Null if main config missing or no directive found.</summary>
    public string? GetLeasesPath()
    {
        var set = GetConfigSet();
        if (set.Files.Count == 0)
            return null;
        var paths = set.Files.Select(f => f.Path).ToList();
        return DnsmasqConfIncludeParser.GetDhcpLeaseFilePathFromConfigFiles(paths);
    }

    /// <summary>Additional hosts paths discovered from the config set (addn-hosts=; cumulative). Empty list if main config missing or no addn-hosts.</summary>
    public IReadOnlyList<string> GetAddnHostsPaths()
    {
        var set = GetConfigSet();
        if (set.Files.Count == 0)
            return Array.Empty<string>();
        var paths = set.Files.Select(f => f.Path).ToList();
        return DnsmasqConfIncludeParser.GetAddnHostsPathsFromConfigFiles(paths);
    }

    private DnsmasqConfigSet GetConfigSet()
    {
        var mainPath = _options.MainConfigPath;
        if (string.IsNullOrEmpty(mainPath))
            return new DnsmasqConfigSet("", "", Array.Empty<DnsmasqConfigSetEntry>());

        var mainFull = Path.GetFullPath(mainPath);
        var withSource = DnsmasqConfIncludeParser.GetIncludedPathsWithSource(mainPath);
        var firstConfDir = DnsmasqConfIncludeParser.GetFirstConfDir(mainPath);
        var managedFilePath = !string.IsNullOrEmpty(firstConfDir)
            ? Path.Combine(firstConfDir, _options.ManagedFileName)
            : "";

        var files = withSource.Select(p => new DnsmasqConfigSetEntry(
            p.Path,
            Path.GetFileName(p.Path),
            p.Source,
            IsManaged: string.Equals(p.Path, managedFilePath, StringComparison.Ordinal)
        )).ToList();

        return new DnsmasqConfigSet(mainFull, managedFilePath, files);
    }
}
