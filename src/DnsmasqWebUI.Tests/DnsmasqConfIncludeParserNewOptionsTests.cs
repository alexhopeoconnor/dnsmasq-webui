using DnsmasqWebUI.Infrastructure.Helpers.Config;
using DnsmasqWebUI.Models.Dnsmasq;
using DnsmasqWebUI.Infrastructure.Parsers;

namespace DnsmasqWebUI.Tests;

/// <summary>
/// Positive parser tests for the 16 added options: key wired and "option present" behaviour.
/// Ensures key names in DnsmasqConfKeys match what the parser expects (complement to OfficialExampleTests which assert "all commented → false/empty").
/// </summary>
public class DnsmasqConfIncludeParserNewOptionsTests
{
    // --- Flags (8): key-only line → GetFlag returns true ---

    [Fact]
    public void GetFlagFromConfigFiles_ReadEthers_WhenPresent_ReturnsTrue()
    {
        WriteFlagAndAssertTrue(DnsmasqConfKeys.ReadEthers);
    }

    [Fact]
    public void GetFlagFromConfigFiles_DhcpRapidCommit_WhenPresent_ReturnsTrue()
    {
        WriteFlagAndAssertTrue(DnsmasqConfKeys.DhcpRapidCommit);
    }

    [Fact]
    public void GetFlagFromConfigFiles_TftpNoFail_WhenPresent_ReturnsTrue()
    {
        WriteFlagAndAssertTrue(DnsmasqConfKeys.TftpNoFail);
    }

    [Fact]
    public void GetFlagFromConfigFiles_TftpNoBlocksize_WhenPresent_ReturnsTrue()
    {
        WriteFlagAndAssertTrue(DnsmasqConfKeys.TftpNoBlocksize);
    }

    [Fact]
    public void GetFlagFromConfigFiles_Localmx_WhenPresent_ReturnsTrue()
    {
        WriteFlagAndAssertTrue(DnsmasqConfKeys.Localmx);
    }

    [Fact]
    public void GetFlagFromConfigFiles_Selfmx_WhenPresent_ReturnsTrue()
    {
        WriteFlagAndAssertTrue(DnsmasqConfKeys.Selfmx);
    }

    [Fact]
    public void GetFlagFromConfigFiles_EnableRa_WhenPresent_ReturnsTrue()
    {
        WriteFlagAndAssertTrue(DnsmasqConfKeys.EnableRa);
    }

    [Fact]
    public void GetFlagFromConfigFiles_LogDhcp_WhenPresent_ReturnsTrue()
    {
        WriteFlagAndAssertTrue(DnsmasqConfKeys.LogDhcp);
    }

    // --- Multi-value (6): key=value line(s) → GetMultiValue returns value(s) ---

    [Fact]
    public void GetMultiValueFromConfigFiles_DhcpOptionForce_WhenSet_ReturnsValues()
    {
        var dir = CreateTempDir();
        var conf = Path.Combine(dir, "dnsmasq.conf");
        var line = "208,f1:00:74:7e";
        try
        {
            File.WriteAllText(conf, $"dhcp-option-force={line}\n");
            var result = DnsmasqConfIncludeParser.GetMultiValueFromConfigFiles(new[] { conf }, DnsmasqConfKeys.DhcpOptionForce);
            Assert.Single(result);
            Assert.Equal(line, result[0]);
        }
        finally { Directory.Delete(dir, recursive: true); }
    }

    [Fact]
    public void GetMultiValueFromConfigFiles_Ipset_WhenSet_ReturnsValues()
    {
        var dir = CreateTempDir();
        var conf = Path.Combine(dir, "dnsmasq.conf");
        var line = "/example.com/vpn,search";
        try
        {
            File.WriteAllText(conf, $"ipset={line}\n");
            var result = DnsmasqConfIncludeParser.GetMultiValueFromConfigFiles(new[] { conf }, DnsmasqConfKeys.Ipset);
            Assert.Single(result);
            Assert.Equal(line, result[0]);
        }
        finally { Directory.Delete(dir, recursive: true); }
    }

