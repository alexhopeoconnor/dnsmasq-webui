using DnsmasqWebUI.Infrastructure.Helpers.Config;
using DnsmasqWebUI.Models.Dnsmasq;
using DnsmasqWebUI.Infrastructure.Parsers;

namespace DnsmasqWebUI.Tests;

/// <summary>
/// Positive parser tests for added options: key wired and "option present" behaviour.
/// Ensures key names in DnsmasqConfKeys match what the parser expects (complement to OfficialExampleTests which assert "all commented → false/empty").
/// Covers process flags (keep-in-foreground, no-daemon), DHCP include paths (dhcp-hostsfile, dhcp-optsfile, dhcp-hostsdir), proxy-dnssec, conntrack.
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

    [Fact]
    public void GetFlagFromConfigFiles_KeepInForeground_WhenPresent_ReturnsTrue()
    {
        WriteFlagAndAssertTrue(DnsmasqConfKeys.KeepInForeground);
    }

    [Fact]
    public void GetFlagFromConfigFiles_NoDaemon_WhenPresent_ReturnsTrue()
    {
        WriteFlagAndAssertTrue(DnsmasqConfKeys.NoDaemon);
    }

    [Fact]
    public void GetFlagFromConfigFiles_ProxyDnssec_WhenPresent_ReturnsTrue()
    {
        WriteFlagAndAssertTrue(DnsmasqConfKeys.ProxyDnssec);
    }

    [Fact]
    public void GetFlagFromConfigFiles_ConnmarkAllowlistEnable_WhenPresent_ReturnsTrue()
    {
        WriteFlagAndAssertTrue(DnsmasqConfKeys.ConnmarkAllowlistEnable);
    }

    [Fact]
    public void GetFlagFromConfigFiles_NoRoundRobin_WhenPresent_ReturnsTrue()
    {
        WriteFlagAndAssertTrue(DnsmasqConfKeys.NoRoundRobin);
    }

    [Fact]
    public void GetFlagFromConfigFiles_DnssecNoTimecheck_WhenPresent_ReturnsTrue()
    {
        WriteFlagAndAssertTrue(DnsmasqConfKeys.DnssecNoTimecheck);
    }

    [Fact]
    public void GetFlagFromConfigFiles_DnssecDebug_WhenPresent_ReturnsTrue()
    {
        WriteFlagAndAssertTrue(DnsmasqConfKeys.DnssecDebug);
    }

    [Fact]
    public void GetFlagFromConfigFiles_Leasequery_WhenPresent_ReturnsTrue()
    {
        WriteFlagAndAssertTrue(DnsmasqConfKeys.Leasequery);
    }

    [Fact]
    public void GetFlagFromConfigFiles_QuietDhcp_WhenPresent_ReturnsTrue()
    {
        WriteFlagAndAssertTrue(DnsmasqConfKeys.QuietDhcp);
    }

    [Fact]
    public void GetFlagFromConfigFiles_QuietTftp_WhenPresent_ReturnsTrue()
    {
        WriteFlagAndAssertTrue(DnsmasqConfKeys.QuietTftp);
    }

    [Fact]
    public void GetFlagFromConfigFiles_DhcpGenerateNames_WhenPresent_ReturnsTrue()
    {
        WriteFlagAndAssertTrue(DnsmasqConfKeys.DhcpGenerateNames);
    }

    [Fact]
    public void GetFlagFromConfigFiles_DhcpBroadcast_WhenPresent_ReturnsTrue()
    {
        WriteFlagAndAssertTrue(DnsmasqConfKeys.DhcpBroadcast);
    }

    [Fact]
    public void GetFlagFromConfigFiles_DhcpSequentialIp_WhenPresent_ReturnsTrue()
    {
        WriteFlagAndAssertTrue(DnsmasqConfKeys.DhcpSequentialIp);
    }

    [Fact]
    public void GetFlagFromConfigFiles_DhcpIgnoreClid_WhenPresent_ReturnsTrue()
    {
        WriteFlagAndAssertTrue(DnsmasqConfKeys.DhcpIgnoreClid);
    }

    [Fact]
    public void GetFlagFromConfigFiles_BootpDynamic_WhenPresent_ReturnsTrue()
    {
        WriteFlagAndAssertTrue(DnsmasqConfKeys.BootpDynamic);
    }

    [Fact]
    public void GetFlagFromConfigFiles_NoPing_WhenPresent_ReturnsTrue()
    {
        WriteFlagAndAssertTrue(DnsmasqConfKeys.NoPing);
    }

    [Fact]
    public void GetFlagFromConfigFiles_ScriptArp_WhenPresent_ReturnsTrue()
    {
        WriteFlagAndAssertTrue(DnsmasqConfKeys.ScriptArp);
    }

    [Fact]
    public void GetFlagFromConfigFiles_ScriptOnRenewal_WhenPresent_ReturnsTrue()
    {
        WriteFlagAndAssertTrue(DnsmasqConfKeys.ScriptOnRenewal);
    }

    [Fact]
    public void GetFlagFromConfigFiles_DhcpNoOverride_WhenPresent_ReturnsTrue()
    {
        WriteFlagAndAssertTrue(DnsmasqConfKeys.DhcpNoOverride);
    }

    [Fact]
    public void GetFlagFromConfigFiles_QuietDhcp6_WhenPresent_ReturnsTrue()
    {
        WriteFlagAndAssertTrue(DnsmasqConfKeys.QuietDhcp6);
    }

    [Fact]
    public void GetFlagFromConfigFiles_QuietRa_WhenPresent_ReturnsTrue()
    {
        WriteFlagAndAssertTrue(DnsmasqConfKeys.QuietRa);
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

    [Fact]
    public void GetMultiValueFromConfigFiles_DhcpHostsfile_WhenSet_ReturnsValues()
    {
        var dir = CreateTempDir();
        var conf = Path.Combine(dir, "dnsmasq.conf");
        var path = "/etc/dnsmasq.d/hosts.d";
        try
        {
            File.WriteAllText(conf, $"dhcp-hostsfile={path}\n");
            var result = DnsmasqConfIncludeParser.GetMultiValueFromConfigFiles(new[] { conf }, DnsmasqConfKeys.DhcpHostsfile);
            Assert.Single(result);
            Assert.Equal(path, result[0]);
        }
        finally { Directory.Delete(dir, recursive: true); }
    }

    [Fact]
    public void GetMultiValueFromConfigFiles_DhcpOptsfile_WhenSet_ReturnsValues()
    {
        var dir = CreateTempDir();
        var conf = Path.Combine(dir, "dnsmasq.conf");
        var path = "/etc/dnsmasq.d/opts.conf";
        try
        {
            File.WriteAllText(conf, $"dhcp-optsfile={path}\n");
            var result = DnsmasqConfIncludeParser.GetMultiValueFromConfigFiles(new[] { conf }, DnsmasqConfKeys.DhcpOptsfile);
            Assert.Single(result);
            Assert.Equal(path, result[0]);
        }
        finally { Directory.Delete(dir, recursive: true); }
    }

    [Fact]
    public void GetMultiValueFromConfigFiles_DhcpHostsdir_WhenSet_ReturnsValues()
    {
        var dir = CreateTempDir();
        var conf = Path.Combine(dir, "dnsmasq.conf");
        var path = "/etc/dnsmasq.d/hosts.d";
        try
        {
            File.WriteAllText(conf, $"dhcp-hostsdir={path}\n");
            var result = DnsmasqConfIncludeParser.GetMultiValueFromConfigFiles(new[] { conf }, DnsmasqConfKeys.DhcpHostsdir);
            Assert.Single(result);
            Assert.Equal(path, result[0]);
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

    [Fact]
    public void GetFlagFromConfigFiles_Conntrack_WhenPresent_ReturnsTrue()
    {
        var dir = CreateTempDir();
        var conf = Path.Combine(dir, "dnsmasq.conf");
        try
        {
            File.WriteAllText(conf, "conntrack\n");
            Assert.True(DnsmasqConfIncludeParser.GetFlagFromConfigFiles(new[] { conf }, DnsmasqConfKeys.Conntrack));
        }
        finally { Directory.Delete(dir, recursive: true); }
    }

    [Fact]
    public void GetFlagFromConfigFiles_Conntrack_WhenAbsent_ReturnsFalse()
    {
        var dir = CreateTempDir();
        var conf = Path.Combine(dir, "dnsmasq.conf");
        try
        {
            File.WriteAllText(conf, "port=53\n");
            Assert.False(DnsmasqConfIncludeParser.GetFlagFromConfigFiles(new[] { conf }, DnsmasqConfKeys.Conntrack));
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
