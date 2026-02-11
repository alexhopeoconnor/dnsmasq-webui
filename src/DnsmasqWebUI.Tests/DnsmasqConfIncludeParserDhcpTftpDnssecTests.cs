using DnsmasqWebUI.Infrastructure.Helpers.Config;
using DnsmasqWebUI.Models.Dnsmasq;
using DnsmasqWebUI.Infrastructure.Parsers;

namespace DnsmasqWebUI.Tests;


/// <summary>DHCP advanced, TFTP/PXE, DNSSEC, enable-dbus/ubus, fast-dns-retry.</summary>
public class DnsmasqConfIncludeParserDhcpTftpDnssecTests
{
    // --- One test per option: key wired, when set returns value(s) ---

    [Fact]
    public void GetMultiValueFromConfigFiles_DhcpMatch_WhenSet_ReturnsValues()
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
    public void GetMultiValueFromConfigFiles_DhcpBoot_WhenSet_ReturnsValues()
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
    public void GetMultiValueFromConfigFiles_DhcpIgnore_WhenSet_ReturnsValues()
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
    public void GetMultiValueFromConfigFiles_DhcpVendorclass_WhenSet_ReturnsValues()
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
    public void GetMultiValueFromConfigFiles_DhcpUserclass_WhenSet_ReturnsValues()
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
    public void GetMultiValueFromConfigFiles_RaParam_WhenSet_ReturnsValues()
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
    public void GetMultiValueFromConfigFiles_Slaac_WhenSet_ReturnsValues()
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
    public void GetLastValueFromConfigFiles_TftpRoot_WhenSet_ReturnsValue()
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
    public void GetMultiValueFromConfigFiles_PxeService_WhenSet_ReturnsValues()
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
    public void GetLastValueFromConfigFiles_PxePrompt_WhenSet_ReturnsValue()
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
    public void GetMultiValueFromConfigFiles_TrustAnchor_WhenSet_ReturnsValues()
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
    public void GetLastValueFromConfigFiles_EnableDbus_WhenSet_ReturnsValue()
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
    public void GetLastValueFromConfigFiles_EnableUbus_WhenSet_ReturnsValue()
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
    public void GetLastValueFromConfigFiles_FastDnsRetry_WhenSet_ReturnsValue()
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
}
