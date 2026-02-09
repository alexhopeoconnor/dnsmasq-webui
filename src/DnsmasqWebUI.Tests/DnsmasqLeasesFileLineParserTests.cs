using DnsmasqWebUI.Models.Dhcp;
using DnsmasqWebUI.Infrastructure.Parsers;

namespace DnsmasqWebUI.Tests;

/// <summary>
/// Tests for DnsmasqLeasesFileLineParser. Format per dnsmasq author (dnsmasq-discuss 2006): five space-separated
/// fields — expiry (epoch), MAC, IP, hostname (or *), client-id (or *).
/// </summary>
public class DnsmasqLeasesFileLineParserTests
{
    [Fact]
    public void ParseLine_Empty_ReturnsNull()
    {
        Assert.Null(DnsmasqLeasesFileLineParser.ParseLine(""));
        Assert.Null(DnsmasqLeasesFileLineParser.ParseLine("   "));
    }

    [Fact]
    public void ParseLine_ValidFiveFields_Parses()
    {
        long epoch = 946689575;
        string mac = "00:00:00:00:00:05";
        string address = "192.168.1.155";
        string name = "wdt";
        string clientId = "01:00:00:00:00:00:05";

        var line = $"{epoch} {mac} {address} {name} {clientId}";
        var e = DnsmasqLeasesFileLineParser.ParseLine(line);

        Assert.NotNull(e);
        Assert.Equal(epoch, e!.Epoch);
        Assert.Equal(mac, e.Mac);
        Assert.Equal(address, e.Address);
        Assert.Equal(name, e.Name);
        Assert.Equal(clientId, e.ClientId);
    }

    [Fact]
    public void ParseLine_UnknownHostname_Asterisk()
    {
        long epoch = 946689522;
        string mac = "00:00:00:00:00:04";
        string address = "192.168.1.237";
        string name = "*";
        string clientId = "01:00:00:00:00:00:04";

        var line = $"{epoch} {mac} {address} {name} {clientId}";
        var e = DnsmasqLeasesFileLineParser.ParseLine(line);

        Assert.NotNull(e);
        Assert.Equal(name, e!.Name);
        Assert.Equal(address, e.Address);
    }

    [Fact]
    public void ParseLine_UnknownClientId_Asterisk()
    {
        long epoch = 946689351;
        string mac = "00:0f:b0:3a:b5:0b";
        string address = "192.168.1.208";
        string name = "colinux";
        string clientId = "*";

        var line = $"{epoch} {mac} {address} {name} {clientId}";
        var e = DnsmasqLeasesFileLineParser.ParseLine(line);

        Assert.NotNull(e);
        Assert.Equal(name, e!.Name);
        Assert.Equal(clientId, e.ClientId);
    }

    [Fact]
    public void ParseLine_MultipleSpacesBetweenFields_Parses()
    {
        long epoch = 946689575;
        string mac = "00:00:00:00:00:05";
        string address = "192.168.1.155";
        string name = "wdt";
        string clientId = "01:00:00:00:00:00:05";

        var line = $"  {epoch}   {mac}   {address}   {name}   {clientId}  ";
        var e = DnsmasqLeasesFileLineParser.ParseLine(line);

        Assert.NotNull(e);
        Assert.Equal(epoch, e!.Epoch);
        Assert.Equal(mac, e.Mac);
        Assert.Equal(address, e.Address);
        Assert.Equal(name, e.Name);
        Assert.Equal(clientId, e.ClientId);
    }

    [Fact]
    public void ParseLine_TooFewFields_ReturnsNull()
    {
        long epoch = 946689575;
        string mac = "00:00:00:00:00:05";
        string address = "192.168.1.155";

        Assert.Null(DnsmasqLeasesFileLineParser.ParseLine($"{epoch} {mac} {address}"));
        Assert.Null(DnsmasqLeasesFileLineParser.ParseLine($"{epoch} {mac}"));
    }

    [Fact]
    public void ParseLine_TrailingJunk_ReturnsNull()
    {
        // Parser uses .End() so extra text after fifth field fails
        long epoch = 946689575;
        string mac = "00:00:00:00:00:05";
        string address = "192.168.1.155";
        string name = "wdt";
        string clientId = "01:00:00:00:00:00:05";

        var line = $"{epoch} {mac} {address} {name} {clientId} extra";
        Assert.Null(DnsmasqLeasesFileLineParser.ParseLine(line));
    }

    [Fact]
    public void ParseLine_InvalidEpoch_ReturnsNull()
    {
        string invalidEpoch = "notanum";
        string mac = "00:00:00:00:00:05";
        string address = "192.168.1.155";
        string name = "wdt";
        string clientId = "01:00:00:00:00:00:05";

        var line = $"{invalidEpoch} {mac} {address} {name} {clientId}";
        Assert.Null(DnsmasqLeasesFileLineParser.ParseLine(line));
    }

    /// <summary>
    /// When DHCPv6 is in use, dnsmasq writes a "duid ..." line; we must skip it (first field is not numeric).
    /// </summary>
    [Fact]
    public void ParseLine_DuidLine_ReturnsNull()
    {
        Assert.Null(DnsmasqLeasesFileLineParser.ParseLine("duid 00:11:22:33:44:55"));
    }

    [Fact]
    public void Timestamp_ConvertsEpochToDateTime()
    {
        long epoch = 946689575;
        string mac = "00:00:00:00:00:05";
        string address = "192.168.1.155";
        string name = "wdt";
        string clientId = "01:00:00:00:00:00:05";

        var line = $"{epoch} {mac} {address} {name} {clientId}";
        var e = DnsmasqLeasesFileLineParser.ParseLine(line);

        Assert.NotNull(e);
        Assert.Equal(epoch, e!.Epoch);
        var dt = e.Timestamp;
        Assert.True(dt.Year >= 1999 && dt.Year <= 2000); // epoch 946689575 is ~1999-12
    }

    [Fact]
    public void Parse_testdata_leases_ParsesAllLines()
    {
        var lines = TestDataHelper.ReadAllLines("leases");
        var entries = new List<LeaseEntry>();
        foreach (var line in lines)
        {
            var e = DnsmasqLeasesFileLineParser.ParseLine(line);
            if (e != null)
                entries.Add(e);
        }
        Assert.Equal(2, entries.Count);
        Assert.Equal("aa:bb:cc:dd:ee:ff", entries[0].Mac);
        Assert.Equal("192.168.1.10", entries[0].Address);
        Assert.Equal("testpc", entries[0].Name);
        Assert.Equal("01:02:03:04:05:06", entries[0].ClientId);
        Assert.Equal("11:22:33:44:55:66", entries[1].Mac);
        Assert.Equal("192.168.1.11", entries[1].Address);
        Assert.Equal("*", entries[1].Name);
        Assert.Equal("*", entries[1].ClientId);
    }
}
