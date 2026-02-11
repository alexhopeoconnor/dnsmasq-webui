using DnsmasqWebUI.Infrastructure.Helpers.Config;
using DnsmasqWebUI.Models.Dnsmasq;
using DnsmasqWebUI.Infrastructure.Parsers;

namespace DnsmasqWebUI.Tests;


/// <summary>Interfaces, auth-server, DHCP/option/resolv multi-value, rebind/bogus/ignore/alias, filter-rr, cache-rr.</summary>
public class DnsmasqConfIncludeParserInterfacesAndFilteringTests
{
    // --- Additional tests: one per conf field so every option has a documented example test ---

    [Fact]
    public void GetMultiValueFromConfigFiles_Interface_WhenSet_ReturnsValues()
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
    public void GetMultiValueFromConfigFiles_ExceptInterface_WhenSet_ReturnsValues()
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
    public void GetMultiValueFromConfigFiles_NoDhcpInterface_WhenSet_ReturnsValues()
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
    public void GetMultiValueFromConfigFiles_NoDhcpv4Interface_WhenSet_ReturnsValues()
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
    public void GetMultiValueFromConfigFiles_NoDhcpv6Interface_WhenSet_ReturnsValues()
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
    public void GetMultiValueFromConfigFiles_AuthServer_WhenSet_ReturnsValues()
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
    public void GetMultiValueFromConfigFiles_DhcpHost_WhenSet_ReturnsValues()
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
    public void GetMultiValueFromConfigFiles_DhcpOption_WhenSet_ReturnsValues()
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
    public void GetMultiValueFromConfigFiles_ResolvFile_WhenSet_ReturnsValues()
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
    public void GetMultiValueFromConfigFiles_RebindDomainOk_WhenSet_ReturnsValues()
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
    public void GetMultiValueFromConfigFiles_BogusNxdomain_WhenSet_ReturnsValues()
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
    public void GetMultiValueFromConfigFiles_IgnoreAddress_WhenSet_ReturnsValues()
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
    public void GetMultiValueFromConfigFiles_Alias_WhenSet_ReturnsValues()
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
    public void GetMultiValueFromConfigFiles_FilterRr_WhenSet_ReturnsValues()
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
    public void GetMultiValueFromConfigFiles_CacheRr_WhenSet_ReturnsValues()
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
}
