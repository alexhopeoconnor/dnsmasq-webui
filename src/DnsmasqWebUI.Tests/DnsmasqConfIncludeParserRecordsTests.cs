using DnsmasqWebUI.Infrastructure.Helpers.Config;
using DnsmasqWebUI.Models.Dnsmasq;
using DnsmasqWebUI.Infrastructure.Parsers;

namespace DnsmasqWebUI.Tests;


/// <summary>DNS records and local names: hostsdir, domain, cname, mx-host, srv, ptr/txt/naptr/host-record, dynamic-host, interface-name.</summary>
public class DnsmasqConfIncludeParserRecordsTests
{
    // --- One test per option: key wired, when set returns value(s) ---

    [Fact]
    public void GetLastValueFromConfigFiles_Hostsdir_WhenSet_ReturnsValue()
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
    public void GetMultiValueFromConfigFiles_Domain_WhenSet_ReturnsValues()
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
    public void GetMultiValueFromConfigFiles_Cname_WhenSet_ReturnsValues()
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
    public void GetMultiValueFromConfigFiles_MxHost_WhenSet_ReturnsValues()
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
    public void GetMultiValueFromConfigFiles_Srv_WhenSet_ReturnsValues()
    {
        var dir = Path.Combine(Path.GetTempPath(), "dnsmasq-srv-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        var conf = Path.Combine(dir, "dnsmasq.conf");
        var line = "_http._tcp.example.com,server.example.com,80";
        try
        {
            File.WriteAllText(conf, $"srv-host={line}\n");
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
    public void GetMultiValueFromConfigFiles_PtrRecord_WhenSet_ReturnsValues()
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
    public void GetMultiValueFromConfigFiles_TxtRecord_WhenSet_ReturnsValues()
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
    public void GetMultiValueFromConfigFiles_NaptrRecord_WhenSet_ReturnsValues()
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
    public void GetMultiValueFromConfigFiles_HostRecord_WhenSet_ReturnsValues()
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
    public void GetMultiValueFromConfigFiles_DynamicHost_WhenSet_ReturnsValues()
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
    public void GetMultiValueFromConfigFiles_InterfaceName_WhenSet_ReturnsValues()
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
}
