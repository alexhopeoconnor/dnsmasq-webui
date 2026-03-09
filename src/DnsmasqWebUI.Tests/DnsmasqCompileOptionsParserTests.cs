using DnsmasqWebUI.Infrastructure.Helpers.Dnsmasq;
using DnsmasqWebUI.Models.Dnsmasq;

namespace DnsmasqWebUI.Tests;

/// <summary>
/// Tests for parsing "Compile time options: ..." from dnsmasq --version output.
/// </summary>
public class DnsmasqCompileOptionsParserTests
{
    [Fact]
    public void Parse_WhenNoCompileLine_ReturnsAllCapabilitiesFalse()
    {
        var result = DnsmasqCompileOptionsParser.Parse("Dnsmasq version 2.89\nCopyright ...", null);
        Assert.False(result.Dhcp);
        Assert.False(result.Tftp);
        Assert.False(result.Dnssec);
        Assert.False(result.Dbus);
        Assert.NotNull(result.RawTokens);
        Assert.Empty(result.RawTokens);
    }

    [Fact]
    public void Parse_WhenCompileLineHasDnssec_SetsDnssecTrue()
    {
        var stdout = "Dnsmasq version 2.89\nCompile time options: IPv6 GNU-getopt no-DBus no-Idn DHCP DHCPv6 TFTP DNSSEC auth";
        var result = DnsmasqCompileOptionsParser.Parse(stdout, null);
        Assert.True(result.Dnssec);
        Assert.True(result.Dhcp);
        Assert.True(result.Tftp);
        Assert.False(result.Dbus);
        Assert.Contains("DNSSEC", result.RawTokens, StringComparer.OrdinalIgnoreCase);
    }

    [Fact]
    public void Parse_WhenCompileLineHasDhcpOnly_SetsDhcpTrueOthersFalse()
    {
        var result = DnsmasqCompileOptionsParser.Parse(null, "Compile time options: DHCP no-DBus no-DNSSEC");
        Assert.True(result.Dhcp);
        Assert.False(result.Tftp);
        Assert.False(result.Dnssec);
        Assert.False(result.Dbus);
    }

    [Fact]
    public void Parse_WhenCompileLineHasDhcpv6_SetsDhcpTrue()
    {
        var result = DnsmasqCompileOptionsParser.Parse("Compile time options: DHCPv6", null);
        Assert.True(result.Dhcp);
    }

    [Fact]
    public void Parse_WhenCompileLineHasTftp_SetsTftpTrue()
    {
        var result = DnsmasqCompileOptionsParser.Parse("Compile time options: TFTP", null);
        Assert.True(result.Tftp);
    }

    [Fact]
    public void Parse_WhenCompileLineHasDbus_SetsDbusTrue()
    {
        var result = DnsmasqCompileOptionsParser.Parse("Compile time options: DBus", null);
        Assert.True(result.Dbus);
    }

    [Fact]
    public void Parse_CombinesStdoutAndStderr()
    {
        var result = DnsmasqCompileOptionsParser.Parse("Version line\n", "Compile time options: DNSSEC");
        Assert.True(result.Dnssec);
    }
}
