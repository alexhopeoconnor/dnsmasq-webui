using DnsmasqWebUI.Infrastructure.Helpers.Config;
using DnsmasqWebUI.Models.Dnsmasq;
using DnsmasqWebUI.Models.Dnsmasq.EffectiveConfig;
using DnsmasqWebUI.Infrastructure.Parsers;

namespace DnsmasqWebUI.Tests;


/// <summary>Config discovery: conf-file=, conf-dir=, GetIncludedPaths, GetFirstConfDir, GetIncludedPathsWithSource, conf-dir suffix.</summary>
public class DnsmasqConfIncludeParserIncludeTests
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

    /// <summary>Direct test for EnumerateConfigLinesInDnsmasqOrder: main has line1, conf-file=extra, line2; extra has extra1. Asserts interleaving order.</summary>
    [Fact]
    public void EnumerateConfigLinesInDnsmasqOrder_ConfFileInMiddle_InterleavesMainThenIncludedThenMain()
    {
        var dir = Path.Combine(Path.GetTempPath(), "dnsmasq-enum-order-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        var main = Path.Combine(dir, "dnsmasq.conf");
        var extra = Path.Combine(dir, "extra.conf");
        try
        {
            File.WriteAllText(main, "line1\nconf-file=extra.conf\nline2\n");
            File.WriteAllText(extra, "extra1\n");
            var mainFull = Path.GetFullPath(main);
            var extraFull = Path.GetFullPath(extra);
            var mainDir = Path.GetDirectoryName(mainFull) ?? "";
            var seq = DnsmasqConfIncludeParser.EnumerateConfigLinesInDnsmasqOrder(mainFull, mainDir, DnsmasqConfFileSource.Main, null).ToList();
            Assert.Equal(5, seq.Count);
            Assert.Equal((mainFull, 0, "", DnsmasqConfFileSource.Main), seq[0]);
            Assert.Equal((mainFull, 1, "line1", DnsmasqConfFileSource.Main), seq[1]);
            Assert.Equal((extraFull, 0, "", DnsmasqConfFileSource.ConfFile), seq[2]);
            Assert.Equal((extraFull, 1, "extra1", DnsmasqConfFileSource.ConfFile), seq[3]);
            Assert.Equal((mainFull, 3, "line2", DnsmasqConfFileSource.Main), seq[4]);
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }
}
