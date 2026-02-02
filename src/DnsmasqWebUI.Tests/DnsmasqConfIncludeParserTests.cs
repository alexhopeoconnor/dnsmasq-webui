using DnsmasqWebUI.Parsers;

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
            var includePath = Path.Combine(dir, "extra.conf");
            File.WriteAllText(includePath, "# extra\n");
            var main = Path.Combine(dir, "dnsmasq.conf");
            File.WriteAllText(main, "conf-file=extra.conf\n");
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
        var subDir = Path.Combine(baseDir, "d");
        Directory.CreateDirectory(subDir);
        try
        {
            File.WriteAllText(Path.Combine(subDir, "zz.conf"), "");
            File.WriteAllText(Path.Combine(subDir, "aa.conf"), "");
            var main = Path.Combine(baseDir, "dnsmasq.conf");
            File.WriteAllText(main, "conf-dir=d\n");
            var paths = DnsmasqConfIncludeParser.GetIncludedPaths(main);
            Assert.Equal(3, paths.Count); // main + aa.conf + zz.conf (alphabetical)
            Assert.Equal(Path.GetFullPath(main), paths[0]);
            Assert.Equal(Path.GetFullPath(Path.Combine(subDir, "aa.conf")), paths[1]);
            Assert.Equal(Path.GetFullPath(Path.Combine(subDir, "zz.conf")), paths[2]);
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
        var subDir = Path.Combine(baseDir, "dnsmasq.d");
        Directory.CreateDirectory(subDir);
        try
        {
            var main = Path.Combine(baseDir, "dnsmasq.conf");
            File.WriteAllText(main, "conf-dir=dnsmasq.d\n");
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
    public void GetIncludedPaths_testdata_dnsmasq_conf_ReturnsMainOnlyWhenConfDirMissing()
    {
        var mainPath = TestDataHelper.GetPath("dnsmasq.conf");
        if (!File.Exists(mainPath))
            return; // testdata not copied
        var paths = DnsmasqConfIncludeParser.GetIncludedPaths(mainPath);
        Assert.Single(paths);
        Assert.Equal(Path.GetFullPath(mainPath), paths[0]);
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
        var servers = DnsmasqConfIncludeParser.GetMultiValueFromConfigFiles(paths, new[] { "server", "local" });
        var addnHosts = DnsmasqConfIncludeParser.GetAddnHostsPathsFromConfigFiles(paths);
        var addresses = DnsmasqConfIncludeParser.GetMultiValueFromConfigFiles(paths, "address");
        var listenAddrs = DnsmasqConfIncludeParser.GetMultiValueFromConfigFiles(paths, "listen-address");
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
            var first = Path.Combine(dir, "first.conf");
            File.WriteAllText(first, "dhcp-leasefile=/var/first.leases\n");
            var second = Path.Combine(dir, "second.conf");
            File.WriteAllText(second, "dhcp-leasefile=/var/second.leases\n");
            var result = DnsmasqConfIncludeParser.GetDhcpLeaseFilePathFromConfigFiles(new[] { first, second });
            Assert.Equal(Path.GetFullPath("/var/second.leases"), result);
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
            var conf = Path.Combine(dir, "dnsmasq.conf");
            File.WriteAllText(conf, "dhcp-leasefile=subdir/leases\n");
            var result = DnsmasqConfIncludeParser.GetDhcpLeaseFilePathFromConfigFiles(new[] { conf });
            Assert.Equal(Path.GetFullPath(Path.Combine(dir, "subdir", "leases")), result);
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
            var first = Path.Combine(dir, "first.conf");
            File.WriteAllText(first, "addn-hosts=/etc/hosts.d/first\n");
            var second = Path.Combine(dir, "second.conf");
            File.WriteAllText(second, "addn-hosts=/data/hosts\naddn-hosts=/data/extra.hosts\n");
            var result = DnsmasqConfIncludeParser.GetAddnHostsPathsFromConfigFiles(new[] { first, second });
            Assert.Equal(3, result.Count);
            Assert.Equal(Path.GetFullPath("/etc/hosts.d/first"), result[0]);
            Assert.Equal(Path.GetFullPath("/data/hosts"), result[1]);
            Assert.Equal(Path.GetFullPath("/data/extra.hosts"), result[2]);
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
            var conf = Path.Combine(dir, "dnsmasq.conf");
            File.WriteAllText(conf, "addn-hosts=hosts.d/app.hosts\n");
            var result = DnsmasqConfIncludeParser.GetAddnHostsPathsFromConfigFiles(new[] { conf });
            Assert.Single(result);
            Assert.Equal(Path.GetFullPath(Path.Combine(dir, "hosts.d", "app.hosts")), result[0]);
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
            var result = DnsmasqConfIncludeParser.GetFlagFromConfigFiles(new[] { conf }, "expand-hosts");
            Assert.True(result);
            Assert.True(DnsmasqConfIncludeParser.GetFlagFromConfigFiles(new[] { conf }, "no-resolv"));
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
            var result = DnsmasqConfIncludeParser.GetFlagFromConfigFiles(new[] { conf }, "no-hosts");
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
        try
        {
            File.WriteAllText(f1, "cache-size=100\n");
            File.WriteAllText(f2, "cache-size=200\n");
            var (value, configDir) = DnsmasqConfIncludeParser.GetLastValueFromConfigFiles(new[] { f1, f2 }, "cache-size");
            Assert.Equal("200", value);
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
            var conf = Path.Combine(dir, "dnsmasq.conf");
            File.WriteAllText(conf, "log-facility=/data/dnsmasq.log\n");
            var (value, configDir) = DnsmasqConfIncludeParser.GetLastValueFromConfigFiles(new[] { conf }, "log-facility");
            Assert.Equal("/data/dnsmasq.log", value);
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
        try
        {
            File.WriteAllText(f1, "log-facility=/var/log/dnsmasq.log\n");
            File.WriteAllText(f2, "log-facility=/data/dnsmasq.log\n");
            var (value, _) = DnsmasqConfIncludeParser.GetLastValueFromConfigFiles(new[] { f1, f2 }, "log-facility");
            Assert.Equal("/data/dnsmasq.log", value);
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
        var result = DnsmasqConfIncludeParser.ResolvePath("sub/pid.pid", dir);
        Assert.Equal(Path.GetFullPath(Path.Combine(dir, "sub", "pid.pid")), result);
    }

    [Fact]
    public void ResolvePath_Absolute_ReturnsAsIs()
    {
        var abs = Path.Combine(Path.GetTempPath(), "absolute.pid");
        var result = DnsmasqConfIncludeParser.ResolvePath(abs, "/some/dir");
        Assert.Equal(Path.GetFullPath(abs), result);
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
            var conf = Path.Combine(dir, "dnsmasq.conf");
            File.WriteAllText(conf, "expand-hosts=1\n");
            var result = DnsmasqConfIncludeParser.GetFlagFromConfigFiles(new[] { conf }, "expand-hosts");
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
            var conf = Path.Combine(dir, "dnsmasq.conf");
            File.WriteAllText(conf, "Port=53\n");
            var (value, _) = DnsmasqConfIncludeParser.GetLastValueFromConfigFiles(new[] { conf }, "port");
            Assert.Null(value);
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
        try
        {
            File.WriteAllText(main, "port=53\nconf-file=extra.conf\nport=5353\n");
            File.WriteAllText(extra, "port=54\n");
            var paths = DnsmasqConfIncludeParser.GetIncludedPaths(main);
            var (value, _) = DnsmasqConfIncludeParser.GetLastValueFromConfigFiles(paths, "port");
            Assert.Equal("54", value);
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
        try
        {
            File.WriteAllText(Path.Combine(subDir, "a.conf"), "");
            File.WriteAllText(Path.Combine(subDir, "b.txt"), "");
            File.WriteAllText(Path.Combine(subDir, "c.conf"), "");
            var main = Path.Combine(baseDir, "dnsmasq.conf");
            File.WriteAllText(main, "conf-dir=d,*.conf\n");
            var paths = DnsmasqConfIncludeParser.GetIncludedPaths(main);
            Assert.Equal(3, paths.Count);
            Assert.Contains(paths, p => p.EndsWith("a.conf", StringComparison.Ordinal));
            Assert.Contains(paths, p => p.EndsWith("c.conf", StringComparison.Ordinal));
            Assert.DoesNotContain(paths, p => p.EndsWith("b.txt", StringComparison.Ordinal));
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
        try
        {
            File.WriteAllText(Path.Combine(subDir, "a.conf"), "");
            File.WriteAllText(Path.Combine(subDir, "b.conf.bak"), "");
            var main = Path.Combine(baseDir, "dnsmasq.conf");
            File.WriteAllText(main, "conf-dir=d,.bak\n");
            var paths = DnsmasqConfIncludeParser.GetIncludedPaths(main);
            Assert.Equal(2, paths.Count);
            Assert.Contains(paths, p => p.EndsWith("a.conf", StringComparison.Ordinal));
            Assert.DoesNotContain(paths, p => p.EndsWith("b.conf.bak", StringComparison.Ordinal));
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
        try
        {
            File.WriteAllText(f1, "addn-hosts=/etc/hosts.a\n");
            File.WriteAllText(f2, "addn-hosts=/etc/hosts.b\naddn-hosts=/etc/hosts.c\n");
            var result = DnsmasqConfIncludeParser.GetAddnHostsPathsFromConfigFilesWithSource(new[] { f1, f2 }, managedPath);
            Assert.Equal(3, result.Count);
            Assert.Equal(Path.GetFullPath("/etc/hosts.a"), result[0].Path);
            Assert.Equal(Path.GetFileName(f1), result[0].Source.FileName);
            Assert.False(result[0].Source.IsManaged);
            Assert.Equal(Path.GetFullPath("/etc/hosts.b"), result[1].Path);
            Assert.Equal(Path.GetFileName(f2), result[1].Source.FileName);
            Assert.Equal(Path.GetFullPath("/etc/hosts.c"), result[2].Path);
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
        try
        {
            File.WriteAllText(mainPath, "addn-hosts=/other/hosts\n");
            File.WriteAllText(managedPath, "addn-hosts=/managed/hosts\n");
            var result = DnsmasqConfIncludeParser.GetAddnHostsPathsFromConfigFilesWithSource(new[] { mainPath, managedPath }, managedPath);
            Assert.Equal(2, result.Count);
            Assert.False(result[0].Source.IsManaged);
            Assert.True(result[1].Source.IsManaged);
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
        try
        {
            File.WriteAllText(f1, "server=1.1.1.1\nlocal=/local/\n");
            File.WriteAllText(f2, "server=/example.com/192.168.1.1\n");
            var result = DnsmasqConfIncludeParser.GetMultiValueFromConfigFiles(new[] { f1, f2 }, new[] { "server", "local" });
            Assert.Equal(3, result.Count);
            Assert.Equal("1.1.1.1", result[0]);
            Assert.Equal("/local/", result[1]);
            Assert.Equal("/example.com/192.168.1.1", result[2]);
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
        try
        {
            File.WriteAllText(conf, "dhcp-range=172.28.0.10,172.28.0.50,12h\ndhcp-range=192.168.1.10,192.168.1.100,255.255.255.0,24h\n");
            var result = DnsmasqConfIncludeParser.GetMultiValueFromConfigFiles(new[] { conf }, "dhcp-range");
            Assert.Equal(2, result.Count);
            Assert.Equal("172.28.0.10,172.28.0.50,12h", result[0]);
            Assert.Equal("192.168.1.10,192.168.1.100,255.255.255.0,24h", result[1]);
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }
}
