using DnsmasqWebUI.Infrastructure.Services.Dnsmasq.Hosts.Abstractions;
using DnsmasqWebUI.Models.Config;
using DnsmasqWebUI.Models.Dnsmasq.EffectiveConfig;
using DnsmasqWebUI.Models.Hosts;
using Microsoft.Extensions.Options;

namespace DnsmasqWebUI.Infrastructure.Services.Dnsmasq.Hosts;

/// <summary>
/// Converts dnsmasq's hosts-related effective config into concrete source items.
/// dnsmasq treats an <c>addn-hosts</c> directory as all of its files in alphabetical order; <c>hostsdir</c>
/// is represented the same way so the caller can watch both the directory and each currently discovered file.
/// </summary>
public sealed class HostSourceResolver : IHostSourceResolver
{
    private readonly DnsmasqOptions _options;

    public HostSourceResolver(IOptions<DnsmasqOptions> options)
    {
        _options = options.Value;
    }

    public IReadOnlyList<HostSource> Resolve(
        string? managedHostsPath,
        EffectiveDnsmasqConfig effectiveConfig,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(effectiveConfig);

        var sources = new List<HostSource>();
        var managedPathFull = TryGetFullPath(managedHostsPath);
        var systemPathFull = TryGetFullPath(_options.SystemHostsPath);
        var loadOrder = 0;

        // dnsmasq loads its system hosts file unless no-hosts is set. Keep the source visible when inactive:
        // an explicitly configured addn-hosts entry for the same physical path remains active and is added below.
        if (systemPathFull != null)
        {
            sources.Add(CreateFileSource(
                HostsRowSourceKind.SystemHosts,
                systemPathFull,
                systemPathFull,
                isActive: !effectiveConfig.NoHosts,
                inactiveReason: effectiveConfig.NoHosts
                    ? "System hosts is ignored because no-hosts is enabled in dnsmasq config."
                    : null,
                loadOrder: loadOrder++));
        }

        foreach (var configuredPath in effectiveConfig.AddnHostsPaths ?? Array.Empty<string>())
        {
            ct.ThrowIfCancellationRequested();
            var fullPath = TryGetFullPath(configuredPath);
            if (fullPath == null)
                continue;

            // The app-owned path is read separately because it is the only file the UI can edit.
            if (managedPathFull != null && PathsEqual(fullPath, managedPathFull))
            {
                sources.Add(CreateFileSource(
                    HostsRowSourceKind.Managed,
                    managedPathFull,
                    fullPath,
                    isActive: true,
                    inactiveReason: null,
                    loadOrder: loadOrder++));
                continue;
            }

            AddConfiguredPath(
                sources,
                HostsRowSourceKind.AddnHosts,
                fullPath,
                isActive: true,
                inactiveReason: null,
                ref loadOrder,
                ct);
        }

        // A managed hosts path is useful to display/edit even before its managed addn-hosts directive exists.
        // Mark it inactive rather than claiming dnsmasq loads it.
        if (managedPathFull != null && !sources.Any(s => s.SourceKind == HostsRowSourceKind.Managed))
        {
            sources.Add(CreateFileSource(
                HostsRowSourceKind.Managed,
                managedPathFull,
                managedPathFull,
                isActive: false,
                inactiveReason: "Managed hosts file is not configured through addn-hosts.",
                loadOrder: loadOrder++));
        }

        var hostsdirPath = TryGetFullPath(effectiveConfig.HostsdirPath);
        if (hostsdirPath != null)
        {
            AddConfiguredPath(
                sources,
                HostsRowSourceKind.Hostsdir,
                hostsdirPath,
                isActive: true,
                inactiveReason: null,
                ref loadOrder,
                ct);
        }

        return sources;
    }

    private static void AddConfiguredPath(
        List<HostSource> sources,
        HostsRowSourceKind sourceKind,
        string fullPath,
        bool isActive,
        string? inactiveReason,
        ref int loadOrder,
        CancellationToken ct)
    {
        if (!Directory.Exists(fullPath))
        {
            sources.Add(CreateFileSource(
                sourceKind,
                fullPath,
                fullPath,
                isActive,
                inactiveReason,
                loadOrder++));
            return;
        }

        sources.Add(new HostSource(
            sourceKind,
            fullPath,
            fullPath,
            IsDirectory: true,
            isActive,
            inactiveReason,
            loadOrder++,
            Diagnostic: null,
            EntryCount: 0));

        try
        {
            foreach (var memberPath in Directory.GetFiles(fullPath).OrderBy(x => x, StringComparer.Ordinal))
            {
                ct.ThrowIfCancellationRequested();
                sources.Add(CreateFileSource(
                    sourceKind,
                    Path.GetFullPath(memberPath),
                    fullPath,
                    isActive,
                    inactiveReason,
                    loadOrder++));
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            var directoryIndex = sources.FindLastIndex(s => s.IsDirectory && PathsEqual(s.Path, fullPath));
            if (directoryIndex >= 0)
            {
                var source = sources[directoryIndex];
                sources[directoryIndex] = source with { Diagnostic = "Directory could not be enumerated." };
            }
        }
    }

    private static HostSource CreateFileSource(
        HostsRowSourceKind sourceKind,
        string path,
        string configuredPath,
        bool isActive,
        string? inactiveReason,
        int loadOrder)
    {
        var diagnostic = File.Exists(path) ? null : "File not found.";
        return new HostSource(
            sourceKind,
            path,
            configuredPath,
            IsDirectory: false,
            isActive,
            inactiveReason,
            loadOrder,
            diagnostic,
            EntryCount: 0);
    }

    private static string? TryGetFullPath(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return null;

        try
        {
            return Path.GetFullPath(path.Trim());
        }
        catch (Exception ex) when (ex is ArgumentException or NotSupportedException or PathTooLongException)
        {
            return null;
        }
    }

    private static bool PathsEqual(string left, string right) =>
        string.Equals(left, right, StringComparison.Ordinal);
}
