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
}
