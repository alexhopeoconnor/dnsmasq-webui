using DnsmasqWebUI.Infrastructure.Helpers.Config;
using DnsmasqWebUI.Models.Dnsmasq;
using DnsmasqWebUI.Infrastructure.Parsers;

namespace DnsmasqWebUI.Tests;


/// <summary>Single-value options (last wins): local-ttl, pid-file, user, group, auth-ttl, edns-packet-max, query-port, port-limit, min/max-port, log-async, local-service, dhcp-lease-max, neg/max/min-cache-ttl, dhcp-ttl.</summary>
public class DnsmasqConfIncludeParserSingleValueTests
{
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
    public void GetLastValueFromConfigFiles_LocalTtl_WhenSet_ReturnsValue()
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
    public void GetLastValueFromConfigFiles_PidFile_WhenSet_ReturnsValue()
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
    public void GetLastValueFromConfigFiles_User_WhenSet_ReturnsValue()
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
    public void GetLastValueFromConfigFiles_Group_WhenSet_ReturnsValue()
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
    public void GetLastValueFromConfigFiles_AuthTtl_WhenSet_ReturnsValue()
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
    public void GetLastValueFromConfigFiles_EdnsPacketMax_WhenSet_ReturnsValue()
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
    public void GetLastValueFromConfigFiles_QueryPort_WhenSet_ReturnsValue()
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
    public void GetLastValueFromConfigFiles_PortLimit_WhenSet_ReturnsValue()
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
    public void GetLastValueFromConfigFiles_MinPort_WhenSet_ReturnsValue()
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
    public void GetLastValueFromConfigFiles_MaxPort_WhenSet_ReturnsValue()
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
    public void GetLastValueFromConfigFiles_LogAsync_WhenSet_ReturnsValue()
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
    public void GetLastValueFromConfigFiles_LocalService_WhenSet_ReturnsValue()
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
    public void GetLastValueFromConfigFiles_DhcpLeaseMax_WhenSet_ReturnsValue()
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
    public void GetLastValueFromConfigFiles_NegTtl_WhenSet_ReturnsValue()
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
    public void GetLastValueFromConfigFiles_MaxTtl_WhenSet_ReturnsValue()
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
    public void GetLastValueFromConfigFiles_MaxCacheTtl_WhenSet_ReturnsValue()
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
    public void GetLastValueFromConfigFiles_MinCacheTtl_WhenSet_ReturnsValue()
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
    public void GetLastValueFromConfigFiles_DhcpTtl_WhenSet_ReturnsValue()
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
