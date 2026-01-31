using DnsmasqWebUI.Models;

namespace DnsmasqWebUI.Parsers;

/// <summary>
/// Parses the main dnsmasq .conf file only for <c>conf-file=</c> and <c>conf-dir=</c> directives
/// to discover the ordered list of included config file paths. Used for config set discovery.
/// Line parsing uses <see cref="DnsmasqConfDirectiveParser.TryParseKeyValue"/> (Superpower).
/// For parsing .conf file content use <see cref="DnsmasqConfFileLineParser"/> or <see cref="DnsmasqConfDirectiveParser"/>.
/// See: https://thekelleys.org.uk/dnsmasq/docs/dnsmasq-man.html
/// </summary>
public static class DnsmasqConfIncludeParser
{
    /// <summary>
    /// Returns the ordered list of absolute config file paths dnsmasq loads: main file first,
    /// then each conf-file= path in order, then each file from each conf-dir= (alphabetically).
    /// </summary>
    public static IReadOnlyList<string> GetIncludedPaths(string mainConfigPath)
    {
        return GetIncludedPathsWithSource(mainConfigPath)
            .Select(x => x.Path)
            .ToList();
    }

    /// <summary>
    /// Returns the ordered list of (path, source) for config set display. Main first, then ConfFile entries, then ConfDir entries.
    /// </summary>
    public static IReadOnlyList<(string Path, DnsmasqConfFileSource Source)> GetIncludedPathsWithSource(string mainConfigPath)
    {
        var mainFull = Path.GetFullPath(mainConfigPath);
        var mainDir = Path.GetDirectoryName(mainFull) ?? "";
        var result = new List<(string Path, DnsmasqConfFileSource Source)> { (mainFull, DnsmasqConfFileSource.Main) };

        if (!File.Exists(mainFull))
            return result;

        foreach (var line in File.ReadAllLines(mainFull))
        {
            var kv = DnsmasqConfDirectiveParser.TryParseKeyValue(line);
            if (kv == null)
                continue;

            var (key, value) = kv.Value;
            if (key.Equals("conf-file", StringComparison.OrdinalIgnoreCase))
            {
                var path = value.Trim();
                var resolved = Path.GetFullPath(Path.Combine(mainDir, path));
                if (File.Exists(resolved))
                    result.Add((resolved, DnsmasqConfFileSource.ConfFile));
                continue;
            }

            if (key.Equals("conf-dir", StringComparison.OrdinalIgnoreCase))
            {
                var path = value.Split(',')[0].Trim();
                var dir = Path.GetFullPath(Path.Combine(mainDir, path));
                if (!Directory.Exists(dir))
                    continue;
                var files = Directory.GetFiles(dir)
                    .OrderBy(Path.GetFileName, StringComparer.Ordinal)
                    .ToList();
                foreach (var f in files)
                    result.Add((f, DnsmasqConfFileSource.ConfDir));
            }
        }

        return result;
    }

    /// <summary>
    /// Returns the first conf-dir= path (absolute directory), or null if none. Used as the directory
    /// where we create the managed file (e.g. zz-dnsmasq-webui.conf, so it loads last).
    /// </summary>
    public static string? GetFirstConfDir(string mainConfigPath)
    {
        var mainFull = Path.GetFullPath(mainConfigPath);
        var mainDir = Path.GetDirectoryName(mainFull) ?? "";

        if (!File.Exists(mainFull))
            return null;

        foreach (var line in File.ReadAllLines(mainFull))
        {
            var kv = DnsmasqConfDirectiveParser.TryParseKeyValue(line);
            if (kv == null)
                continue;
            var (key, value) = kv.Value;
            if (!key.Equals("conf-dir", StringComparison.OrdinalIgnoreCase))
                continue;
            var path = value.Split(',')[0].Trim();
            return Path.GetFullPath(Path.Combine(mainDir, path));
        }

        return null;
    }

