using DnsmasqWebUI.Infrastructure.Helpers.Config;
using DnsmasqWebUI.Models.Dnsmasq;
using DnsmasqWebUI.Infrastructure.Parsers;

namespace DnsmasqWebUI.Tests;


/// <summary>Parser semantics: flags (option present/absent/with value), last-wins, case-sensitive, path order.</summary>
public class DnsmasqConfIncludeParserCoreBehaviourTests
{
    [Fact]
    public void GetFlagFromConfigFiles_OptionPresent_ReturnsTrue()
    {
        var dir = Path.Combine(Path.GetTempPath(), "dnsmasq-flag-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        try
        {
            var conf = Path.Combine(dir, "dnsmasq.conf");
            File.WriteAllText(conf, "expand-hosts\nno-resolv\n");
            var result = DnsmasqConfIncludeParser.GetFlagFromConfigFiles(new[] { conf }, DnsmasqConfKeys.ExpandHosts);
            Assert.True(result);
            Assert.True(DnsmasqConfIncludeParser.GetFlagFromConfigFiles(new[] { conf }, DnsmasqConfKeys.NoResolv));
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    [Fact]
    public void GetFlagFromConfigFiles_OptionAbsent_ReturnsFalse()
    {
        var dir = Path.Combine(Path.GetTempPath(), "dnsmasq-flag-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        try
        {
            var conf = Path.Combine(dir, "dnsmasq.conf");
            File.WriteAllText(conf, "port=53\n");
            var result = DnsmasqConfIncludeParser.GetFlagFromConfigFiles(new[] { conf }, DnsmasqConfKeys.NoHosts);
            Assert.False(result);
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    [Fact]
    public void GetLastValueFromConfigFiles_LastWins()
    {
        var dir = Path.Combine(Path.GetTempPath(), "dnsmasq-last-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        var f1 = Path.Combine(dir, "01.conf");
        var f2 = Path.Combine(dir, "02.conf");
        var firstVal = "100";
        var secondVal = "200";
        try
        {
            File.WriteAllText(f1, $"cache-size={firstVal}\n");
            File.WriteAllText(f2, $"cache-size={secondVal}\n");
            var (value, configDir) = DnsmasqConfIncludeParser.GetLastValueFromConfigFiles(new[] { f1, f2 }, DnsmasqConfKeys.CacheSize);
            Assert.Equal(secondVal, value);
            Assert.Equal(dir, configDir);
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    [Fact]
    public void GetLastValueFromConfigFiles_LogFacility_ReturnsValue()
    {
        var dir = Path.Combine(Path.GetTempPath(), "dnsmasq-logfac-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        try
        {
            var logPath = "/data/dnsmasq.log";
            var conf = Path.Combine(dir, "dnsmasq.conf");
            File.WriteAllText(conf, $"log-facility={logPath}\n");
            var (value, configDir) = DnsmasqConfIncludeParser.GetLastValueFromConfigFiles(new[] { conf }, DnsmasqConfKeys.LogFacility);
            Assert.Equal(logPath, value);
            Assert.Equal(dir, configDir);
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    [Fact]
    public void GetLastValueFromConfigFiles_LogFacility_LastWins()
    {
        var dir = Path.Combine(Path.GetTempPath(), "dnsmasq-logfac2-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        var f1 = Path.Combine(dir, "01.conf");
        var f2 = Path.Combine(dir, "02.conf");
        var firstLogPath = "/var/log/dnsmasq.log";
        var secondLogPath = "/data/dnsmasq.log";
        try
        {
            File.WriteAllText(f1, $"log-facility={firstLogPath}\n");
            File.WriteAllText(f2, $"log-facility={secondLogPath}\n");
            var (value, _) = DnsmasqConfIncludeParser.GetLastValueFromConfigFiles(new[] { f1, f2 }, DnsmasqConfKeys.LogFacility);
            Assert.Equal(secondLogPath, value);
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    [Fact]
    public void ResolvePath_Relative_ResolvesAgainstDir()
    {
        var dir = Path.GetTempPath();
        var subDirName = "sub";
        var fileName = "pid.pid";
        var relativePath = $"{subDirName}/{fileName}";
        var result = DnsmasqConfIncludeParser.ResolvePath(relativePath, dir);
        Assert.Equal(Path.GetFullPath(Path.Combine(dir, subDirName, fileName)), result);
    }

    [Fact]
    public void ResolvePath_Absolute_ReturnsAsIs()
    {
        var absPath = Path.Combine(Path.GetTempPath(), "absolute.pid");
        var result = DnsmasqConfIncludeParser.ResolvePath(absPath, "/some/dir");
        Assert.Equal(Path.GetFullPath(absPath), result);
    }

    // --- Tests matching dnsmasq option.c semantics (ARG_ONE = last wins, flags = no value allowed) ---

    /// <summary>dnsmasq option.c: opts[i].has_arg == 0 && arg -> "extraneous parameter"; flag with value is invalid, so we must not count it.</summary>
    [Fact]
    public void GetFlagFromConfigFiles_FlagWithValue_ReturnsFalse()
    {
        var dir = Path.Combine(Path.GetTempPath(), "dnsmasq-flag-val-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        try
        {
            var invalidValue = "1";
            var conf = Path.Combine(dir, "dnsmasq.conf");
            File.WriteAllText(conf, $"expand-hosts={invalidValue}\n");
            var result = DnsmasqConfIncludeParser.GetFlagFromConfigFiles(new[] { conf }, DnsmasqConfKeys.ExpandHosts);
            Assert.False(result);
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    /// <summary>dnsmasq option.c: strcmp(opts[i].name, start) == 0 — option names are case-sensitive; "Port" is bad option.</summary>
    [Fact]
    public void GetLastValueFromConfigFiles_CaseSensitive_PortWithCapitalP_ReturnsNull()
    {
        var dir = Path.Combine(Path.GetTempPath(), "dnsmasq-case-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        try
        {
            var wrongKey = "Port";
            var value = "53";
            var conf = Path.Combine(dir, "dnsmasq.conf");
            File.WriteAllText(conf, $"{wrongKey}={value}\n");
            var (parsedValue, _) = DnsmasqConfIncludeParser.GetLastValueFromConfigFiles(new[] { conf }, DnsmasqConfKeys.Port);
            Assert.Null(parsedValue);
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    /// <summary>Path list is [main, conf-file...]. We read each file entirely in that order, so last port is last occurrence in that stream (main lines then extra). Main has port=53, port=5353; extra has port=54 → last is 54.</summary>
    [Fact]
    public void GetLastValueFromConfigFiles_TwoFiles_LastOccurrenceInPathOrderWins()
    {
        var dir = Path.Combine(Path.GetTempPath(), "dnsmasq-two-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        var main = Path.Combine(dir, "dnsmasq.conf");
        var extra = Path.Combine(dir, "extra.conf");
        var port1 = "53";
        var port2 = "5353";
        var port3 = "54";
        try
        {
            File.WriteAllText(main, $"port={port1}\nconf-file=extra.conf\nport={port2}\n");
            File.WriteAllText(extra, $"port={port3}\n");
            var paths = DnsmasqConfIncludeParser.GetIncludedPaths(main);
            var (value, _) = DnsmasqConfIncludeParser.GetLastValueFromConfigFiles(paths, DnsmasqConfKeys.Port);
            Assert.Equal(port3, value);
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    // --- pathToLines overloads: same result as file-based when content is pre-read ---

    [Fact]
    public void GetFlagFromConfigFiles_WithPathToLines_ReturnsSameResultAsFileBased()
    {
        var dir = Path.Combine(Path.GetTempPath(), "dnsmasq-ptl-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        var conf = Path.Combine(dir, "dnsmasq.conf");
        var lines = new[] { "expand-hosts", "no-resolv" };
        try
        {
            File.WriteAllText(conf, string.Join("\n", lines) + "\n");
            var paths = new[] { conf };
            var pathToLines = new Dictionary<string, string[]>(StringComparer.Ordinal)
            {
                [Path.GetFullPath(conf)] = lines
            };
            var fromFile = DnsmasqConfIncludeParser.GetFlagFromConfigFiles(paths, DnsmasqConfKeys.ExpandHosts);
            var fromDict = DnsmasqConfIncludeParser.GetFlagFromConfigFiles(paths, pathToLines, DnsmasqConfKeys.ExpandHosts);
            Assert.Equal(fromFile, fromDict);
            Assert.True(fromDict);
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    [Fact]
    public void GetLastValueFromConfigFiles_WithPathToLines_ReturnsSameResultAsFileBased()
    {
        var dir = Path.Combine(Path.GetTempPath(), "dnsmasq-ptl-last-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        var conf = Path.Combine(dir, "dnsmasq.conf");
        var value = "1000";
        var lines = new[] { $"cache-size={value}" };
        try
        {
            File.WriteAllText(conf, lines[0] + "\n");
            var paths = new[] { conf };
            var pathToLines = new Dictionary<string, string[]>(StringComparer.Ordinal)
            {
                [Path.GetFullPath(conf)] = lines
            };
            var (fromFileVal, fromFileDir) = DnsmasqConfIncludeParser.GetLastValueFromConfigFiles(paths, DnsmasqConfKeys.CacheSize);
            var (fromDictVal, fromDictDir) = DnsmasqConfIncludeParser.GetLastValueFromConfigFiles(paths, pathToLines, DnsmasqConfKeys.CacheSize);
            Assert.Equal(fromFileVal, fromDictVal);
            Assert.Equal(value, fromDictVal);
            Assert.Equal(fromFileDir, fromDictDir);
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    [Fact]
    public void GetMultiValueFromConfigFiles_WithPathToLines_ReturnsSameResultAsFileBased()
    {
        var dir = Path.Combine(Path.GetTempPath(), "dnsmasq-ptl-multi-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        var conf = Path.Combine(dir, "dnsmasq.conf");
        var lines = new[] { "server=1.1.1.1", "server=8.8.8.8" };
        try
        {
            File.WriteAllText(conf, string.Join("\n", lines) + "\n");
            var paths = new[] { conf };
            var pathToLines = new Dictionary<string, string[]>(StringComparer.Ordinal)
            {
                [Path.GetFullPath(conf)] = lines
            };
            var fromFile = DnsmasqConfIncludeParser.GetMultiValueFromConfigFiles(paths, DnsmasqConfKeys.Server);
            var fromDict = DnsmasqConfIncludeParser.GetMultiValueFromConfigFiles(paths, pathToLines, DnsmasqConfKeys.Server);
            Assert.Equal(fromFile, fromDict);
            Assert.Equal(2, fromDict.Count);
            Assert.Equal("1.1.1.1", fromDict[0]);
            Assert.Equal("8.8.8.8", fromDict[1]);
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    // --- Empty path list ---

    [Fact]
    public void GetLastValueFromConfigFiles_EmptyPathList_ReturnsNullAndNull()
    {
        var (value, configDir) = DnsmasqConfIncludeParser.GetLastValueFromConfigFiles(Array.Empty<string>(), DnsmasqConfKeys.Port);
        Assert.Null(value);
        Assert.Null(configDir);
    }

    [Fact]
    public void GetMultiValueFromConfigFiles_EmptyPathList_ReturnsEmptyList()
    {
        var result = DnsmasqConfIncludeParser.GetMultiValueFromConfigFiles(Array.Empty<string>(), DnsmasqConfKeys.Server);
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    // --- Non-existent file in path list: skipped without throw, last-wins from existing ---

    [Fact]
    public void GetLastValueFromConfigFiles_NonExistentFileInList_SkipsMissingFileLastWinsFromExisting()
    {
        var dir = Path.Combine(Path.GetTempPath(), "dnsmasq-missing-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        var existing = Path.Combine(dir, "existing.conf");
        var missing = Path.Combine(dir, "missing.conf");
        var other = Path.Combine(dir, "other.conf");
        var valueInExisting = "100";
        var valueInOther = "200";
        try
        {
            File.WriteAllText(existing, $"port={valueInExisting}\n");
            File.WriteAllText(other, $"port={valueInOther}\n");
            Assert.False(File.Exists(missing));
            var paths = new[] { existing, missing, other };
            var (value, _) = DnsmasqConfIncludeParser.GetLastValueFromConfigFiles(paths, DnsmasqConfKeys.Port);
            Assert.Equal(valueInOther, value);
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

}
