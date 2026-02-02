using DnsmasqWebUI.Models.Config;
using DnsmasqWebUI.Models.Dhcp;
using DnsmasqWebUI.Parsers;

namespace DnsmasqWebUI.Tests;

/// <summary>
/// Tests for DnsmasqConfFileLineParser. Parses dnsmasq config file lines into Blank, Comment, AddnHosts, DhcpHost, Other.
/// </summary>
public class DnsmasqConfFileLineParserTests
{
    [Fact]
    public void ParseFile_Empty_ReturnsEmpty()
    {
        var lines = DnsmasqConfFileLineParser.ParseFile([]);
        Assert.Empty(lines);
    }

    [Fact]
    public void ParseFile_BlankLine_BlankKind()
    {
        var lines = DnsmasqConfFileLineParser.ParseFile([""]);
        Assert.Single(lines);
        var blank = Assert.IsType<BlankLine>(lines[0]);
        Assert.Equal(1, blank.LineNumber);
        Assert.Equal("", blank.RawLine);
    }

    [Fact]
    public void ParseFile_BlankLineWithSpaces_BlankKind()
    {
        var lines = DnsmasqConfFileLineParser.ParseFile(["   \t  "]);
        Assert.Single(lines);
        var blank = Assert.IsType<BlankLine>(lines[0]);
        Assert.Equal("   \t  ", blank.RawLine);
    }

    [Fact]
    public void ParseFile_CommentLine_CommentKind()
    {
        var lines = DnsmasqConfFileLineParser.ParseFile(["# Not managed by this app."]);
        Assert.Single(lines);
        var comment = Assert.IsType<CommentLine>(lines[0]);
        Assert.Equal(1, comment.LineNumber);
        Assert.Equal("# Not managed by this app.", comment.RawLine);
    }

    [Fact]
    public void ParseFile_OtherDirective_OtherKind()
    {
        var lines = DnsmasqConfFileLineParser.ParseFile(["domain=local"]);
        Assert.Single(lines);
        var other = Assert.IsType<OtherLine>(lines[0]);
        Assert.Equal("domain=local", other.RawLine);
    }

    [Fact]
    public void ParseFile_AddnHostsLine_AddnHostsKind()
    {
        var lines = DnsmasqConfFileLineParser.ParseFile(["addn-hosts=/var/lib/dnsmasq/hosts"]);
        Assert.Single(lines);
        var addn = Assert.IsType<AddnHostsLine>(lines[0]);
        Assert.Equal("/var/lib/dnsmasq/hosts", addn.AddnHostsPath);
    }

    [Fact]
    public void ToLine_AddnHosts_EmitsAddnHostsLine()
    {
        var line = new AddnHostsLine { LineNumber = 1, AddnHostsPath = "/etc/hosts" };
        Assert.Equal("addn-hosts=/etc/hosts", DnsmasqConfFileLineParser.ToLine(line));
    }

    [Fact]
    public void ParseFile_DhcpHostLine_DhcpHostKind()
    {
        var lines = DnsmasqConfFileLineParser.ParseFile(["dhcp-host=aa:bb:cc:dd:ee:ff,192.168.1.10,testpc,infinite"]);
        Assert.Single(lines);
        var dhcpLine = Assert.IsType<DhcpHostLine>(lines[0]);
        Assert.Single(dhcpLine.DhcpHost.MacAddresses);
        Assert.Equal("aa:bb:cc:dd:ee:ff", dhcpLine.DhcpHost.MacAddresses[0]);
        Assert.Equal("192.168.1.10", dhcpLine.DhcpHost.Address);
    }

    [Fact]
    public void ParseFile_CommentedDhcpHost_TreatedAsComment()
    {
        // Parser treats any line starting with # as Comment (does not parse dhcp-host content)
        var lines = DnsmasqConfFileLineParser.ParseFile(["#dhcp-host=aa:bb:cc:dd:ee:ff,192.168.1.10,oldpc"]);
        Assert.Single(lines);
        Assert.Equal(DnsmasqConfLineKind.Comment, lines[0].Kind);
    }