    /// <summary>
    /// Returns true if any config file contains the given option as a flag (no value or empty value).
    /// Used for options like no-hosts, expand-hosts, bogus-priv (dnsmasq: once set, set).
    /// </summary>
    public static bool GetFlagFromConfigFiles(IReadOnlyList<string> configFilePathsInOrder, string optionKey)
    {
        var key = optionKey.Trim();
        if (string.IsNullOrEmpty(key))
            return false;
        foreach (var configPath in configFilePathsInOrder)
        {
            if (!File.Exists(configPath))
                continue;
            foreach (var line in File.ReadAllLines(configPath))
            {
                var kv = DnsmasqConfDirectiveParser.TryParseKeyValue(line);
                if (kv == null)
                    continue;
                var (k, v) = kv.Value;
                if (!k.Equals(key, StringComparison.OrdinalIgnoreCase))
                    continue;
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// Returns the last value for the given option key across config files, and the directory of the file
    /// that contained it (for resolving relative paths). Keys are matched case-insensitively.
    /// </summary>
    public static (string? Value, string? ConfigFileDir) GetLastValueFromConfigFiles(IReadOnlyList<string> configFilePathsInOrder, string optionKey)
    {
        var key = optionKey.Trim();
        if (string.IsNullOrEmpty(key))
            return (null, null);
        string? lastValue = null;
        string? lastDir = null;
        foreach (var configPath in configFilePathsInOrder)
        {
            if (!File.Exists(configPath))
                continue;
            var dir = Path.GetDirectoryName(configPath) ?? "";
            foreach (var line in File.ReadAllLines(configPath))
            {
                var kv = DnsmasqConfDirectiveParser.TryParseKeyValue(line);
                if (kv == null)
                    continue;
                var (k, value) = kv.Value;
                if (!k.Equals(key, StringComparison.OrdinalIgnoreCase))
                    continue;
                var trimmed = value.Trim();
                lastValue = trimmed;
                lastDir = dir;
            }
        }
        return (lastValue, lastDir);
    }

    /// <summary>
    /// Resolves a path value against the config file directory. If value is null/empty or already absolute, returns as-is (or null).
    /// </summary>
    public static string? ResolvePath(string? value, string? configFileDir)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;
        if (Path.IsPathRooted(value))
            return value;
        if (string.IsNullOrEmpty(configFileDir))
            return Path.GetFullPath(value);
        return Path.GetFullPath(Path.Combine(configFileDir, value));
    }

    /// <summary>
    /// Reads the given config files in order and returns the last <c>dhcp-leasefile=</c> or <c>dhcp-lease-file=</c> path.
    /// Both option names are the same in dnsmasq (last occurrence of either wins). Relative paths resolved against config file dir.
    /// </summary>
    public static string? GetDhcpLeaseFilePathFromConfigFiles(IReadOnlyList<string> configFilePathsInOrder)
    {
        string? result = null;
        foreach (var configPath in configFilePathsInOrder)
        {
            if (!File.Exists(configPath))
                continue;
            var dir = Path.GetDirectoryName(configPath) ?? "";
            foreach (var line in File.ReadAllLines(configPath))
            {
                var kv = DnsmasqConfDirectiveParser.TryParseKeyValue(line);
                if (kv == null)
                    continue;
                var (key, value) = kv.Value;
                if (!key.Equals("dhcp-leasefile", StringComparison.OrdinalIgnoreCase) &&
                    !key.Equals("dhcp-lease-file", StringComparison.OrdinalIgnoreCase))
                    continue;
                var path = value.Trim();
                if (!string.IsNullOrEmpty(path))
                    result = ResolvePath(path, dir) ?? result;
            }
        }
        return result;
    }

    /// <summary>
    /// Reads the given config files in order and returns true if <c>no-hosts</c> appears in any file.
    /// When true, dnsmasq does not read /etc/hosts; only addn-hosts= files are used (if any).
    /// </summary>
    public static bool GetNoHostsFromConfigFiles(IReadOnlyList<string> configFilePathsInOrder) =>
        GetFlagFromConfigFiles(configFilePathsInOrder, "no-hosts");

    /// <summary>
    /// Reads the given config files in order and returns all <c>addn-hosts=</c> paths (cumulative; dnsmasq loads each in order).
    /// Relative paths are resolved against the config file's directory. Used so the app can show which hosts files dnsmasq loads.
    /// </summary>
    public static IReadOnlyList<string> GetAddnHostsPathsFromConfigFiles(IReadOnlyList<string> configFilePathsInOrder)
    {
        var result = new List<string>();
        foreach (var configPath in configFilePathsInOrder)
        {
            if (!File.Exists(configPath))
                continue;
            var dir = Path.GetDirectoryName(configPath) ?? "";
            foreach (var line in File.ReadAllLines(configPath))
            {
                var kv = DnsmasqConfDirectiveParser.TryParseKeyValue(line);
                if (kv == null)
                    continue;
                var (key, value) = kv.Value;
                if (!key.Equals("addn-hosts", StringComparison.OrdinalIgnoreCase))
                    continue;
                var path = value.Trim();
                if (string.IsNullOrEmpty(path))
                    continue;
                result.Add(Path.GetFullPath(Path.Combine(dir, path)));
            }
        }
        return result;
    }
}
