using DnsmasqWebUI.Models.Dhcp;
using DnsmasqWebUI.Infrastructure.Parsers;

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
        string mac = "aa:bb:cc:dd:ee:ff";
        string address = "192.168.1.10";
        string name = "testpc";
        string lease = "infinite";

        var line = $"dhcp-host={mac},{address},{name},{lease}";
        var e = DnsmasqConfDhcpHostLineParser.ParseLine(line, 1);

        Assert.NotNull(e);
        Assert.False(e!.IsComment);
        Assert.False(e.IsDeleted);
        Assert.Single(e.MacAddresses);
        Assert.Equal(mac, e.MacAddresses[0]);
        Assert.Equal(address, e.Address);
        Assert.Equal(name, e.Name);
        Assert.Equal(lease, e.Lease);
        Assert.Empty(e.Extra);
    }

    [Fact]
    public void ParseLine_CommentedLine_ParsesWithIsComment()
    {
        string mac = "aa:bb:cc:dd:ee:ff";
        string address = "192.168.1.10";
        string name = "testpc";
        string lease = "infinite";

        var line = $"#dhcp-host={mac},{address},{name},{lease}";
        var e = DnsmasqConfDhcpHostLineParser.ParseLine(line, 1);

        Assert.NotNull(e);
        Assert.True(e!.IsComment);
        Assert.False(e.IsDeleted);
        Assert.Single(e.MacAddresses);
        Assert.Equal(address, e.Address);
    }

    [Fact]
    public void ParseLine_DeletedLine_ParsesWithIsDeleted()
    {
        string mac = "aa:bb:cc:dd:ee:ff";
        string address = "192.168.1.10";
        string name = "oldpc";

        var line = $"##dhcp-host={mac},{address},{name}";
        var e = DnsmasqConfDhcpHostLineParser.ParseLine(line, 1);

        Assert.NotNull(e);
        Assert.True(e!.IsComment);
        Assert.True(e.IsDeleted);
        Assert.Single(e.MacAddresses);
        Assert.Equal(address, e.Address);
    }

    [Fact]
    public void ParseLine_TrailingComment_ParsesComment()
    {
        string mac = "11:22:33:44:55:66";
        string address = "192.168.1.11";
        string name = "laptop";
        string lease = "infinite";
        string comment = "main laptop";

        var line = $"dhcp-host={mac},{address},{name},{lease} # {comment}";
        var e = DnsmasqConfDhcpHostLineParser.ParseLine(line, 1);

        Assert.NotNull(e);
        Assert.Equal(comment, e!.Comment);
        Assert.Single(e.MacAddresses);
    }

    [Fact]
    public void ParseLine_MultipleMacs_SameIp_Parses()
    {
        string mac1 = "11:22:33:44:55:66";
        string mac2 = "12:34:56:78:90:12";
        string address = "192.168.0.2";

        var line = $"dhcp-host={mac1},{mac2},{address}";
        var e = DnsmasqConfDhcpHostLineParser.ParseLine(line, 1);

        Assert.NotNull(e);
        Assert.Equal(2, e!.MacAddresses.Count);
        Assert.Equal(mac1, e.MacAddresses[0]);
        Assert.Equal(mac2, e.MacAddresses[1]);
        Assert.Equal(address, e.Address);
    }

    [Fact]
    public void ParseLine_WithSetTag_PutsInExtra()
    {
        string mac = "AA:BB:CC:DD:CC:BB";
        string name = "redhost1";
        string address = "192.168.1.41";
        string lease = "infinite";
        string extraTag = "set:red";

        var line = $"dhcp-host={mac},{name},{address},{lease},{extraTag}";
        var e = DnsmasqConfDhcpHostLineParser.ParseLine(line, 1);

        Assert.NotNull(e);
        Assert.Equal(address, e!.Address);
        Assert.Equal(name, e.Name);
        Assert.Contains(e.Extra, x => x == extraTag);
    }

    [Fact]
    public void ParseLine_HostnameOnly_Parses()
    {
        string name = "lap";
        string address = "192.168.0.199";

        var line = $"dhcp-host={name},{address}";
        var e = DnsmasqConfDhcpHostLineParser.ParseLine(line, 1);

        Assert.NotNull(e);
        Assert.Equal(name, e!.Name);
        Assert.Equal(address, e.Address);
        Assert.Empty(e.MacAddresses);
    }

    [Fact]
    public void ParseLine_Ignore_Parses()
    {
        string mac = "00:20:e0:3b:13:af";

        var line = $"dhcp-host={mac},ignore";
        var e = DnsmasqConfDhcpHostLineParser.ParseLine(line, 1);

        Assert.NotNull(e);
        Assert.True(e!.Ignore);
        Assert.Single(e.MacAddresses);
        Assert.Equal(mac, e.MacAddresses[0]);
    }

    [Fact]
    public void ParseLine_NumericLease_Parses()
    {
        string mac = "aa:bb:cc:dd:ee:ff";
        string address = "192.168.1.10";
        string name = "pc";
        string lease = "3600";

        var line = $"dhcp-host={mac},{address},{name},{lease}";
        var e = DnsmasqConfDhcpHostLineParser.ParseLine(line, 1);

        Assert.NotNull(e);
        Assert.Equal(lease, e!.Lease);
    }

    [Fact]
    public void ParseLine_WithIdClient_PutsInExtra()
    {
        string idPart = "id:01:02:03:04";
        string address = "192.168.1.50";
        string name = "myhost";

        var line = $"dhcp-host={idPart},{address},{name}";
        var e = DnsmasqConfDhcpHostLineParser.ParseLine(line, 1);

        Assert.NotNull(e);
        Assert.Contains(e!.Extra, x => x.StartsWith("id:", StringComparison.OrdinalIgnoreCase));
        Assert.Equal(address, e.Address);
    }

    [Fact]
    public void ToLine_Roundtrip_Basic()
    {
        string mac = "aa:bb:cc:dd:ee:ff";
        string address = "192.168.1.10";
        string name = "testpc";
        string lease = "infinite";

        var line = $"dhcp-host={mac},{address},{name},{lease}";
        var e = DnsmasqConfDhcpHostLineParser.ParseLine(line, 1);

        Assert.NotNull(e);
        var back = DnsmasqConfDhcpHostLineParser.ToLine(e!);
        Assert.StartsWith("dhcp-host=", back);
        Assert.Contains(mac, back);
        Assert.Contains(address, back);
        Assert.Contains(name, back);
        Assert.Contains(lease, back);
    }

    [Fact]
    public void ToLine_Roundtrip_WithComment()
    {
        string mac = "11:22:33:44:55:66";
        string address = "192.168.1.11";
        string name = "laptop";
        string lease = "infinite";
        string comment = "main laptop";

        var line = $"dhcp-host={mac},{address},{name},{lease} # {comment}";
        var e = DnsmasqConfDhcpHostLineParser.ParseLine(line, 1);

        Assert.NotNull(e);
        var back = DnsmasqConfDhcpHostLineParser.ToLine(e!);
        Assert.StartsWith("dhcp-host=", back);
        Assert.Contains(comment, back);
    }

    [Fact]
    public void ToLine_CommentedEntry_Prefix()
    {
        string mac = "aa:bb:cc:dd:ee:ff";
        string address = "192.168.1.10";
        string name = "testpc";

        var line = $"#dhcp-host={mac},{address},{name}";
        var e = DnsmasqConfDhcpHostLineParser.ParseLine(line, 1);

        Assert.NotNull(e);
        var back = DnsmasqConfDhcpHostLineParser.ToLine(e!);
        Assert.StartsWith("#dhcp-host=", back);
    }

    [Fact]
    public void ToLine_DeletedEntry_DoubleHash()
    {
        string mac = "aa:bb:cc:dd:ee:ff";
        string address = "192.168.1.10";
        string name = "old";

        var line = $"##dhcp-host={mac},{address},{name}";
        var e = DnsmasqConfDhcpHostLineParser.ParseLine(line, 1);

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
