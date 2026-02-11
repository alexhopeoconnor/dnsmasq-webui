using DnsmasqWebUI.Infrastructure.Helpers.Config;
using DnsmasqWebUI.Models.Dnsmasq;
using DnsmasqWebUI.Infrastructure.Parsers;

namespace DnsmasqWebUI.Tests;


/// <summary>Server/local, rev-server, dhcp-range, all-servers, log-queries.</summary>
public class DnsmasqConfIncludeParserServerAndResolvTests
{

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
