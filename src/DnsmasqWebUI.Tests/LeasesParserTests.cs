using DnsmasqWebUI.Models;
using DnsmasqWebUI.Parsers;

namespace DnsmasqWebUI.Tests;

/// <summary>
/// Tests for LeasesParser. Format per dnsmasq author (dnsmasq-discuss 2006): five space-separated
/// fields — expiry (epoch), MAC, IP, hostname (or *), client-id (or *).
/// </summary>
public class LeasesParserTests
{
    [Fact]
    public void ParseLine_Empty_ReturnsNull()
    {
        Assert.Null(LeasesParser.ParseLine(""));
        Assert.Null(LeasesParser.ParseLine("   "));
    }

    [Fact]
    public void ParseLine_ValidFiveFields_Parses()
    {
        var e = LeasesParser.ParseLine("946689575 00:00:00:00:00:05 192.168.1.155 wdt 01:00:00:00:00:00:05");
        Assert.NotNull(e);
        Assert.Equal(946689575, e!.Epoch);
        Assert.Equal("00:00:00:00:00:05", e.Mac);
        Assert.Equal("192.168.1.155", e.Address);
        Assert.Equal("wdt", e.Name);
        Assert.Equal("01:00:00:00:00:00:05", e.ClientId);
    }

    [Fact]
    public void ParseLine_UnknownHostname_Asterisk()
    {
        var e = LeasesParser.ParseLine("946689522 00:00:00:00:00:04 192.168.1.237 * 01:00:00:00:00:00:04");
        Assert.NotNull(e);
        Assert.Equal("*", e!.Name);
        Assert.Equal("192.168.1.237", e.Address);
    }

    [Fact]
    public void ParseLine_UnknownClientId_Asterisk()
    {
        var e = LeasesParser.ParseLine("946689351 00:0f:b0:3a:b5:0b 192.168.1.208 colinux *");
        Assert.NotNull(e);
        Assert.Equal("colinux", e!.Name);
        Assert.Equal("*", e.ClientId);
    }

    [Fact]
    public void ParseLine_MultipleSpacesBetweenFields_Parses()
    {
        var e = LeasesParser.ParseLine("  946689575   00:00:00:00:00:05   192.168.1.155   wdt   01:00:00:00:00:00:05  ");
        Assert.NotNull(e);
        Assert.Equal(946689575, e!.Epoch);
        Assert.Equal("00:00:00:00:00:05", e.Mac);
        Assert.Equal("192.168.1.155", e.Address);
        Assert.Equal("wdt", e.Name);
        Assert.Equal("01:00:00:00:00:00:05", e.ClientId);
    }

    [Fact]
    public void ParseLine_TooFewFields_ReturnsNull()
    {
        Assert.Null(LeasesParser.ParseLine("946689575 00:00:00:00:00:05 192.168.1.155"));
        Assert.Null(LeasesParser.ParseLine("946689575 00:00:00:00:00:05"));
    }

    [Fact]
    public void ParseLine_TrailingJunk_ReturnsNull()
    {
        // Parser uses .End() so extra text after fifth field fails
        Assert.Null(LeasesParser.ParseLine("946689575 00:00:00:00:00:05 192.168.1.155 wdt 01:00:00:00:00:00:05 extra"));
    }

    [Fact]
    public void ParseLine_InvalidEpoch_ReturnsNull()
    {
        Assert.Null(LeasesParser.ParseLine("notanum 00:00:00:00:00:05 192.168.1.155 wdt 01:00:00:00:00:00:05"));
    }

    /// <summary>
    /// When DHCPv6 is in use, dnsmasq writes a "duid ..." line; we must skip it (first field is not numeric).
    /// </summary>
    [Fact]
    public void ParseLine_DuidLine_ReturnsNull()
    {
        Assert.Null(LeasesParser.ParseLine("duid 00:11:22:33:44:55"));
    }

    [Fact]
    public void Timestamp_ConvertsEpochToDateTime()
    {
        var e = LeasesParser.ParseLine("946689575 00:00:00:00:00:05 192.168.1.155 wdt 01:00:00:00:00:00:05");
        Assert.NotNull(e);
        var dt = e!.Timestamp;
        Assert.True(dt.Year >= 1999 && dt.Year <= 2000); // epoch 946689575 is ~1999-12
    }

    [Fact]
    public void Parse_testdata_leases_ParsesAllLines()
    {
        var lines = TestDataHelper.ReadAllLines("leases");
        var entries = new List<LeaseEntry>();
        foreach (var line in lines)
        {
            var e = LeasesParser.ParseLine(line);
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
