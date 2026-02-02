using DnsmasqWebUI.Models.EffectiveConfig;
using Superpower;
using Superpower.Model;
using Superpower.Parsers;

namespace DnsmasqWebUI.Parsers;

/// <summary>
/// Parses the main dnsmasq .conf file only for <c>conf-file=</c> and <c>conf-dir=</c> directives
/// to discover the ordered list of included config file paths. Used for config set discovery.
/// File order and option semantics match dnsmasq source: main file line-by-line with conf-file/conf-dir
/// processed in place (interleaved). Option names matched case-sensitively per dnsmasq.
/// See: https://thekelleys.org.uk/dnsmasq/docs/dnsmasq-man.html
/// </summary>
public static class DnsmasqConfIncludeParser
{
    private static readonly StringComparison KeyComparison = StringComparison.Ordinal;

    /// <summary>
    /// Returns the ordered list of absolute config file paths dnsmasq loads, in the exact order
    /// dnsmasq reads them (main interleaved with conf-file and conf-dir).
    /// </summary>
    public static IReadOnlyList<string> GetIncludedPaths(string mainConfigPath)
    {
        return GetIncludedPathsWithSource(mainConfigPath)
            .Select(x => x.Path)
            .ToList();
    }

    /// <summary>
    /// Returns the ordered list of (path, source) for config set display, in the exact order
    /// dnsmasq reads files (main, then conf-file/conf-dir as encountered line-by-line).
    /// </summary>
    public static IReadOnlyList<(string Path, DnsmasqConfFileSource Source)> GetIncludedPathsWithSource(string mainConfigPath)
    {
        var mainFull = Path.GetFullPath(mainConfigPath);
        if (!File.Exists(mainFull))
            return new List<(string Path, DnsmasqConfFileSource Source)> { (mainFull, DnsmasqConfFileSource.Main) };

        var result = new List<(string Path, DnsmasqConfFileSource Source)>();
        var added = new HashSet<string>(StringComparer.Ordinal);
        var seenPaths = new HashSet<string>(StringComparer.Ordinal);
        foreach (var (filePath, _, _, source) in EnumerateConfigLinesInDnsmasqOrder(mainFull, Path.GetDirectoryName(mainFull) ?? "", DnsmasqConfFileSource.Main, seenPaths))
        {
            if (added.Add(filePath))
                result.Add((filePath, source));
        }

        return result;
    }

    /// <summary>
    /// Yields (filePath, lineNumber, line, source) in the exact order dnsmasq reads config.
    /// When conf-file=X or conf-dir=Y is encountered, that file(s) are yielded before continuing the current file.
    /// </summary>
    public static IEnumerable<(string FilePath, int LineNumber, string Line, DnsmasqConfFileSource Source)> EnumerateConfigLinesInDnsmasqOrder(
        string currentFilePath,
        string currentFileDir,
        DnsmasqConfFileSource source,
        HashSet<string>? seenPaths = null)
    {
        seenPaths ??= new HashSet<string>(StringComparer.Ordinal);
        if (!File.Exists(currentFilePath))
            yield break;
        var canonicalPath = Path.GetFullPath(currentFilePath);
        if (!seenPaths.Add(canonicalPath))
            yield break; // dnsmasq skips re-reading same file (by inode; we use path)

        var lines = File.ReadAllLines(currentFilePath);
        var dir = Path.GetDirectoryName(canonicalPath) ?? "";

        // Yield "file started" so callers (e.g. GetIncludedPathsWithSource) can add this file even if it only has conf-file/conf-dir lines.
        yield return (canonicalPath, 0, "", source);

        for (var i = 0; i < lines.Length; i++)
        {
            var line = lines[i];
            var kv = DnsmasqConfDirectiveParser.TryParseKeyValue(line);
            if (kv == null)
                continue;
            var (key, value) = kv.Value;

            if (string.Equals(key, "conf-file", KeyComparison))
            {
                var path = value.Trim();
                if (string.IsNullOrEmpty(path)) continue;
                var resolved = ResolvePath(path, dir) ?? path;
                foreach (var t in EnumerateConfigLinesInDnsmasqOrder(resolved, Path.GetDirectoryName(resolved) ?? "", DnsmasqConfFileSource.ConfFile, seenPaths))
                    yield return t;
                continue;
            }

            if (string.Equals(key, "conf-dir", KeyComparison))
            {
                var (directory, matchSuffix, ignoreSuffix) = ParseConfDirValue(value);
                if (string.IsNullOrEmpty(directory)) continue;
                var resolvedDir = ResolvePath(directory, dir) ?? directory;
                if (!Directory.Exists(resolvedDir)) continue;
                var files = GetConfDirFilesSorted(resolvedDir, matchSuffix, ignoreSuffix);
                foreach (var f in files)
                {
                    foreach (var t in EnumerateConfigLinesInDnsmasqOrder(f, Path.GetDirectoryName(f) ?? "", DnsmasqConfFileSource.ConfDir, seenPaths))
                        yield return t;
                }
                continue;
            }

            yield return (canonicalPath, i + 1, line, source);
        }
    }

