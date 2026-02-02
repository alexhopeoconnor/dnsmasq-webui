using DnsmasqWebUI.Models.Dhcp;
using DnsmasqWebUI.Parsers;

namespace DnsmasqWebUI.Tests;

/// <summary>
/// Tests for DnsmasqConfDhcpHostLineParser. Format: dhcp-host=[hwaddr][,id:...][,set:tag][,tag:tag][,ip][,hostname][,lease][,ignore]
/// Comma-separated; ## = deleted, # = comment; trailing # comment allowed.
/// </summary>
public class DnsmasqConfDhcpHostLineParserTests
{
    [Fact]
    public void ParseLine_NotDhcpHost_ReturnsNull()
    {
        Assert.Null(DnsmasqConfDhcpHostLineParser.ParseLine("domain=local", 1));
        Assert.Null(DnsmasqConfDhcpHostLineParser.ParseLine("dhcp-range=192.168.1.1,192.168.1.100", 1));
        Assert.Null(DnsmasqConfDhcpHostLineParser.ParseLine("# comment", 1));
    }

    [Fact]
    public void ParseLine_BasicMacIpNameLease_Parses()
    {
        var e = DnsmasqConfDhcpHostLineParser.ParseLine("dhcp-host=aa:bb:cc:dd:ee:ff,192.168.1.10,testpc,infinite", 1);
        Assert.NotNull(e);
        Assert.False(e!.IsComment);
        Assert.False(e.IsDeleted);
        Assert.Single(e.MacAddresses);
        Assert.Equal("aa:bb:cc:dd:ee:ff", e.MacAddresses[0]);
        Assert.Equal("192.168.1.10", e.Address);
        Assert.Equal("testpc", e.Name);
        Assert.Equal("infinite", e.Lease);
        Assert.Empty(e.Extra);
    }

    [Fact]
    public void ParseLine_CommentedLine_ParsesWithIsComment()
    {
        var e = DnsmasqConfDhcpHostLineParser.ParseLine("#dhcp-host=aa:bb:cc:dd:ee:ff,192.168.1.10,testpc,infinite", 1);
        Assert.NotNull(e);
        Assert.True(e!.IsComment);
        Assert.False(e.IsDeleted);
        Assert.Single(e.MacAddresses);
        Assert.Equal("192.168.1.10", e.Address);
    }

    [Fact]
    public void ParseLine_DeletedLine_ParsesWithIsDeleted()
    {
        var e = DnsmasqConfDhcpHostLineParser.ParseLine("##dhcp-host=aa:bb:cc:dd:ee:ff,192.168.1.10,oldpc", 1);
        Assert.NotNull(e);
        Assert.True(e!.IsComment);
        Assert.True(e.IsDeleted);
        Assert.Single(e.MacAddresses);
        Assert.Equal("192.168.1.10", e.Address);
    }

    [Fact]
    public void ParseLine_TrailingComment_ParsesComment()
    {
        var e = DnsmasqConfDhcpHostLineParser.ParseLine("dhcp-host=11:22:33:44:55:66,192.168.1.11,laptop,infinite # main laptop", 1);
        Assert.NotNull(e);
        Assert.Equal("main laptop", e!.Comment);
        Assert.Single(e.MacAddresses);
    }

    [Fact]
    public void ParseLine_MultipleMacs_SameIp_Parses()
    {
        var e = DnsmasqConfDhcpHostLineParser.ParseLine("dhcp-host=11:22:33:44:55:66,12:34:56:78:90:12,192.168.0.2", 1);
        Assert.NotNull(e);
        Assert.Equal(2, e!.MacAddresses.Count);
        Assert.Equal("11:22:33:44:55:66", e.MacAddresses[0]);
        Assert.Equal("12:34:56:78:90:12", e.MacAddresses[1]);
        Assert.Equal("192.168.0.2", e.Address);
    }

    [Fact]
    public void ParseLine_WithSetTag_PutsInExtra()
    {
        var e = DnsmasqConfDhcpHostLineParser.ParseLine("dhcp-host=AA:BB:CC:DD:CC:BB,redhost1,192.168.1.41,infinite,set:red", 1);
        Assert.NotNull(e);
        Assert.Equal("192.168.1.41", e!.Address);
        Assert.Equal("redhost1", e.Name);
        Assert.Contains(e.Extra, x => x == "set:red");
    }