    [Fact]
    public void ParseFile_MixedFile_ParsesAll()
    {
        var input = new[]
        {
            "# Sample config",
            "",
            "domain=local",
            "dhcp-host=11:22:33:44:55:66,192.168.1.11,laptop,infinite",
            "dhcp-range=192.168.1.100,192.168.1.200"
        };
        var lines = DnsmasqConfFileLineParser.ParseFile(input);
        Assert.Equal(5, lines.Count);

        Assert.IsType<CommentLine>(lines[0]);
        Assert.IsType<BlankLine>(lines[1]);
        var other2 = Assert.IsType<OtherLine>(lines[2]);
        Assert.Equal("domain=local", other2.RawLine);
        var dhcp3 = Assert.IsType<DhcpHostLine>(lines[3]);
        Assert.Equal("192.168.1.11", dhcp3.DhcpHost.Address);
        var other4 = Assert.IsType<OtherLine>(lines[4]);
        Assert.Equal("dhcp-range=192.168.1.100,192.168.1.200", other4.RawLine);
    }

    [Fact]
    public void ToLine_Blank_PreservesRawLine()
    {
        var line = new BlankLine { LineNumber = 1, RawLine = "  " };
        Assert.Equal("  ", DnsmasqConfFileLineParser.ToLine(line));
    }

    [Fact]
    public void ToLine_Comment_PreservesRawLine()
    {
        var line = new CommentLine { LineNumber = 1, RawLine = "# comment" };
        Assert.Equal("# comment", DnsmasqConfFileLineParser.ToLine(line));
    }

    [Fact]
    public void ToLine_Other_PreservesRawLine()
    {
        var line = new OtherLine { LineNumber = 1, RawLine = "domain=local" };
        Assert.Equal("domain=local", DnsmasqConfFileLineParser.ToLine(line));
    }

    [Fact]
    public void ToLine_DhcpHost_SerializesViaDhcpHostLineParser()
    {
        var dhcp = new DhcpHostEntry
        {
            LineNumber = 1,
            MacAddresses = ["aa:bb:cc:dd:ee:ff"],
            Address = "192.168.1.10",
            Name = "testpc",
            Lease = "infinite"
        };
        var line = new DhcpHostLine { LineNumber = 1, DhcpHost = dhcp };
        var back = DnsmasqConfFileLineParser.ToLine(line);
        Assert.Equal("dhcp-host=aa:bb:cc:dd:ee:ff, testpc, 192.168.1.10, infinite", back);
    }

    [Fact]
    public void ParseFile_LineNumbers_Sequential()
    {
        var input = new[] { "a", "b", "c" };
        var lines = DnsmasqConfFileLineParser.ParseFile(input);
        Assert.Equal(1, lines[0].LineNumber);
        Assert.Equal(2, lines[1].LineNumber);
        Assert.Equal(3, lines[2].LineNumber);
    }

    [Fact]
    public void ParseFile_testdata_dhcp_conf_ParsesCommentAndDhcpHost()
    {
        var input = TestDataHelper.ReadAllLines("dnsmasq.d/dhcp.conf");
        var lines = DnsmasqConfFileLineParser.ParseFile(input);
        Assert.Equal(3, lines.Count);
        Assert.IsType<CommentLine>(lines[0]);
        Assert.IsType<CommentLine>(lines[1]);
        var dhcpLine = Assert.IsType<DhcpHostLine>(lines[2]);
        Assert.Equal("aa:bb:cc:dd:ee:ff", dhcpLine.DhcpHost.MacAddresses[0]);
        Assert.Equal("172.28.0.100", dhcpLine.DhcpHost.Address);
        Assert.Equal("testpc", dhcpLine.DhcpHost.Name);
    }

    [Fact]
    public void ParseFile_testdata_01_other_conf_ParsesCommentAndOther()
    {
        var input = TestDataHelper.ReadAllLines("dnsmasq.d/01-other.conf");
        var lines = DnsmasqConfFileLineParser.ParseFile(input);
        Assert.Equal(2, lines.Count);
        Assert.IsType<CommentLine>(lines[0]);
        var other = Assert.IsType<OtherLine>(lines[1]);
        Assert.Equal("domain=local", other.RawLine);
    }
}