    // conf-dir value: comma-delimited fields (dir, optional *suffix, optional ignore suffixes). Superpower grammar.
    private static readonly TextParser<string> ConfDirField =
        Character.Matching(c => c != ',' && c != '\r' && c != '\n', "field character").AtLeastOnce().Text()
            .Then(s => Character.WhiteSpace.Many().IgnoreThen(Parse.Return(s.Trim())));

    private static readonly TextParser<List<string>> ConfDirValueParser =
        ConfDirField.AtLeastOnceDelimitedBy(ConfParserHelpers.Token(Character.EqualTo(',')))
            .AtEnd()
            .Select(list => list.ToList())
            .Named("conf-dir value");

    private static (string? directory, List<string>? matchSuffix, List<string>? ignoreSuffix) ParseConfDirValue(string value)
    {
        var parsed = ConfDirValueParser.TryParse(value.Trim());
        if (!parsed.HasValue || parsed.Value.Count == 0)
            return (null, null, null);
        var parts = parsed.Value;
        var dir = parts[0];
        var matchSuffix = new List<string>();
        var ignoreSuffix = new List<string>();
        for (var i = 1; i < parts.Count; i++)
        {
            var p = parts[i];
            if (p.StartsWith('*'))
                matchSuffix.Add(p.Length > 1 ? p[1..] : "");
            else
                ignoreSuffix.Add(p);
        }
        return (dir, matchSuffix.Count > 0 ? matchSuffix : null, ignoreSuffix.Count > 0 ? ignoreSuffix : null);
    }

    private static bool ConfDirFileFilter(string name)
    {
        if (string.IsNullOrEmpty(name)) return false;
        if (name[^1] == '~') return false;
        if (name.Length >= 2 && name[0] == '#' && name[^1] == '#') return false;
        if (name[0] == '.') return false;
        return true;
    }

    private static IReadOnlyList<string> GetConfDirFilesSorted(string directory, List<string>? matchSuffix, List<string>? ignoreSuffix)
    {
        var files = new List<string>();
        foreach (var name in Directory.GetFiles(directory).Select(Path.GetFileName).Where(n => n != null).Cast<string>())
        {
            if (!ConfDirFileFilter(name)) continue;
            var fullPath = Path.GetFullPath(Path.Combine(directory, name));
            if ((File.GetAttributes(fullPath) & FileAttributes.Directory) != 0) continue;

            if (matchSuffix != null && matchSuffix.Count > 0)
            {
                var hasMatch = matchSuffix.Any(s => s.Length > 0 && name.EndsWith(s, StringComparison.Ordinal));
                if (!hasMatch) continue;
            }
            if (ignoreSuffix != null && ignoreSuffix.Count > 0)
            {
                var ignored = ignoreSuffix.Any(s => name.EndsWith(s, StringComparison.Ordinal));
                if (ignored) continue;
            }
            files.Add(fullPath);
        }
        files.Sort(StringComparer.Ordinal);
        return files;
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
            if (!string.Equals(key, "conf-dir", KeyComparison))
                continue;
            var path = value.Split(',')[0].Trim();
            return Path.GetFullPath(Path.Combine(mainDir, path));
        }