    [Fact]
    public void GetMultiValueFromConfigFiles_Nftset_WhenSet_ReturnsValues()
    {
        var dir = CreateTempDir();
        var conf = Path.Combine(dir, "dnsmasq.conf");
        var line = "/example.com/ip#test#vpn";
        try
        {
            File.WriteAllText(conf, $"nftset={line}\n");
            var result = DnsmasqConfIncludeParser.GetMultiValueFromConfigFiles(new[] { conf }, DnsmasqConfKeys.Nftset);
            Assert.Single(result);
            Assert.Equal(line, result[0]);
        }
        finally { Directory.Delete(dir, recursive: true); }
    }

    [Fact]
    public void GetMultiValueFromConfigFiles_DhcpMac_WhenSet_ReturnsValues()
    {
        var dir = CreateTempDir();
        var conf = Path.Combine(dir, "dnsmasq.conf");
        var line = "set:red,00:60:8C:*:*:*";
        try
        {
            File.WriteAllText(conf, $"dhcp-mac={line}\n");
            var result = DnsmasqConfIncludeParser.GetMultiValueFromConfigFiles(new[] { conf }, DnsmasqConfKeys.DhcpMac);
            Assert.Single(result);
            Assert.Equal(line, result[0]);
        }
        finally { Directory.Delete(dir, recursive: true); }
    }

    [Fact]
    public void GetMultiValueFromConfigFiles_DhcpNameMatch_WhenSet_ReturnsValues()
    {
        var dir = CreateTempDir();
        var conf = Path.Combine(dir, "dnsmasq.conf");
        var line = "set:wpad-ignore,wpad";
        try
        {
            File.WriteAllText(conf, $"dhcp-name-match={line}\n");
            var result = DnsmasqConfIncludeParser.GetMultiValueFromConfigFiles(new[] { conf }, DnsmasqConfKeys.DhcpNameMatch);
            Assert.Single(result);
            Assert.Equal(line, result[0]);
        }
        finally { Directory.Delete(dir, recursive: true); }
    }

    [Fact]
    public void GetMultiValueFromConfigFiles_DhcpIgnoreNames_WhenSet_ReturnsValues()
    {
        var dir = CreateTempDir();
        var conf = Path.Combine(dir, "dnsmasq.conf");
        var line = "tag:wpad-ignore";
        try
        {
            File.WriteAllText(conf, $"dhcp-ignore-names={line}\n");
            var result = DnsmasqConfIncludeParser.GetMultiValueFromConfigFiles(new[] { conf }, DnsmasqConfKeys.DhcpIgnoreNames);
            Assert.Single(result);
            Assert.Equal(line, result[0]);
        }
        finally { Directory.Delete(dir, recursive: true); }
    }

    // --- Single-value (2): key=value → GetLastValue returns value ---

    [Fact]
    public void GetLastValueFromConfigFiles_MxTarget_WhenSet_ReturnsValue()
    {
        var dir = CreateTempDir();
        var conf = Path.Combine(dir, "dnsmasq.conf");
        var value = "mail.example.com";
        try
        {
            File.WriteAllText(conf, $"mx-target={value}\n");
            var (result, _) = DnsmasqConfIncludeParser.GetLastValueFromConfigFiles(new[] { conf }, DnsmasqConfKeys.MxTarget);
            Assert.Equal(value, result);
        }
        finally { Directory.Delete(dir, recursive: true); }
    }

    [Fact]
    public void GetLastValueFromConfigFiles_DhcpScript_WhenSet_ReturnsValue()
    {
        var dir = CreateTempDir();
        var conf = Path.Combine(dir, "dnsmasq.conf");
        var value = "/usr/bin/dnsmasq-script";
        try
        {
            File.WriteAllText(conf, $"dhcp-script={value}\n");
            var (result, _) = DnsmasqConfIncludeParser.GetLastValueFromConfigFiles(new[] { conf }, DnsmasqConfKeys.DhcpScript);
            Assert.Equal(value, result);
        }
        finally { Directory.Delete(dir, recursive: true); }
    }

    private static void WriteFlagAndAssertTrue(string key)
    {
        var dir = CreateTempDir();
        var conf = Path.Combine(dir, "dnsmasq.conf");
        try
        {
            File.WriteAllText(conf, key + "\n");
            Assert.True(DnsmasqConfIncludeParser.GetFlagFromConfigFiles(new[] { conf }, key));
        }
        finally { Directory.Delete(dir, recursive: true); }
    }

    private static string CreateTempDir()
    {
        var dir = Path.Combine(Path.GetTempPath(), "dnsmasq-newopts-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        return dir;
    }
}