    [Fact]
    public void ParseLine_HostnameOnly_Parses()
    {
        var e = DnsmasqConfDhcpHostLineParser.ParseLine("dhcp-host=lap,192.168.0.199", 1);
        Assert.NotNull(e);
        Assert.Equal("lap", e!.Name);
        Assert.Equal("192.168.0.199", e.Address);
        Assert.Empty(e.MacAddresses);
    }

    [Fact]
    public void ParseLine_Ignore_Parses()
    {
        var e = DnsmasqConfDhcpHostLineParser.ParseLine("dhcp-host=00:20:e0:3b:13:af,ignore", 1);
        Assert.NotNull(e);
        Assert.True(e!.Ignore);
        Assert.Single(e.MacAddresses);
        Assert.Equal("00:20:e0:3b:13:af", e.MacAddresses[0]);
    }

    [Fact]
    public void ParseLine_NumericLease_Parses()
    {
        var e = DnsmasqConfDhcpHostLineParser.ParseLine("dhcp-host=aa:bb:cc:dd:ee:ff,192.168.1.10,pc,3600", 1);
        Assert.NotNull(e);
        Assert.Equal("3600", e!.Lease);
    }

    [Fact]
    public void ParseLine_WithIdClient_PutsInExtra()
    {
        var e = DnsmasqConfDhcpHostLineParser.ParseLine("dhcp-host=id:01:02:03:04,192.168.1.50,myhost", 1);
        Assert.NotNull(e);
        Assert.Contains(e!.Extra, x => x.StartsWith("id:", StringComparison.OrdinalIgnoreCase));
        Assert.Equal("192.168.1.50", e.Address);
    }

    [Fact]
    public void ToLine_Roundtrip_Basic()
    {
        var line = "dhcp-host=aa:bb:cc:dd:ee:ff,192.168.1.10,testpc,infinite";
        var e = DnsmasqConfDhcpHostLineParser.ParseLine(line, 1);
        Assert.NotNull(e);
        var back = DnsmasqConfDhcpHostLineParser.ToLine(e!);
        Assert.StartsWith("dhcp-host=", back);
        Assert.Contains("aa:bb:cc:dd:ee:ff", back);
        Assert.Contains("192.168.1.10", back);
        Assert.Contains("testpc", back);
        Assert.Contains("infinite", back);
    }

    [Fact]
    public void ToLine_Roundtrip_WithComment()
    {
        var line = "dhcp-host=11:22:33:44:55:66,192.168.1.11,laptop,infinite # main laptop";
        var e = DnsmasqConfDhcpHostLineParser.ParseLine(line, 1);
        Assert.NotNull(e);
        var back = DnsmasqConfDhcpHostLineParser.ToLine(e!);
        Assert.StartsWith("dhcp-host=", back);
        Assert.Contains("main laptop", back);
    }

    [Fact]
    public void ToLine_CommentedEntry_Prefix()
    {
        var e = DnsmasqConfDhcpHostLineParser.ParseLine("#dhcp-host=aa:bb:cc:dd:ee:ff,192.168.1.10,testpc", 1);
        Assert.NotNull(e);
        var back = DnsmasqConfDhcpHostLineParser.ToLine(e!);
        Assert.StartsWith("#dhcp-host=", back);
    }

    [Fact]
    public void ToLine_DeletedEntry_DoubleHash()
    {
        var e = DnsmasqConfDhcpHostLineParser.ParseLine("##dhcp-host=aa:bb:cc:dd:ee:ff,192.168.1.10,old", 1);
        Assert.NotNull(e);
        var back = DnsmasqConfDhcpHostLineParser.ToLine(e!);
        Assert.StartsWith("##dhcp-host=", back);
    }

    [Fact]
    public void Parse_testdata_dhcp_conf_ParsesDhcpHostLines()
    {
        var lines = TestDataHelper.ReadAllLines("dnsmasq.d/dhcp.conf");
        var dhcpEntries = new List<DhcpHostEntry>();
        for (var i = 0; i < lines.Length; i++)
        {
            var e = DnsmasqConfDhcpHostLineParser.ParseLine(lines[i], i + 1);
            if (e != null)
                dhcpEntries.Add(e);
        }
        Assert.Single(dhcpEntries);
        Assert.Equal("aa:bb:cc:dd:ee:ff", dhcpEntries[0].MacAddresses[0]);
        Assert.Equal("172.28.0.100", dhcpEntries[0].Address);
        Assert.Equal("testpc", dhcpEntries[0].Name);
        Assert.Equal("infinite", dhcpEntries[0].Lease);
    }
}