        return null;
    }

    /// <summary>
    /// Returns true if any config file contains the given option as a flag (key-only line, no value).
    /// dnsmasq: flag options (has_arg==0) must have no '='; extraneous parameter otherwise.
    /// Keys matched case-sensitively per dnsmasq.
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
                if (!string.Equals(k, key, KeyComparison))
                    continue;
                if (!string.IsNullOrEmpty(v?.Trim()))
                    continue; // dnsmasq rejects flag with value
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// Returns the last value for the given option key across config files, and the directory of the file
    /// that contained it (for resolving relative paths). Keys matched case-sensitively per dnsmasq.
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
                if (!string.Equals(k, key, KeyComparison))
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
    /// Reads the given config files in order and returns the last <c>dhcp-leasefile=</c> or <c>dhcp-lease=</c> path.
    /// dnsmasq only accepts these two option names (no dhcp-lease-file). Case-sensitive. Relative paths resolved against config file dir.
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
                if (!string.Equals(key, "dhcp-leasefile", KeyComparison) && !string.Equals(key, "dhcp-lease", KeyComparison))
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
                if (!string.Equals(key, "addn-hosts", KeyComparison))
                    continue;
                var path = value.Trim();
                if (string.IsNullOrEmpty(path))
                    continue;
                result.Add(Path.GetFullPath(Path.Combine(dir, path)));
            }
        }
        return result;
    }

    private static ConfigValueSource MakeSource(string configPath, string? managedFilePath) =>
        new(configPath, Path.GetFileName(configPath), string.Equals(Path.GetFullPath(configPath), managedFilePath != null ? Path.GetFullPath(managedFilePath) : null, StringComparison.Ordinal));

    /// <summary>Like <see cref="GetLastValueFromConfigFiles"/> but returns which file set the value (for readonly/editable UI).</summary>
    public static (string? Value, ConfigValueSource? Source) GetLastValueFromConfigFilesWithSource(
        IReadOnlyList<string> configFilePathsInOrder, string optionKey, string? managedFilePath)
    {
        var key = optionKey.Trim();
        if (string.IsNullOrEmpty(key))
            return (null, null);
        string? lastValue = null;
        ConfigValueSource? lastSource = null;
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
                if (!string.Equals(k, key, KeyComparison))
                    continue;
                var trimmed = value.Trim();
                lastValue = trimmed;
                lastSource = MakeSource(configPath, managedFilePath);
            }
        }
        return (lastValue, lastSource);
    }

    /// <summary>Like <see cref="GetFlagFromConfigFiles"/> but returns which file set the flag (for readonly: if not managed, user cannot unset from UI).</summary>
    public static (bool IsSet, ConfigValueSource? Source) GetFlagFromConfigFilesWithSource(
        IReadOnlyList<string> configFilePathsInOrder, string optionKey, string? managedFilePath)
    {
        var key = optionKey.Trim();
        if (string.IsNullOrEmpty(key))
            return (false, null);
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
                if (!string.Equals(k, key, KeyComparison))
                    continue;
                if (!string.IsNullOrEmpty(v?.Trim()))
                    continue;
                return (true, MakeSource(configPath, managedFilePath));
            }
        }
        return (false, null);
    }

    /// <summary>Like <see cref="GetDhcpLeaseFilePathFromConfigFiles"/> but returns which file set the value.</summary>
    public static (string? Path, ConfigValueSource? Source) GetDhcpLeaseFilePathFromConfigFilesWithSource(
        IReadOnlyList<string> configFilePathsInOrder, string? managedFilePath)
    {
        string? result = null;
        ConfigValueSource? lastSource = null;
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
                if (!string.Equals(key, "dhcp-leasefile", KeyComparison) && !string.Equals(key, "dhcp-lease", KeyComparison))
                    continue;
                var path = value.Trim();
                if (!string.IsNullOrEmpty(path))
                {
                    result = ResolvePath(path, dir) ?? result;
                    lastSource = MakeSource(configPath, managedFilePath);
                }
            }
        }
        return (result, lastSource);
    }

    /// <summary>Like <see cref="GetAddnHostsPathsFromConfigFiles"/> but returns source file for each path (for multi-value; each entry can be readonly or from managed).</summary>
    public static IReadOnlyList<(string Path, ConfigValueSource Source)> GetAddnHostsPathsFromConfigFilesWithSource(
        IReadOnlyList<string> configFilePathsInOrder, string? managedFilePath)
    {
        var result = new List<(string Path, ConfigValueSource Source)>();
        foreach (var configPath in configFilePathsInOrder)
        {
            if (!File.Exists(configPath))
                continue;
            var dir = Path.GetDirectoryName(configPath) ?? "";
            var source = MakeSource(configPath, managedFilePath);
            foreach (var line in File.ReadAllLines(configPath))
            {
                var kv = DnsmasqConfDirectiveParser.TryParseKeyValue(line);
                if (kv == null)
                    continue;
                var (key, value) = kv.Value;
                if (!string.Equals(key, "addn-hosts", KeyComparison))
                    continue;
                var path = value.Trim();
                if (string.IsNullOrEmpty(path))
                    continue;
                result.Add((Path.GetFullPath(Path.Combine(dir, path)), source));
            }
        }
        return result;
    }

    /// <summary>Collects all values for a single option key across config files (ARG_DUP style; order preserved).</summary>
    public static IReadOnlyList<string> GetMultiValueFromConfigFiles(
        IReadOnlyList<string> configFilePathsInOrder, string optionKey)
    {
        var result = new List<string>();
        var key = optionKey.Trim();
        if (string.IsNullOrEmpty(key))
            return result;
        foreach (var configPath in configFilePathsInOrder)
        {
            if (!File.Exists(configPath))
                continue;
            foreach (var line in File.ReadAllLines(configPath))
            {
                var kv = DnsmasqConfDirectiveParser.TryParseKeyValue(line);
                if (kv == null)
                    continue;
                var (k, value) = kv.Value;
                if (!string.Equals(k, key, KeyComparison))
                    continue;
                result.Add(value.Trim());
            }
        }
        return result;
    }

    /// <summary>Collects all values for multiple option keys (e.g. server and local) in file order. Keys matched case-sensitively.</summary>
    public static IReadOnlyList<string> GetMultiValueFromConfigFiles(
        IReadOnlyList<string> configFilePathsInOrder, IReadOnlyList<string> optionKeys)
    {
        var keys = new HashSet<string>(optionKeys.Select(k => k.Trim()).Where(k => k.Length > 0), StringComparer.Ordinal);
        if (keys.Count == 0)
            return Array.Empty<string>();
        var result = new List<string>();
        foreach (var configPath in configFilePathsInOrder)
        {
            if (!File.Exists(configPath))
                continue;
            foreach (var line in File.ReadAllLines(configPath))
            {
                var kv = DnsmasqConfDirectiveParser.TryParseKeyValue(line);
                if (kv == null)
                    continue;
                var (k, value) = kv.Value;
                if (!keys.Contains(k))
                    continue;
                result.Add(value.Trim());
            }
        }
        return result;
    }

    /// <summary>Like <see cref="GetMultiValueFromConfigFiles(IReadOnlyList{string}, string)"/> but returns source per value.</summary>
    public static IReadOnlyList<(string Value, ConfigValueSource Source)> GetMultiValueFromConfigFilesWithSource(
        IReadOnlyList<string> configFilePathsInOrder, string optionKey, string? managedFilePath)
    {
        var result = new List<(string Value, ConfigValueSource Source)>();
        var key = optionKey.Trim();
        if (string.IsNullOrEmpty(key))
            return result;
        foreach (var configPath in configFilePathsInOrder)
        {
            if (!File.Exists(configPath))
                continue;
            var source = MakeSource(configPath, managedFilePath);
            foreach (var line in File.ReadAllLines(configPath))
            {
                var kv = DnsmasqConfDirectiveParser.TryParseKeyValue(line);
                if (kv == null)
                    continue;
                var (k, value) = kv.Value;
                if (!string.Equals(k, key, KeyComparison))
                    continue;
                result.Add((value.Trim(), source));
            }
        }
        return result;
    }

    /// <summary>Like <see cref="GetMultiValueFromConfigFiles(IReadOnlyList{string}, IReadOnlyList{string})"/> but returns source per value.</summary>
    public static IReadOnlyList<(string Value, ConfigValueSource Source)> GetMultiValueFromConfigFilesWithSource(
        IReadOnlyList<string> configFilePathsInOrder, IReadOnlyList<string> optionKeys, string? managedFilePath)
    {
        var keys = new HashSet<string>(optionKeys.Select(k => k.Trim()).Where(k => k.Length > 0), StringComparer.Ordinal);
        if (keys.Count == 0)
            return Array.Empty<(string, ConfigValueSource)>();
        var result = new List<(string Value, ConfigValueSource Source)>();
        foreach (var configPath in configFilePathsInOrder)
        {
            if (!File.Exists(configPath))
                continue;
            var source = MakeSource(configPath, managedFilePath);
            foreach (var line in File.ReadAllLines(configPath))
            {
                var kv = DnsmasqConfDirectiveParser.TryParseKeyValue(line);
                if (kv == null)
                    continue;
                var (k, value) = kv.Value;
                if (!keys.Contains(k))
                    continue;
                result.Add((value.Trim(), source));
            }
        }
        return result;
    }
}
