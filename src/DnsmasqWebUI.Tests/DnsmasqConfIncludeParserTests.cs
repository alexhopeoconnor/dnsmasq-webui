using DnsmasqWebUI.Infrastructure.Helpers.Config;
using DnsmasqWebUI.Models.Dnsmasq;
using DnsmasqWebUI.Models.Dnsmasq.EffectiveConfig;
using DnsmasqWebUI.Infrastructure.Parsers;

namespace DnsmasqWebUI.Tests;

/// <summary>
/// Tests for DnsmasqConfIncludeParser. Discovers conf-file= and conf-dir= from main dnsmasq config
/// to build ordered list of included file paths and first conf-dir for managed file.
/// </summary>
public class DnsmasqConfIncludeParserTests
{
    [Fact]
    public void GetIncludedPaths_MainFileMissing_ReturnsOnlyMainPath()
    {
        var main = Path.Combine(Path.GetTempPath(), "nonexistent-dnsmasq-" + Guid.NewGuid().ToString("N") + ".conf");
        var paths = DnsmasqConfIncludeParser.GetIncludedPaths(main);
        Assert.Single(paths);
        Assert.Equal(Path.GetFullPath(main), paths[0]);
    }

    [Fact]
    public void GetIncludedPaths_MainOnly_ReturnsSinglePath()
    {
        var dir = Path.Combine(Path.GetTempPath(), "dnsmasq-main-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        try
        {
            var main = Path.Combine(dir, "dnsmasq.conf");
            File.WriteAllText(main, "domain=local\n");
            var paths = DnsmasqConfIncludeParser.GetIncludedPaths(main);
            Assert.Single(paths);
            Assert.Equal(Path.GetFullPath(main), paths[0]);
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    [Fact]
    public void GetIncludedPaths_OneConfFile_ReturnsMainThenConfFile()
    {
        var dir = Path.Combine(Path.GetTempPath(), "dnsmasq-conffile-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        try
        {
            var includeFileName = "extra.conf";
            var includePath = Path.Combine(dir, includeFileName);
            File.WriteAllText(includePath, "# extra\n");
            var main = Path.Combine(dir, "dnsmasq.conf");
            File.WriteAllText(main, $"conf-file={includeFileName}\n");
            var paths = DnsmasqConfIncludeParser.GetIncludedPaths(main);
            Assert.Equal(2, paths.Count);
            Assert.Equal(Path.GetFullPath(main), paths[0]);
            Assert.Equal(Path.GetFullPath(includePath), paths[1]);
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    [Fact]
    public void GetIncludedPaths_OneConfDir_ReturnsMainThenDirFilesSorted()
    {
        var baseDir = Path.Combine(Path.GetTempPath(), "dnsmasq-confdir-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(baseDir);
        var confDirName = "d";
        var subDir = Path.Combine(baseDir, confDirName);
        Directory.CreateDirectory(subDir);
        var file1 = "aa.conf";
        var file2 = "zz.conf";
        try
        {
            File.WriteAllText(Path.Combine(subDir, file2), "");
            File.WriteAllText(Path.Combine(subDir, file1), "");
            var main = Path.Combine(baseDir, "dnsmasq.conf");
            File.WriteAllText(main, $"conf-dir={confDirName}\n");
            var paths = DnsmasqConfIncludeParser.GetIncludedPaths(main);
            Assert.Equal(3, paths.Count); // main + aa.conf + zz.conf (alphabetical)
            Assert.Equal(Path.GetFullPath(main), paths[0]);
            Assert.Equal(Path.GetFullPath(Path.Combine(subDir, file1)), paths[1]);
            Assert.Equal(Path.GetFullPath(Path.Combine(subDir, file2)), paths[2]);
        }
        finally
        {
            Directory.Delete(baseDir, recursive: true);
        }
    }

    [Fact]
    public void GetFirstConfDir_NoConfDir_ReturnsNull()
    {
        var dir = Path.Combine(Path.GetTempPath(), "dnsmasq-noconfdir-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        try
        {
            var main = Path.Combine(dir, "dnsmasq.conf");
            File.WriteAllText(main, "domain=local\nconf-file=other.conf\n");
            var first = DnsmasqConfIncludeParser.GetFirstConfDir(main);
            Assert.Null(first);
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    [Fact]
    public void GetFirstConfDir_HasConfDir_ReturnsResolvedPath()
    {
        var baseDir = Path.Combine(Path.GetTempPath(), "dnsmasq-firstdir-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(baseDir);
        var confDirName = "dnsmasq.d";
        var subDir = Path.Combine(baseDir, confDirName);
        Directory.CreateDirectory(subDir);
        try
        {
            var main = Path.Combine(baseDir, "dnsmasq.conf");
            File.WriteAllText(main, $"conf-dir={confDirName}\n");
            var first = DnsmasqConfIncludeParser.GetFirstConfDir(main);
            Assert.NotNull(first);
            Assert.Equal(Path.GetFullPath(subDir), first);
        }
        finally
        {
            Directory.Delete(baseDir, recursive: true);
        }
    }

    [Fact]
    public void GetIncludedPaths_CommentsAndBlanks_Ignored()
    {
        var dir = Path.Combine(Path.GetTempPath(), "dnsmasq-comments-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        try
        {
            var main = Path.Combine(dir, "dnsmasq.conf");
            File.WriteAllText(main, "# comment\n\n  \nconf-file=extra.conf\n");
            var extra = Path.Combine(dir, "extra.conf");
            File.WriteAllText(extra, "");
            var paths = DnsmasqConfIncludeParser.GetIncludedPaths(main);
            Assert.Equal(2, paths.Count);
            Assert.Equal(Path.GetFullPath(extra), paths[1]);
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    [Fact]
    public void GetIncludedPaths_ConfDirMissing_ReturnsMainOnly()
    {
        var baseDir = Path.Combine(Path.GetTempPath(), "dnsmasq-confdir-missing-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(baseDir);
        try
        {
            var main = Path.Combine(baseDir, "dnsmasq.conf");
            File.WriteAllText(main, "conf-dir=nonexistent-subdir\n");
            var paths = DnsmasqConfIncludeParser.GetIncludedPaths(main);
            Assert.Single(paths);
            Assert.Equal(Path.GetFullPath(main), paths[0]);
        }
        finally
        {
            Directory.Delete(baseDir, recursive: true);
        }
    }

    /// <summary>Mirrors harness layout: main + conf-dir with multiple files (including lan-dns.conf) + conf-file. Asserts all appear in load order with correct sources.</summary>
    [Fact]
    public void GetIncludedPathsWithSource_ConfDirAndConfFile_ReturnsAllFilesInLoadOrderWithSources()
    {
        var baseDir = Path.Combine(Path.GetTempPath(), "dnsmasq-harness-layout-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(baseDir);
        var confDir = Path.Combine(baseDir, "dnsmasq.d");
        Directory.CreateDirectory(confDir);
        try
        {
            File.WriteAllText(Path.Combine(confDir, "01-other.conf"), "# other\n");
            File.WriteAllText(Path.Combine(confDir, "02-servers.conf"), "# servers\n");
            File.WriteAllText(Path.Combine(confDir, "lan-dns.conf"), "# lan dns\n");
            File.WriteAllText(Path.Combine(baseDir, "zz-dnsmasq-webui.conf"), "# managed\n");
            var main = Path.Combine(baseDir, "dnsmasq.conf");
            File.WriteAllText(main, "conf-dir=dnsmasq.d\nconf-file=zz-dnsmasq-webui.conf\n");
            var withSource = DnsmasqConfIncludeParser.GetIncludedPathsWithSource(main);
            Assert.Equal(5, withSource.Count);
            Assert.Equal(Path.GetFullPath(main), withSource[0].Path);
            Assert.Equal(DnsmasqConfFileSource.Main, withSource[0].Source);
            Assert.Equal(Path.GetFullPath(Path.Combine(confDir, "01-other.conf")), withSource[1].Path);
            Assert.Equal(DnsmasqConfFileSource.ConfDir, withSource[1].Source);
            Assert.Equal(Path.GetFullPath(Path.Combine(confDir, "02-servers.conf")), withSource[2].Path);
            Assert.Equal(DnsmasqConfFileSource.ConfDir, withSource[2].Source);
            Assert.Equal(Path.GetFullPath(Path.Combine(confDir, "lan-dns.conf")), withSource[3].Path);
            Assert.Equal(DnsmasqConfFileSource.ConfDir, withSource[3].Source);
            Assert.Equal(Path.GetFullPath(Path.Combine(baseDir, "zz-dnsmasq-webui.conf")), withSource[4].Path);
            Assert.Equal(DnsmasqConfFileSource.ConfFile, withSource[4].Source);
        }
        finally
        {
            Directory.Delete(baseDir, recursive: true);
        }
    }

    [Fact]
    public void GetDhcpLeaseFilePathFromConfigFiles_FromTestdata_ReturnsLeasesPath()
    {
        var mainPath = TestDataHelper.GetPath("dnsmasq-test.conf");
        if (!File.Exists(mainPath))
            return;
        var paths = new[] { mainPath };
        var result = DnsmasqConfIncludeParser.GetDhcpLeaseFilePathFromConfigFiles(paths);
        Assert.NotNull(result);
        Assert.Equal(Path.GetFullPath("/data/leases"), result);
    }

    /// <summary>Uses testdata/dnsmasq-test.conf (harness main) to assert multi-value options are collected from testdata.</summary>
    [Fact]
    public void GetMultiValueFromConfigFiles_FromTestdata_ReturnsServerAndAddnHosts()
    {
        var mainPath = TestDataHelper.GetPath("dnsmasq-test.conf");
        if (!File.Exists(mainPath))
            return;
        var paths = new[] { mainPath };
        var servers = DnsmasqConfIncludeParser.GetMultiValueFromConfigFiles(paths, DnsmasqConfKeys.ServerLocalKeys);
        var addnHosts = DnsmasqConfIncludeParser.GetAddnHostsPathsFromConfigFiles(paths);
        var addresses = DnsmasqConfIncludeParser.GetMultiValueFromConfigFiles(paths, DnsmasqConfKeys.Address);
        var listenAddrs = DnsmasqConfIncludeParser.GetMultiValueFromConfigFiles(paths, DnsmasqConfKeys.ListenAddress);
        Assert.True(servers.Count >= 2, "testdata dnsmasq-test.conf should have at least 2 server= lines");
        Assert.Contains("1.1.1.1", servers);
        Assert.Contains("8.8.8.8", servers);
        Assert.True(addnHosts.Count >= 2, "testdata should have at least 2 addn-hosts paths");
        Assert.True(addresses.Count >= 2, "testdata should have at least 2 address= lines");
        Assert.Single(listenAddrs);
        Assert.Equal("172.28.0.2", listenAddrs[0]);
    }

    [Fact]
    public void GetDhcpLeaseFilePathFromConfigFiles_LastWins()
    {
        var dir = Path.Combine(Path.GetTempPath(), "dnsmasq-lease-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        try
        {
            var firstLeasePath = "/var/first.leases";
            var secondLeasePath = "/var/second.leases";
            var first = Path.Combine(dir, "first.conf");
            File.WriteAllText(first, $"dhcp-leasefile={firstLeasePath}\n");
            var second = Path.Combine(dir, "second.conf");
            File.WriteAllText(second, $"dhcp-leasefile={secondLeasePath}\n");
            var result = DnsmasqConfIncludeParser.GetDhcpLeaseFilePathFromConfigFiles(new[] { first, second });
            Assert.Equal(Path.GetFullPath(secondLeasePath), result);
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    [Fact]
    public void GetDhcpLeaseFilePathFromConfigFiles_RelativePath_ResolvedAgainstConfigDir()
    {
        var dir = Path.Combine(Path.GetTempPath(), "dnsmasq-lease-rel-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        try
        {
            var subdir = "subdir";
            var leasesFile = "leases";
            var relativePath = $"{subdir}/{leasesFile}";
            var conf = Path.Combine(dir, "dnsmasq.conf");
            File.WriteAllText(conf, $"dhcp-leasefile={relativePath}\n");
            var result = DnsmasqConfIncludeParser.GetDhcpLeaseFilePathFromConfigFiles(new[] { conf });
            Assert.Equal(Path.GetFullPath(Path.Combine(dir, subdir, leasesFile)), result);
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    [Fact]
    public void GetAddnHostsPathsFromConfigFiles_Empty_ReturnsEmpty()
    {
        var result = DnsmasqConfIncludeParser.GetAddnHostsPathsFromConfigFiles(Array.Empty<string>());
        Assert.Empty(result);
    }

    [Fact]
    public void GetAddnHostsPathsFromConfigFiles_Cumulative_ReturnsAllInOrder()
    {
        var dir = Path.Combine(Path.GetTempPath(), "dnsmasq-addn-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        try
        {
            var path1 = "/etc/hosts.d/first";
            var path2 = "/data/hosts";
            var path3 = "/data/extra.hosts";
            var first = Path.Combine(dir, "first.conf");
            File.WriteAllText(first, $"addn-hosts={path1}\n");
            var second = Path.Combine(dir, "second.conf");
            File.WriteAllText(second, $"addn-hosts={path2}\naddn-hosts={path3}\n");
            var result = DnsmasqConfIncludeParser.GetAddnHostsPathsFromConfigFiles(new[] { first, second });
            Assert.Equal(3, result.Count);
            Assert.Equal(Path.GetFullPath(path1), result[0]);
            Assert.Equal(Path.GetFullPath(path2), result[1]);
            Assert.Equal(Path.GetFullPath(path3), result[2]);
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    [Fact]
    public void GetAddnHostsPathsFromConfigFiles_RelativePath_ResolvedAgainstConfigDir()
    {
        var dir = Path.Combine(Path.GetTempPath(), "dnsmasq-addn-rel-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        try
        {
            var pathPart1 = "hosts.d";
            var pathPart2 = "app.hosts";
            var addnRelativePath = $"{pathPart1}/{pathPart2}";
            var conf = Path.Combine(dir, "dnsmasq.conf");
            File.WriteAllText(conf, $"addn-hosts={addnRelativePath}\n");
            var result = DnsmasqConfIncludeParser.GetAddnHostsPathsFromConfigFiles(new[] { conf });
            Assert.Single(result);
            Assert.Equal(Path.GetFullPath(Path.Combine(dir, pathPart1, pathPart2)), result[0]);
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

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

    /// <summary>dnsmasq conf-dir=dir,*.suffix only includes files ending with suffix (option.c match_suffix).</summary>
    [Fact]
    public void GetIncludedPaths_ConfDir_WithMatchSuffix_OnlyIncludesMatchingFiles()
    {
        var baseDir = Path.Combine(Path.GetTempPath(), "dnsmasq-match-" + Guid.NewGuid().ToString("N"));
        var subDir = Path.Combine(baseDir, "d");
        Directory.CreateDirectory(subDir);
        var fileConf1 = "a.conf";
        var fileTxt = "b.txt";
        var fileConf2 = "c.conf";
        try
        {
            File.WriteAllText(Path.Combine(subDir, fileConf1), "");
            File.WriteAllText(Path.Combine(subDir, fileTxt), "");
            File.WriteAllText(Path.Combine(subDir, fileConf2), "");
            var main = Path.Combine(baseDir, "dnsmasq.conf");
            File.WriteAllText(main, "conf-dir=d,*.conf\n");
            var paths = DnsmasqConfIncludeParser.GetIncludedPaths(main);
            Assert.Equal(3, paths.Count);
            Assert.Contains(paths, p => p.EndsWith(fileConf1, StringComparison.Ordinal));
            Assert.Contains(paths, p => p.EndsWith(fileConf2, StringComparison.Ordinal));
            Assert.DoesNotContain(paths, p => p.EndsWith(fileTxt, StringComparison.Ordinal));
        }
        finally
        {
            Directory.Delete(baseDir, recursive: true);
        }
    }

    /// <summary>dnsmasq conf-dir=dir,.suffix ignores files ending with suffix (option.c ignore_suffix).</summary>
    [Fact]
    public void GetIncludedPaths_ConfDir_WithIgnoreSuffix_ExcludesMatchingFiles()
    {
        var baseDir = Path.Combine(Path.GetTempPath(), "dnsmasq-ignore-" + Guid.NewGuid().ToString("N"));
        var subDir = Path.Combine(baseDir, "d");
        Directory.CreateDirectory(subDir);
        var fileIncluded = "a.conf";
        var fileExcluded = "b.conf.bak";
        try
        {
            File.WriteAllText(Path.Combine(subDir, fileIncluded), "");
            File.WriteAllText(Path.Combine(subDir, fileExcluded), "");
            var main = Path.Combine(baseDir, "dnsmasq.conf");
            File.WriteAllText(main, "conf-dir=d,.bak\n");
            var paths = DnsmasqConfIncludeParser.GetIncludedPaths(main);
            Assert.Equal(2, paths.Count);
            Assert.Contains(paths, p => p.EndsWith(fileIncluded, StringComparison.Ordinal));
            Assert.DoesNotContain(paths, p => p.EndsWith(fileExcluded, StringComparison.Ordinal));
        }
        finally
        {
            Directory.Delete(baseDir, recursive: true);
        }
    }

    [Fact]
    public void GetAddnHostsPathsFromConfigFilesWithSource_ReturnsSourcePerPath()
    {
        var dir = Path.Combine(Path.GetTempPath(), "dnsmasq-addn-src-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        var f1 = Path.Combine(dir, "01.conf");
        var f2 = Path.Combine(dir, "02.conf");
        var managedPath = Path.Combine(dir, "managed.conf");
        var path1 = "/etc/hosts.a";
        var path2 = "/etc/hosts.b";
        var path3 = "/etc/hosts.c";
        try
        {
            File.WriteAllText(f1, $"addn-hosts={path1}\n");
            File.WriteAllText(f2, $"addn-hosts={path2}\naddn-hosts={path3}\n");
            var result = DnsmasqConfIncludeParser.GetAddnHostsPathsFromConfigFilesWithSource(new[] { f1, f2 }, managedPath);
            Assert.Equal(3, result.Count);
            Assert.Equal(Path.GetFullPath(path1), result[0].Path);
            Assert.Equal(Path.GetFileName(f1), result[0].Source.FileName);
            Assert.False(result[0].Source.IsManaged);
            Assert.Equal(1, result[0].Source.LineNumber);
            Assert.Equal(Path.GetFullPath(path2), result[1].Path);
            Assert.Equal(Path.GetFileName(f2), result[1].Source.FileName);
            Assert.Equal(1, result[1].Source.LineNumber);
            Assert.Equal(Path.GetFullPath(path3), result[2].Path);
            Assert.Equal(2, result[2].Source.LineNumber);
            Assert.True(result[2].Source.IsReadOnly == !result[2].Source.IsManaged);
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    [Fact]
    public void GetAddnHostsPathsFromConfigFilesWithSource_WhenManagedFileInList_IsManagedTrue()
    {
        var dir = Path.Combine(Path.GetTempPath(), "dnsmasq-addn-mgd-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        var mainPath = Path.Combine(dir, "dnsmasq.conf");
        var managedPath = Path.Combine(dir, "zz-managed.conf");
        var readOnlyPath = "/other/hosts";
        var managedAddnPath = "/managed/hosts";
        try
        {
            File.WriteAllText(mainPath, $"addn-hosts={readOnlyPath}\n");
            File.WriteAllText(managedPath, $"addn-hosts={managedAddnPath}\n");
            var result = DnsmasqConfIncludeParser.GetAddnHostsPathsFromConfigFilesWithSource(new[] { mainPath, managedPath }, managedPath);
            Assert.Equal(2, result.Count);
            Assert.False(result[0].Source.IsManaged);
            Assert.Equal(1, result[0].Source.LineNumber);
            Assert.True(result[1].Source.IsManaged);
            Assert.Equal(1, result[1].Source.LineNumber);
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    [Fact]
    public void GetMultiValueFromConfigFiles_ServerAndLocal_CollectsBothInOrder()
    {
        var dir = Path.Combine(Path.GetTempPath(), "dnsmasq-multi-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        var f1 = Path.Combine(dir, "01.conf");
        var f2 = Path.Combine(dir, "02.conf");
        var v1 = "1.1.1.1";
        var v2 = "/local/";
        var v3 = "/example.com/192.168.1.1";
        try
        {
            File.WriteAllText(f1, $"server={v1}\nlocal={v2}\n");
            File.WriteAllText(f2, $"server={v3}\n");
            var result = DnsmasqConfIncludeParser.GetMultiValueFromConfigFiles(new[] { f1, f2 }, DnsmasqConfKeys.ServerLocalKeys);
            Assert.Equal(3, result.Count);
            Assert.Equal(v1, result[0]);
            Assert.Equal(v2, result[1]);
            Assert.Equal(v3, result[2]);
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    [Fact]
    public void GetMultiValueFromConfigFiles_DhcpRange_CollectsAllInOrder()
    {
        var dir = Path.Combine(Path.GetTempPath(), "dnsmasq-dhcp-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        var conf = Path.Combine(dir, "dnsmasq.conf");
        var range1 = "172.28.0.10,172.28.0.50,12h";
        var range2 = "192.168.1.10,192.168.1.100,255.255.255.0,24h";
        try
        {
            File.WriteAllText(conf, $"dhcp-range={range1}\ndhcp-range={range2}\n");
            var result = DnsmasqConfIncludeParser.GetMultiValueFromConfigFiles(new[] { conf }, DnsmasqConfKeys.DhcpRange);
            Assert.Equal(2, result.Count);
            Assert.Equal(range1, result[0]);
            Assert.Equal(range2, result[1]);
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    [Fact]
    public void GetFlagFromConfigFiles_AllServers_WhenPresent_ReturnsTrue()
    {
        var dir = Path.Combine(Path.GetTempPath(), "dnsmasq-allsrv-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        var conf = Path.Combine(dir, "dnsmasq.conf");
        try
        {
            File.WriteAllText(conf, "all-servers\n");
            var result = DnsmasqConfIncludeParser.GetFlagFromConfigFiles(new[] { conf }, DnsmasqConfKeys.AllServers);
            Assert.True(result);
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    [Fact]
    public void GetLastValueFromConfigFiles_LogQueries_ReturnsValue()
    {
        var dir = Path.Combine(Path.GetTempPath(), "dnsmasq-logq-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        var conf = Path.Combine(dir, "dnsmasq.conf");
        try
        {
            File.WriteAllText(conf, "log-queries=extra\n");
            var (value, _) = DnsmasqConfIncludeParser.GetLastValueFromConfigFiles(new[] { conf }, DnsmasqConfKeys.LogQueries);
            Assert.Equal("extra", value);
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    [Fact]
    public void GetMultiValueFromConfigFiles_RevServer_ReturnsAllValues()
    {
        var dir = Path.Combine(Path.GetTempPath(), "dnsmasq-rev-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        var conf = Path.Combine(dir, "dnsmasq.conf");
        var v1 = "1.2.3.0/24,192.168.0.1";
        var v2 = "10.0.0.0/8,10.0.0.1";
        try
        {
            File.WriteAllText(conf, $"rev-server={v1}\nrev-server={v2}\n");
            var result = DnsmasqConfIncludeParser.GetMultiValueFromConfigFiles(new[] { conf }, DnsmasqConfKeys.RevServer);
            Assert.Equal(2, result.Count);
            Assert.Equal(v1, result[0]);
            Assert.Equal(v2, result[1]);
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    // --- Phase 4: one test per new option with a real dnsmasq example ---

    [Fact]
    public void GetLastValueFromConfigFiles_Hostsdir_ParsesRealExample()
    {
        var dir = Path.Combine(Path.GetTempPath(), "dnsmasq-hostsdir-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        var conf = Path.Combine(dir, "dnsmasq.conf");
        var path = "/etc/dnsmasq.d/hosts";
        try
        {
            File.WriteAllText(conf, $"hostsdir={path}\n");
            var (value, configDir) = DnsmasqConfIncludeParser.GetLastValueFromConfigFiles(new[] { conf }, DnsmasqConfKeys.Hostsdir);
            Assert.Equal(path, value);
            Assert.Equal(dir, configDir);
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    [Fact]
    public void GetMultiValueFromConfigFiles_Domain_ParsesRealExample()
    {
        var dir = Path.Combine(Path.GetTempPath(), "dnsmasq-domain-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        var conf = Path.Combine(dir, "dnsmasq.conf");
        var line = "home,192.168.1.1";
        try
        {
            File.WriteAllText(conf, $"domain={line}\n");
            var result = DnsmasqConfIncludeParser.GetMultiValueFromConfigFiles(new[] { conf }, DnsmasqConfKeys.Domain);
            Assert.Single(result);
            Assert.Equal(line, result[0]);
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    [Fact]
    public void GetMultiValueFromConfigFiles_Cname_ParsesRealExample()
    {
        var dir = Path.Combine(Path.GetTempPath(), "dnsmasq-cname-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        var conf = Path.Combine(dir, "dnsmasq.conf");
        var line = "mail.example.com,real.example.com";
        try
        {
            File.WriteAllText(conf, $"cname={line}\n");
            var result = DnsmasqConfIncludeParser.GetMultiValueFromConfigFiles(new[] { conf }, DnsmasqConfKeys.Cname);
            Assert.Single(result);
            Assert.Equal(line, result[0]);
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    [Fact]
    public void GetMultiValueFromConfigFiles_MxHost_ParsesRealExample()
    {
        var dir = Path.Combine(Path.GetTempPath(), "dnsmasq-mx-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        var conf = Path.Combine(dir, "dnsmasq.conf");
        var line = "example.com,mail.example.com,50";
        try
        {
            File.WriteAllText(conf, $"mx-host={line}\n");
            var result = DnsmasqConfIncludeParser.GetMultiValueFromConfigFiles(new[] { conf }, DnsmasqConfKeys.MxHost);
            Assert.Single(result);
            Assert.Equal(line, result[0]);
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    [Fact]
    public void GetMultiValueFromConfigFiles_Srv_ParsesRealExample()
    {
        var dir = Path.Combine(Path.GetTempPath(), "dnsmasq-srv-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        var conf = Path.Combine(dir, "dnsmasq.conf");
        var line = "_http._tcp.example.com,server.example.com,80";
        try
        {
            File.WriteAllText(conf, $"srv={line}\n");
            var result = DnsmasqConfIncludeParser.GetMultiValueFromConfigFiles(new[] { conf }, DnsmasqConfKeys.Srv);
            Assert.Single(result);
            Assert.Equal(line, result[0]);
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    [Fact]
    public void GetMultiValueFromConfigFiles_PtrRecord_ParsesRealExample()
    {
        var dir = Path.Combine(Path.GetTempPath(), "dnsmasq-ptr-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        var conf = Path.Combine(dir, "dnsmasq.conf");
        var line = "1.168.192.in-addr.arpa,host.example.com";
        try
        {
            File.WriteAllText(conf, $"ptr-record={line}\n");
            var result = DnsmasqConfIncludeParser.GetMultiValueFromConfigFiles(new[] { conf }, DnsmasqConfKeys.PtrRecord);
            Assert.Single(result);
            Assert.Equal(line, result[0]);
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    [Fact]
    public void GetMultiValueFromConfigFiles_TxtRecord_ParsesRealExample()
    {
        var dir = Path.Combine(Path.GetTempPath(), "dnsmasq-txt-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        var conf = Path.Combine(dir, "dnsmasq.conf");
        var line = "example.com,v=spf1 include:_spf.example.com";
        try
        {
            File.WriteAllText(conf, $"txt-record={line}\n");
            var result = DnsmasqConfIncludeParser.GetMultiValueFromConfigFiles(new[] { conf }, DnsmasqConfKeys.TxtRecord);
            Assert.Single(result);
            Assert.Equal(line, result[0]);
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    [Fact]
    public void GetMultiValueFromConfigFiles_NaptrRecord_ParsesRealExample()
    {
        var dir = Path.Combine(Path.GetTempPath(), "dnsmasq-naptr-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        var conf = Path.Combine(dir, "dnsmasq.conf");
        var line = "example.com,100,10,u,sip+E2U,sips:.*@example.com,.";
        try
        {
            File.WriteAllText(conf, $"naptr-record={line}\n");
            var result = DnsmasqConfIncludeParser.GetMultiValueFromConfigFiles(new[] { conf }, DnsmasqConfKeys.NaptrRecord);
            Assert.Single(result);
            Assert.Equal(line, result[0]);
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    [Fact]
    public void GetMultiValueFromConfigFiles_HostRecord_ParsesRealExample()
    {
        var dir = Path.Combine(Path.GetTempPath(), "dnsmasq-hostrecord-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        var conf = Path.Combine(dir, "dnsmasq.conf");
        var line = "router.example.com,192.168.1.1";
        try
        {
            File.WriteAllText(conf, $"host-record={line}\n");
            var result = DnsmasqConfIncludeParser.GetMultiValueFromConfigFiles(new[] { conf }, DnsmasqConfKeys.HostRecord);
            Assert.Single(result);
            Assert.Equal(line, result[0]);
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    [Fact]
    public void GetMultiValueFromConfigFiles_DynamicHost_ParsesRealExample()
    {
        var dir = Path.Combine(Path.GetTempPath(), "dnsmasq-dynamichost-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        var conf = Path.Combine(dir, "dnsmasq.conf");
        var line = "hostname,example.com,192.168.1.50";
        try
        {
            File.WriteAllText(conf, $"dynamic-host={line}\n");
            var result = DnsmasqConfIncludeParser.GetMultiValueFromConfigFiles(new[] { conf }, DnsmasqConfKeys.DynamicHost);
            Assert.Single(result);
            Assert.Equal(line, result[0]);
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    [Fact]
    public void GetMultiValueFromConfigFiles_InterfaceName_ParsesRealExample()
    {
        var dir = Path.Combine(Path.GetTempPath(), "dnsmasq-ifname-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        var conf = Path.Combine(dir, "dnsmasq.conf");
        var line = "eth0,router.local";
        try
        {
            File.WriteAllText(conf, $"interface-name={line}\n");
            var result = DnsmasqConfIncludeParser.GetMultiValueFromConfigFiles(new[] { conf }, DnsmasqConfKeys.InterfaceName);
            Assert.Single(result);
            Assert.Equal(line, result[0]);
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    // --- Remaining options: one test per new option with a real dnsmasq example ---

    [Fact]
    public void GetMultiValueFromConfigFiles_DhcpMatch_ParsesRealExample()
    {
        var dir = Path.Combine(Path.GetTempPath(), "dnsmasq-dhcpmatch-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        var conf = Path.Combine(dir, "dnsmasq.conf");
        var line = "set:bios,option:client-arch,0";
        try
        {
            File.WriteAllText(conf, $"dhcp-match={line}\n");
            var result = DnsmasqConfIncludeParser.GetMultiValueFromConfigFiles(new[] { conf }, DnsmasqConfKeys.DhcpMatch);
            Assert.Single(result);
            Assert.Equal(line, result[0]);
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    [Fact]
    public void GetMultiValueFromConfigFiles_DhcpBoot_ParsesRealExample()
    {
        var dir = Path.Combine(Path.GetTempPath(), "dnsmasq-dhcpboot-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        var conf = Path.Combine(dir, "dnsmasq.conf");
        var line = "tag:bios-x86,firmware/ipxe.pxe,192.168.1.1";
        try
        {
            File.WriteAllText(conf, $"dhcp-boot={line}\n");
            var result = DnsmasqConfIncludeParser.GetMultiValueFromConfigFiles(new[] { conf }, DnsmasqConfKeys.DhcpBoot);
            Assert.Single(result);
            Assert.Equal(line, result[0]);
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    [Fact]
    public void GetMultiValueFromConfigFiles_DhcpIgnore_ParsesRealExample()
    {
        var dir = Path.Combine(Path.GetTempPath(), "dnsmasq-dhcpignore-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        var conf = Path.Combine(dir, "dnsmasq.conf");
        var line = "tag:!known";
        try
        {
            File.WriteAllText(conf, $"dhcp-ignore={line}\n");
            var result = DnsmasqConfIncludeParser.GetMultiValueFromConfigFiles(new[] { conf }, DnsmasqConfKeys.DhcpIgnore);
            Assert.Single(result);
            Assert.Equal(line, result[0]);
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    [Fact]
    public void GetMultiValueFromConfigFiles_DhcpVendorclass_ParsesRealExample()
    {
        var dir = Path.Combine(Path.GetTempPath(), "dnsmasq-dhcpvc-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        var conf = Path.Combine(dir, "dnsmasq.conf");
        var line = "set:pxe,option:vendor-class-identifier,PXEClient";
        try
        {
            File.WriteAllText(conf, $"dhcp-vendorclass={line}\n");
            var result = DnsmasqConfIncludeParser.GetMultiValueFromConfigFiles(new[] { conf }, DnsmasqConfKeys.DhcpVendorclass);
            Assert.Single(result);
            Assert.Equal(line, result[0]);
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    [Fact]
    public void GetMultiValueFromConfigFiles_DhcpUserclass_ParsesRealExample()
    {
        var dir = Path.Combine(Path.GetTempPath(), "dnsmasq-dhcpuc-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        var conf = Path.Combine(dir, "dnsmasq.conf");
        var line = "set:ipxe,iPXE";
        try
        {
            File.WriteAllText(conf, $"dhcp-userclass={line}\n");
            var result = DnsmasqConfIncludeParser.GetMultiValueFromConfigFiles(new[] { conf }, DnsmasqConfKeys.DhcpUserclass);
            Assert.Single(result);
            Assert.Equal(line, result[0]);
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    [Fact]
    public void GetMultiValueFromConfigFiles_RaParam_ParsesRealExample()
    {
        var dir = Path.Combine(Path.GetTempPath(), "dnsmasq-raparam-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        var conf = Path.Combine(dir, "dnsmasq.conf");
        var line = "eth0,high,0";
        try
        {
            File.WriteAllText(conf, $"ra-param={line}\n");
            var result = DnsmasqConfIncludeParser.GetMultiValueFromConfigFiles(new[] { conf }, DnsmasqConfKeys.RaParam);
            Assert.Single(result);
            Assert.Equal(line, result[0]);
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    [Fact]
    public void GetMultiValueFromConfigFiles_Slaac_ParsesRealExample()
    {
        var dir = Path.Combine(Path.GetTempPath(), "dnsmasq-slaac-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        var conf = Path.Combine(dir, "dnsmasq.conf");
        var line = "eth0,0";
        try
        {
            File.WriteAllText(conf, $"slaac={line}\n");
            var result = DnsmasqConfIncludeParser.GetMultiValueFromConfigFiles(new[] { conf }, DnsmasqConfKeys.Slaac);
            Assert.Single(result);
            Assert.Equal(line, result[0]);
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    [Fact]
    public void GetFlagFromConfigFiles_EnableTftp_WhenPresent_ReturnsTrue()
    {
        var dir = Path.Combine(Path.GetTempPath(), "dnsmasq-tftp-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        var conf = Path.Combine(dir, "dnsmasq.conf");
        try
        {
            File.WriteAllText(conf, "enable-tftp\n");
            var result = DnsmasqConfIncludeParser.GetFlagFromConfigFiles(new[] { conf }, DnsmasqConfKeys.EnableTftp);
            Assert.True(result);
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    [Fact]
    public void GetLastValueFromConfigFiles_TftpRoot_ParsesRealExample()
    {
        var dir = Path.Combine(Path.GetTempPath(), "dnsmasq-tftproot-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        var conf = Path.Combine(dir, "dnsmasq.conf");
        var path = "/var/lib/tftpboot";
        try
        {
            File.WriteAllText(conf, $"tftp-root={path}\n");
            var (value, _) = DnsmasqConfIncludeParser.GetLastValueFromConfigFiles(new[] { conf }, DnsmasqConfKeys.TftpRoot);
            Assert.Equal(path, value);
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    [Fact]
    public void GetFlagFromConfigFiles_TftpSecure_WhenPresent_ReturnsTrue()
    {
        var dir = Path.Combine(Path.GetTempPath(), "dnsmasq-tftpsec-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        var conf = Path.Combine(dir, "dnsmasq.conf");
        try
        {
            File.WriteAllText(conf, "tftp-secure\n");
            var result = DnsmasqConfIncludeParser.GetFlagFromConfigFiles(new[] { conf }, DnsmasqConfKeys.TftpSecure);
            Assert.True(result);
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    [Fact]
    public void GetMultiValueFromConfigFiles_PxeService_ParsesRealExample()
    {
        var dir = Path.Combine(Path.GetTempPath(), "dnsmasq-pxe-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        var conf = Path.Combine(dir, "dnsmasq.conf");
        var line = "x86PC,pxelinux.0";
        try
        {
            File.WriteAllText(conf, $"pxe-service={line}\n");
            var result = DnsmasqConfIncludeParser.GetMultiValueFromConfigFiles(new[] { conf }, DnsmasqConfKeys.PxeService);
            Assert.Single(result);
            Assert.Equal(line, result[0]);
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    [Fact]
    public void GetLastValueFromConfigFiles_PxePrompt_ParsesRealExample()
    {
        var dir = Path.Combine(Path.GetTempPath(), "dnsmasq-pxeprompt-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        var conf = Path.Combine(dir, "dnsmasq.conf");
        var line = "timeout,0";
        try
        {
            File.WriteAllText(conf, $"pxe-prompt={line}\n");
            var (value, _) = DnsmasqConfIncludeParser.GetLastValueFromConfigFiles(new[] { conf }, DnsmasqConfKeys.PxePrompt);
            Assert.Equal(line, value);
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    [Fact]
    public void GetFlagFromConfigFiles_Dnssec_WhenPresent_ReturnsTrue()
    {
        var dir = Path.Combine(Path.GetTempPath(), "dnsmasq-dnssec-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        var conf = Path.Combine(dir, "dnsmasq.conf");
        try
        {
            File.WriteAllText(conf, "dnssec\n");
            var result = DnsmasqConfIncludeParser.GetFlagFromConfigFiles(new[] { conf }, DnsmasqConfKeys.Dnssec);
            Assert.True(result);
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    [Fact]
    public void GetMultiValueFromConfigFiles_TrustAnchor_ParsesRealExample()
    {
        var dir = Path.Combine(Path.GetTempPath(), "dnsmasq-trust-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        var conf = Path.Combine(dir, "dnsmasq.conf");
        var line = ". IN DS 19036 8 2 49AAC11D7B6F6446702E54A1607371607A1A41855200FD2CE1CDE32F024E8FC5";
        try
        {
            File.WriteAllText(conf, $"trust-anchor={line}\n");
            var result = DnsmasqConfIncludeParser.GetMultiValueFromConfigFiles(new[] { conf }, DnsmasqConfKeys.TrustAnchor);
            Assert.Single(result);
            Assert.Equal(line, result[0]);
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    [Fact]
    public void GetFlagFromConfigFiles_DnssecCheckUnsigned_WhenPresent_ReturnsTrue()
    {
        var dir = Path.Combine(Path.GetTempPath(), "dnsmasq-dnsseccu-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        var conf = Path.Combine(dir, "dnsmasq.conf");
        try
        {
            File.WriteAllText(conf, "dnssec-check-unsigned\n");
            var result = DnsmasqConfIncludeParser.GetFlagFromConfigFiles(new[] { conf }, DnsmasqConfKeys.DnssecCheckUnsigned);
            Assert.True(result);
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    [Fact]
    public void GetLastValueFromConfigFiles_EnableDbus_ParsesRealExample()
    {
        var dir = Path.Combine(Path.GetTempPath(), "dnsmasq-dbus-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        var conf = Path.Combine(dir, "dnsmasq.conf");
        var svc = "org.freedesktop.NetworkManager";
        try
        {
            File.WriteAllText(conf, $"enable-dbus={svc}\n");
            var (value, _) = DnsmasqConfIncludeParser.GetLastValueFromConfigFiles(new[] { conf }, DnsmasqConfKeys.EnableDbus);
            Assert.Equal(svc, value);
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    [Fact]
    public void GetLastValueFromConfigFiles_EnableUbus_ParsesRealExample()
    {
        var dir = Path.Combine(Path.GetTempPath(), "dnsmasq-ubus-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        var conf = Path.Combine(dir, "dnsmasq.conf");
        var svc = "com.example.dnsmasq";
        try
        {
            File.WriteAllText(conf, $"enable-ubus={svc}\n");
            var (value, _) = DnsmasqConfIncludeParser.GetLastValueFromConfigFiles(new[] { conf }, DnsmasqConfKeys.EnableUbus);
            Assert.Equal(svc, value);
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    [Fact]
    public void GetLastValueFromConfigFiles_FastDnsRetry_ParsesRealExample()
    {
        var dir = Path.Combine(Path.GetTempPath(), "dnsmasq-fastretry-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        var conf = Path.Combine(dir, "dnsmasq.conf");
        var line = "500,5000";
        try
        {
            File.WriteAllText(conf, $"fast-dns-retry={line}\n");
            var (value, _) = DnsmasqConfIncludeParser.GetLastValueFromConfigFiles(new[] { conf }, DnsmasqConfKeys.FastDnsRetry);
            Assert.Equal(line, value);
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    // --- Additional tests: one per conf field so every option has a documented example test ---

    [Fact]
    public void GetMultiValueFromConfigFiles_Interface_ParsesRealExample()
    {
        var dir = Path.Combine(Path.GetTempPath(), "dnsmasq-if-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        var conf = Path.Combine(dir, "dnsmasq.conf");
        try
        {
            File.WriteAllText(conf, "interface=eth0\ninterface=wlan0\n");
            var result = DnsmasqConfIncludeParser.GetMultiValueFromConfigFiles(new[] { conf }, DnsmasqConfKeys.Interface);
            Assert.Equal(2, result.Count);
            Assert.Equal("eth0", result[0]);
            Assert.Equal("wlan0", result[1]);
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    [Fact]
    public void GetMultiValueFromConfigFiles_ExceptInterface_ParsesRealExample()
    {
        var dir = Path.Combine(Path.GetTempPath(), "dnsmasq-except-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        var conf = Path.Combine(dir, "dnsmasq.conf");
        try
        {
            File.WriteAllText(conf, "except-interface=eth1\n");
            var result = DnsmasqConfIncludeParser.GetMultiValueFromConfigFiles(new[] { conf }, DnsmasqConfKeys.ExceptInterface);
            Assert.Single(result);
            Assert.Equal("eth1", result[0]);
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    [Fact]
    public void GetMultiValueFromConfigFiles_NoDhcpInterface_ParsesRealExample()
    {
        var dir = Path.Combine(Path.GetTempPath(), "dnsmasq-nodhcpi-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        var conf = Path.Combine(dir, "dnsmasq.conf");
        try
        {
            File.WriteAllText(conf, "no-dhcp-interface=eth2\n");
            var result = DnsmasqConfIncludeParser.GetMultiValueFromConfigFiles(new[] { conf }, DnsmasqConfKeys.NoDhcpInterface);
            Assert.Single(result);
            Assert.Equal("eth2", result[0]);
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    [Fact]
    public void GetMultiValueFromConfigFiles_NoDhcpv4Interface_ParsesRealExample()
    {
        var dir = Path.Combine(Path.GetTempPath(), "dnsmasq-nodhcp4-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        var conf = Path.Combine(dir, "dnsmasq.conf");
        try
        {
            File.WriteAllText(conf, "no-dhcpv4-interface=eth0\n");
            var result = DnsmasqConfIncludeParser.GetMultiValueFromConfigFiles(new[] { conf }, DnsmasqConfKeys.NoDhcpv4Interface);
            Assert.Single(result);
            Assert.Equal("eth0", result[0]);
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    [Fact]
    public void GetMultiValueFromConfigFiles_NoDhcpv6Interface_ParsesRealExample()
    {
        var dir = Path.Combine(Path.GetTempPath(), "dnsmasq-nodhcp6-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        var conf = Path.Combine(dir, "dnsmasq.conf");
        try
        {
            File.WriteAllText(conf, "no-dhcpv6-interface=eth0\n");
            var result = DnsmasqConfIncludeParser.GetMultiValueFromConfigFiles(new[] { conf }, DnsmasqConfKeys.NoDhcpv6Interface);
            Assert.Single(result);
            Assert.Equal("eth0", result[0]);
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    [Fact]
    public void GetMultiValueFromConfigFiles_AuthServer_ParsesRealExample()
    {
        var dir = Path.Combine(Path.GetTempPath(), "dnsmasq-auth-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        var conf = Path.Combine(dir, "dnsmasq.conf");
        var line = "lan,eth0";
        try
        {
            File.WriteAllText(conf, $"auth-server={line}\n");
            var result = DnsmasqConfIncludeParser.GetMultiValueFromConfigFiles(new[] { conf }, DnsmasqConfKeys.AuthServer);
            Assert.Single(result);
            Assert.Equal(line, result[0]);
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    [Fact]
    public void GetMultiValueFromConfigFiles_DhcpHost_ParsesRealExample()
    {
        var dir = Path.Combine(Path.GetTempPath(), "dnsmasq-dhcphost-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        var conf = Path.Combine(dir, "dnsmasq.conf");
        var line = "00:11:22:33:44:55,192.168.1.100,router,12h";
        try
        {
            File.WriteAllText(conf, $"dhcp-host={line}\n");
            var result = DnsmasqConfIncludeParser.GetMultiValueFromConfigFiles(new[] { conf }, DnsmasqConfKeys.DhcpHost);
            Assert.Single(result);
            Assert.Equal(line, result[0]);
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    [Fact]
    public void GetMultiValueFromConfigFiles_DhcpOption_ParsesRealExample()
    {
        var dir = Path.Combine(Path.GetTempPath(), "dnsmasq-dhcpopt-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        var conf = Path.Combine(dir, "dnsmasq.conf");
        var line = "option:router,192.168.1.1";
        try
        {
            File.WriteAllText(conf, $"dhcp-option={line}\n");
            var result = DnsmasqConfIncludeParser.GetMultiValueFromConfigFiles(new[] { conf }, DnsmasqConfKeys.DhcpOption);
            Assert.Single(result);
            Assert.Equal(line, result[0]);
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    [Fact]
    public void GetMultiValueFromConfigFiles_ResolvFile_ParsesRealExample()
    {
        var dir = Path.Combine(Path.GetTempPath(), "dnsmasq-resolv-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        var conf = Path.Combine(dir, "dnsmasq.conf");
        var path = "/etc/resolv.dnsmasq";
        try
        {
            File.WriteAllText(conf, $"resolv-file={path}\n");
            var result = DnsmasqConfIncludeParser.GetMultiValueFromConfigFiles(new[] { conf }, DnsmasqConfKeys.ResolvFile);
            Assert.Single(result);
            Assert.Equal(path, result[0]);
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    [Fact]
    public void GetMultiValueFromConfigFiles_RebindDomainOk_ParsesRealExample()
    {
        var dir = Path.Combine(Path.GetTempPath(), "dnsmasq-rebind-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        var conf = Path.Combine(dir, "dnsmasq.conf");
        var line = "/local.lan/";
        try
        {
            File.WriteAllText(conf, $"rebind-domain-ok={line}\n");
            var result = DnsmasqConfIncludeParser.GetMultiValueFromConfigFiles(new[] { conf }, DnsmasqConfKeys.RebindDomainOk);
            Assert.Single(result);
            Assert.Equal(line, result[0]);
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    [Fact]
    public void GetMultiValueFromConfigFiles_BogusNxdomain_ParsesRealExample()
    {
        var dir = Path.Combine(Path.GetTempPath(), "dnsmasq-bogusnx-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        var conf = Path.Combine(dir, "dnsmasq.conf");
        var line = "64.94.110.11";
        try
        {
            File.WriteAllText(conf, $"bogus-nxdomain={line}\n");
            var result = DnsmasqConfIncludeParser.GetMultiValueFromConfigFiles(new[] { conf }, DnsmasqConfKeys.BogusNxdomain);
            Assert.Single(result);
            Assert.Equal(line, result[0]);
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    [Fact]
    public void GetMultiValueFromConfigFiles_IgnoreAddress_ParsesRealExample()
    {
        var dir = Path.Combine(Path.GetTempPath(), "dnsmasq-ignoreaddr-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        var conf = Path.Combine(dir, "dnsmasq.conf");
        var line = "192.168.1.0/24";
        try
        {
            File.WriteAllText(conf, $"ignore-address={line}\n");
            var result = DnsmasqConfIncludeParser.GetMultiValueFromConfigFiles(new[] { conf }, DnsmasqConfKeys.IgnoreAddress);
            Assert.Single(result);
            Assert.Equal(line, result[0]);
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    [Fact]
    public void GetMultiValueFromConfigFiles_Alias_ParsesRealExample()
    {
        var dir = Path.Combine(Path.GetTempPath(), "dnsmasq-alias-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        var conf = Path.Combine(dir, "dnsmasq.conf");
        var line = "1.2.3.0,6.7.8.0,255.255.255.0";
        try
        {
            File.WriteAllText(conf, $"alias={line}\n");
            var result = DnsmasqConfIncludeParser.GetMultiValueFromConfigFiles(new[] { conf }, DnsmasqConfKeys.Alias);
            Assert.Single(result);
            Assert.Equal(line, result[0]);
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    [Fact]
    public void GetMultiValueFromConfigFiles_FilterRr_ParsesRealExample()
    {
        var dir = Path.Combine(Path.GetTempPath(), "dnsmasq-filterrr-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        var conf = Path.Combine(dir, "dnsmasq.conf");
        var line = "TXT,MX";
        try
        {
            File.WriteAllText(conf, $"filter-rr={line}\n");
            var result = DnsmasqConfIncludeParser.GetMultiValueFromConfigFiles(new[] { conf }, DnsmasqConfKeys.FilterRr);
            Assert.Single(result);
            Assert.Equal(line, result[0]);
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    [Fact]
    public void GetMultiValueFromConfigFiles_CacheRr_ParsesRealExample()
    {
        var dir = Path.Combine(Path.GetTempPath(), "dnsmasq-cacherr-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        var conf = Path.Combine(dir, "dnsmasq.conf");
        var line = "TXT";
        try
        {
            File.WriteAllText(conf, $"cache-rr={line}\n");
            var result = DnsmasqConfIncludeParser.GetMultiValueFromConfigFiles(new[] { conf }, DnsmasqConfKeys.CacheRr);
            Assert.Single(result);
            Assert.Equal(line, result[0]);
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    [Fact]
    public void GetFlagFromConfigFiles_NoHosts_WhenPresent_ReturnsTrue()
    {
        var dir = Path.Combine(Path.GetTempPath(), "dnsmasq-nohosts-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        var conf = Path.Combine(dir, "dnsmasq.conf");
        try
        {
            File.WriteAllText(conf, "no-hosts\n");
            var result = DnsmasqConfIncludeParser.GetFlagFromConfigFiles(new[] { conf }, DnsmasqConfKeys.NoHosts);
            Assert.True(result);
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    [Fact]
    public void GetFlagFromConfigFiles_BogusPriv_WhenPresent_ReturnsTrue()
    {
        var dir = Path.Combine(Path.GetTempPath(), "dnsmasq-boguspriv-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        var conf = Path.Combine(dir, "dnsmasq.conf");
        try
        {
            File.WriteAllText(conf, "bogus-priv\n");
            Assert.True(DnsmasqConfIncludeParser.GetFlagFromConfigFiles(new[] { conf }, DnsmasqConfKeys.BogusPriv));
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    [Fact]
    public void GetFlagFromConfigFiles_StrictOrder_WhenPresent_ReturnsTrue()
    {
        var dir = Path.Combine(Path.GetTempPath(), "dnsmasq-strict-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        var conf = Path.Combine(dir, "dnsmasq.conf");
        try
        {
            File.WriteAllText(conf, "strict-order\n");
            Assert.True(DnsmasqConfIncludeParser.GetFlagFromConfigFiles(new[] { conf }, DnsmasqConfKeys.StrictOrder));
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    [Fact]
    public void GetFlagFromConfigFiles_DomainNeeded_WhenPresent_ReturnsTrue()
    {
        var dir = Path.Combine(Path.GetTempPath(), "dnsmasq-domainneed-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        var conf = Path.Combine(dir, "dnsmasq.conf");
        try
        {
            File.WriteAllText(conf, "domain-needed\n");
            Assert.True(DnsmasqConfIncludeParser.GetFlagFromConfigFiles(new[] { conf }, DnsmasqConfKeys.DomainNeeded));
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    [Fact]
    public void GetFlagFromConfigFiles_NoPoll_WhenPresent_ReturnsTrue()
    {
        var dir = Path.Combine(Path.GetTempPath(), "dnsmasq-nopoll-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        var conf = Path.Combine(dir, "dnsmasq.conf");
        try
        {
            File.WriteAllText(conf, "no-poll\n");
            Assert.True(DnsmasqConfIncludeParser.GetFlagFromConfigFiles(new[] { conf }, DnsmasqConfKeys.NoPoll));
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    [Fact]
    public void GetFlagFromConfigFiles_BindInterfaces_WhenPresent_ReturnsTrue()
    {
        var dir = Path.Combine(Path.GetTempPath(), "dnsmasq-bindif-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        var conf = Path.Combine(dir, "dnsmasq.conf");
        try
        {
            File.WriteAllText(conf, "bind-interfaces\n");
            Assert.True(DnsmasqConfIncludeParser.GetFlagFromConfigFiles(new[] { conf }, DnsmasqConfKeys.BindInterfaces));
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    [Fact]
    public void GetFlagFromConfigFiles_BindDynamic_WhenPresent_ReturnsTrue()
    {
        var dir = Path.Combine(Path.GetTempPath(), "dnsmasq-binddyn-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        var conf = Path.Combine(dir, "dnsmasq.conf");
        try
        {
            File.WriteAllText(conf, "bind-dynamic\n");
            Assert.True(DnsmasqConfIncludeParser.GetFlagFromConfigFiles(new[] { conf }, DnsmasqConfKeys.BindDynamic));
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    [Fact]
    public void GetFlagFromConfigFiles_NoNegcache_WhenPresent_ReturnsTrue()
    {
        var dir = Path.Combine(Path.GetTempPath(), "dnsmasq-noneg-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        var conf = Path.Combine(dir, "dnsmasq.conf");
        try
        {
            File.WriteAllText(conf, "no-negcache\n");
            Assert.True(DnsmasqConfIncludeParser.GetFlagFromConfigFiles(new[] { conf }, DnsmasqConfKeys.NoNegcache));
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    [Fact]
    public void GetFlagFromConfigFiles_DnsLoopDetect_WhenPresent_ReturnsTrue()
    {
        var dir = Path.Combine(Path.GetTempPath(), "dnsmasq-loop-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        var conf = Path.Combine(dir, "dnsmasq.conf");
        try
        {
            File.WriteAllText(conf, "dns-loop-detect\n");
            Assert.True(DnsmasqConfIncludeParser.GetFlagFromConfigFiles(new[] { conf }, DnsmasqConfKeys.DnsLoopDetect));
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    [Fact]
    public void GetFlagFromConfigFiles_StopDnsRebind_WhenPresent_ReturnsTrue()
    {
        var dir = Path.Combine(Path.GetTempPath(), "dnsmasq-rebind-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        var conf = Path.Combine(dir, "dnsmasq.conf");
        try
        {
            File.WriteAllText(conf, "stop-dns-rebind\n");
            Assert.True(DnsmasqConfIncludeParser.GetFlagFromConfigFiles(new[] { conf }, DnsmasqConfKeys.StopDnsRebind));
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    [Fact]
    public void GetFlagFromConfigFiles_RebindLocalhostOk_WhenPresent_ReturnsTrue()
    {
        var dir = Path.Combine(Path.GetTempPath(), "dnsmasq-rebindlo-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        var conf = Path.Combine(dir, "dnsmasq.conf");
        try
        {
            File.WriteAllText(conf, "rebind-localhost-ok\n");
            Assert.True(DnsmasqConfIncludeParser.GetFlagFromConfigFiles(new[] { conf }, DnsmasqConfKeys.RebindLocalhostOk));
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    [Fact]
    public void GetFlagFromConfigFiles_ClearOnReload_WhenPresent_ReturnsTrue()
    {
        var dir = Path.Combine(Path.GetTempPath(), "dnsmasq-clear-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        var conf = Path.Combine(dir, "dnsmasq.conf");
        try
        {
            File.WriteAllText(conf, "clear-on-reload\n");
            Assert.True(DnsmasqConfIncludeParser.GetFlagFromConfigFiles(new[] { conf }, DnsmasqConfKeys.ClearOnReload));
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    [Fact]
    public void GetFlagFromConfigFiles_Filterwin2k_WhenPresent_ReturnsTrue()
    {
        var dir = Path.Combine(Path.GetTempPath(), "dnsmasq-filtw2k-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        var conf = Path.Combine(dir, "dnsmasq.conf");
        try
        {
            File.WriteAllText(conf, "filterwin2k\n");
            Assert.True(DnsmasqConfIncludeParser.GetFlagFromConfigFiles(new[] { conf }, DnsmasqConfKeys.Filterwin2k));
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    [Fact]
    public void GetFlagFromConfigFiles_FilterA_WhenPresent_ReturnsTrue()
    {
        var dir = Path.Combine(Path.GetTempPath(), "dnsmasq-filta-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        var conf = Path.Combine(dir, "dnsmasq.conf");
        try
        {
            File.WriteAllText(conf, "filter-A\n");
            Assert.True(DnsmasqConfIncludeParser.GetFlagFromConfigFiles(new[] { conf }, DnsmasqConfKeys.FilterA));
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    [Fact]
    public void GetFlagFromConfigFiles_FilterAaaa_WhenPresent_ReturnsTrue()
    {
        var dir = Path.Combine(Path.GetTempPath(), "dnsmasq-filtaaaa-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        var conf = Path.Combine(dir, "dnsmasq.conf");
        try
        {
            File.WriteAllText(conf, "filter-AAAA\n");
            Assert.True(DnsmasqConfIncludeParser.GetFlagFromConfigFiles(new[] { conf }, DnsmasqConfKeys.FilterAaaa));
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    [Fact]
    public void GetFlagFromConfigFiles_LocaliseQueries_WhenPresent_ReturnsTrue()
    {
        var dir = Path.Combine(Path.GetTempPath(), "dnsmasq-localise-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        var conf = Path.Combine(dir, "dnsmasq.conf");
        try
        {
            File.WriteAllText(conf, "localise-queries\n");
            Assert.True(DnsmasqConfIncludeParser.GetFlagFromConfigFiles(new[] { conf }, DnsmasqConfKeys.LocaliseQueries));
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    [Fact]
    public void GetFlagFromConfigFiles_LogDebug_WhenPresent_ReturnsTrue()
    {
        var dir = Path.Combine(Path.GetTempPath(), "dnsmasq-logdbg-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        var conf = Path.Combine(dir, "dnsmasq.conf");
        try
        {
            File.WriteAllText(conf, "log-debug\n");
            Assert.True(DnsmasqConfIncludeParser.GetFlagFromConfigFiles(new[] { conf }, DnsmasqConfKeys.LogDebug));
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    [Fact]
    public void GetFlagFromConfigFiles_DhcpAuthoritative_WhenPresent_ReturnsTrue()
    {
        var dir = Path.Combine(Path.GetTempPath(), "dnsmasq-dhcpauth-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        var conf = Path.Combine(dir, "dnsmasq.conf");
        try
        {
            File.WriteAllText(conf, "dhcp-authoritative\n");
            Assert.True(DnsmasqConfIncludeParser.GetFlagFromConfigFiles(new[] { conf }, DnsmasqConfKeys.DhcpAuthoritative));
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    [Fact]
    public void GetFlagFromConfigFiles_LeasefileRo_WhenPresent_ReturnsTrue()
    {
        var dir = Path.Combine(Path.GetTempPath(), "dnsmasq-leasero-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        var conf = Path.Combine(dir, "dnsmasq.conf");
        try
        {
            File.WriteAllText(conf, "leasefile-ro\n");
            Assert.True(DnsmasqConfIncludeParser.GetFlagFromConfigFiles(new[] { conf }, DnsmasqConfKeys.LeasefileRo));
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    [Fact]
    public void GetLastValueFromConfigFiles_LocalTtl_ParsesRealExample()
    {
        var dir = Path.Combine(Path.GetTempPath(), "dnsmasq-localttl-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        var conf = Path.Combine(dir, "dnsmasq.conf");
        try
        {
            File.WriteAllText(conf, "local-ttl=300\n");
            var (value, _) = DnsmasqConfIncludeParser.GetLastValueFromConfigFiles(new[] { conf }, DnsmasqConfKeys.LocalTtl);
            Assert.Equal("300", value);
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    [Fact]
    public void GetLastValueFromConfigFiles_PidFile_ParsesRealExample()
    {
        var dir = Path.Combine(Path.GetTempPath(), "dnsmasq-pid-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        var conf = Path.Combine(dir, "dnsmasq.conf");
        var path = "/var/run/dnsmasq.pid";
        try
        {
            File.WriteAllText(conf, $"pid-file={path}\n");
            var (value, _) = DnsmasqConfIncludeParser.GetLastValueFromConfigFiles(new[] { conf }, DnsmasqConfKeys.PidFile);
            Assert.Equal(path, value);
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    [Fact]
    public void GetLastValueFromConfigFiles_User_ParsesRealExample()
    {
        var dir = Path.Combine(Path.GetTempPath(), "dnsmasq-user-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        var conf = Path.Combine(dir, "dnsmasq.conf");
        try
        {
            File.WriteAllText(conf, "user=nobody\n");
            var (value, _) = DnsmasqConfIncludeParser.GetLastValueFromConfigFiles(new[] { conf }, DnsmasqConfKeys.User);
            Assert.Equal("nobody", value);
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    [Fact]
    public void GetLastValueFromConfigFiles_Group_ParsesRealExample()
    {
        var dir = Path.Combine(Path.GetTempPath(), "dnsmasq-group-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        var conf = Path.Combine(dir, "dnsmasq.conf");
        try
        {
            File.WriteAllText(conf, "group=dip\n");
            var (value, _) = DnsmasqConfIncludeParser.GetLastValueFromConfigFiles(new[] { conf }, DnsmasqConfKeys.Group);
            Assert.Equal("dip", value);
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    [Fact]
    public void GetLastValueFromConfigFiles_AuthTtl_ParsesRealExample()
    {
        var dir = Path.Combine(Path.GetTempPath(), "dnsmasq-authttl-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        var conf = Path.Combine(dir, "dnsmasq.conf");
        try
        {
            File.WriteAllText(conf, "auth-ttl=60\n");
            var (value, _) = DnsmasqConfIncludeParser.GetLastValueFromConfigFiles(new[] { conf }, DnsmasqConfKeys.AuthTtl);
            Assert.Equal("60", value);
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    [Fact]
    public void GetLastValueFromConfigFiles_EdnsPacketMax_ParsesRealExample()
    {
        var dir = Path.Combine(Path.GetTempPath(), "dnsmasq-edns-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        var conf = Path.Combine(dir, "dnsmasq.conf");
        try
        {
            File.WriteAllText(conf, "edns-packet-max=1232\n");
            var (value, _) = DnsmasqConfIncludeParser.GetLastValueFromConfigFiles(new[] { conf }, DnsmasqConfKeys.EdnsPacketMax);
            Assert.Equal("1232", value);
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    [Fact]
    public void GetLastValueFromConfigFiles_QueryPort_ParsesRealExample()
    {
        var dir = Path.Combine(Path.GetTempPath(), "dnsmasq-qport-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        var conf = Path.Combine(dir, "dnsmasq.conf");
        try
        {
            File.WriteAllText(conf, "query-port=0\n");
            var (value, _) = DnsmasqConfIncludeParser.GetLastValueFromConfigFiles(new[] { conf }, DnsmasqConfKeys.QueryPort);
            Assert.Equal("0", value);
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    [Fact]
    public void GetLastValueFromConfigFiles_PortLimit_ParsesRealExample()
    {
        var dir = Path.Combine(Path.GetTempPath(), "dnsmasq-portlimit-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        var conf = Path.Combine(dir, "dnsmasq.conf");
        try
        {
            File.WriteAllText(conf, "port-limit=3\n");
            var (value, _) = DnsmasqConfIncludeParser.GetLastValueFromConfigFiles(new[] { conf }, DnsmasqConfKeys.PortLimit);
            Assert.Equal("3", value);
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    [Fact]
    public void GetLastValueFromConfigFiles_MinPort_ParsesRealExample()
    {
        var dir = Path.Combine(Path.GetTempPath(), "dnsmasq-minport-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        var conf = Path.Combine(dir, "dnsmasq.conf");
        try
        {
            File.WriteAllText(conf, "min-port=1024\n");
            var (value, _) = DnsmasqConfIncludeParser.GetLastValueFromConfigFiles(new[] { conf }, DnsmasqConfKeys.MinPort);
            Assert.Equal("1024", value);
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    [Fact]
    public void GetLastValueFromConfigFiles_MaxPort_ParsesRealExample()
    {
        var dir = Path.Combine(Path.GetTempPath(), "dnsmasq-maxport-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        var conf = Path.Combine(dir, "dnsmasq.conf");
        try
        {
            File.WriteAllText(conf, "max-port=65535\n");
            var (value, _) = DnsmasqConfIncludeParser.GetLastValueFromConfigFiles(new[] { conf }, DnsmasqConfKeys.MaxPort);
            Assert.Equal("65535", value);
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    [Fact]
    public void GetLastValueFromConfigFiles_LogAsync_ParsesRealExample()
    {
        var dir = Path.Combine(Path.GetTempPath(), "dnsmasq-logasync-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        var conf = Path.Combine(dir, "dnsmasq.conf");
        try
        {
            File.WriteAllText(conf, "log-async=25\n");
            var (value, _) = DnsmasqConfIncludeParser.GetLastValueFromConfigFiles(new[] { conf }, DnsmasqConfKeys.LogAsync);
            Assert.Equal("25", value);
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    [Fact]
    public void GetLastValueFromConfigFiles_LocalService_ParsesRealExample()
    {
        var dir = Path.Combine(Path.GetTempPath(), "dnsmasq-localsvc-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        var conf = Path.Combine(dir, "dnsmasq.conf");
        try
        {
            File.WriteAllText(conf, "local-service=host\n");
            var (value, _) = DnsmasqConfIncludeParser.GetLastValueFromConfigFiles(new[] { conf }, DnsmasqConfKeys.LocalService);
            Assert.Equal("host", value);
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    [Fact]
    public void GetLastValueFromConfigFiles_DhcpLeaseMax_ParsesRealExample()
    {
        var dir = Path.Combine(Path.GetTempPath(), "dnsmasq-leasemax-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        var conf = Path.Combine(dir, "dnsmasq.conf");
        try
        {
            File.WriteAllText(conf, "dhcp-lease-max=150\n");
            var (value, _) = DnsmasqConfIncludeParser.GetLastValueFromConfigFiles(new[] { conf }, DnsmasqConfKeys.DhcpLeaseMax);
            Assert.Equal("150", value);
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    [Fact]
    public void GetLastValueFromConfigFiles_NegTtl_ParsesRealExample()
    {
        var dir = Path.Combine(Path.GetTempPath(), "dnsmasq-negttl-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        var conf = Path.Combine(dir, "dnsmasq.conf");
        try
        {
            File.WriteAllText(conf, "neg-ttl=60\n");
            var (value, _) = DnsmasqConfIncludeParser.GetLastValueFromConfigFiles(new[] { conf }, DnsmasqConfKeys.NegTtl);
            Assert.Equal("60", value);
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    [Fact]
    public void GetLastValueFromConfigFiles_MaxTtl_ParsesRealExample()
    {
        var dir = Path.Combine(Path.GetTempPath(), "dnsmasq-maxttl-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        var conf = Path.Combine(dir, "dnsmasq.conf");
        try
        {
            File.WriteAllText(conf, "max-ttl=3600\n");
            var (value, _) = DnsmasqConfIncludeParser.GetLastValueFromConfigFiles(new[] { conf }, DnsmasqConfKeys.MaxTtl);
            Assert.Equal("3600", value);
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    [Fact]
    public void GetLastValueFromConfigFiles_MaxCacheTtl_ParsesRealExample()
    {
        var dir = Path.Combine(Path.GetTempPath(), "dnsmasq-maxcttl-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        var conf = Path.Combine(dir, "dnsmasq.conf");
        try
        {
            File.WriteAllText(conf, "max-cache-ttl=86400\n");
            var (value, _) = DnsmasqConfIncludeParser.GetLastValueFromConfigFiles(new[] { conf }, DnsmasqConfKeys.MaxCacheTtl);
            Assert.Equal("86400", value);
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    [Fact]
    public void GetLastValueFromConfigFiles_MinCacheTtl_ParsesRealExample()
    {
        var dir = Path.Combine(Path.GetTempPath(), "dnsmasq-mincttl-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        var conf = Path.Combine(dir, "dnsmasq.conf");
        try
        {
            File.WriteAllText(conf, "min-cache-ttl=60\n");
            var (value, _) = DnsmasqConfIncludeParser.GetLastValueFromConfigFiles(new[] { conf }, DnsmasqConfKeys.MinCacheTtl);
            Assert.Equal("60", value);
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    [Fact]
    public void GetLastValueFromConfigFiles_DhcpTtl_ParsesRealExample()
    {
        var dir = Path.Combine(Path.GetTempPath(), "dnsmasq-dhcpttl-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        var conf = Path.Combine(dir, "dnsmasq.conf");
        try
        {
            File.WriteAllText(conf, "dhcp-ttl=0\n");
            var (value, _) = DnsmasqConfIncludeParser.GetLastValueFromConfigFiles(new[] { conf }, DnsmasqConfKeys.DhcpTtl);
            Assert.Equal("0", value);
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }
}
