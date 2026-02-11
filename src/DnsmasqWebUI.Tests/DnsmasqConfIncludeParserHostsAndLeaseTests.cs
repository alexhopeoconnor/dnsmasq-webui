using DnsmasqWebUI.Infrastructure.Helpers.Config;
using DnsmasqWebUI.Models.Dnsmasq;
using DnsmasqWebUI.Infrastructure.Parsers;

namespace DnsmasqWebUI.Tests;


/// <summary>Hosts (addn-hosts, hostsdir) and DHCP lease file parsing.</summary>
public class DnsmasqConfIncludeParserHostsAndLeaseTests
{
    [Fact]
    public void GetDhcpLeaseFilePathFromConfigFiles_FromTestdata_ReturnsLeasesPath()
    {
        var mainPath = TestDataHelper.GetPath("dnsmasq-test.conf");
        Assert.True(File.Exists(mainPath), "Testdata dnsmasq-test.conf required");
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
        Assert.True(File.Exists(mainPath), "Testdata dnsmasq-test.conf required");
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
}
