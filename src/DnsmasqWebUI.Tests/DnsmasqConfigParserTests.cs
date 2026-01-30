using DnsmasqWebUI.Models;
using DnsmasqWebUI.Parsers;

namespace DnsmasqWebUI.Tests;

/// <summary>
/// Tests for DnsmasqConfigParser. Parses full config into Blank, Comment, DhcpHost, Other.
/// </summary>
public class DnsmasqConfigParserTests
{
    [Fact]
    public void ParseFile_Empty_ReturnsEmpty()
    {
        var lines = DnsmasqConfigParser.ParseFile([]);
        Assert.Empty(lines);
    }

    [Fact]
    public void ParseFile_BlankLine_BlankKind()
    {
        var lines = DnsmasqConfigParser.ParseFile([""]);
        Assert.Single(lines);
        Assert.Equal(ConfigLineKind.Blank, lines[0].Kind);
        Assert.Equal(1, lines[0].LineNumber);
        Assert.Equal("", lines[0].RawLine);
    }

    [Fact]
    public void ParseFile_BlankLineWithSpaces_BlankKind()
    {
        var lines = DnsmasqConfigParser.ParseFile(["   \t  "]);
        Assert.Single(lines);
        Assert.Equal(ConfigLineKind.Blank, lines[0].Kind);
        Assert.Equal("   \t  ", lines[0].RawLine);
    }

    [Fact]
    public void ParseFile_CommentLine_CommentKind()
    {
        var lines = DnsmasqConfigParser.ParseFile(["# Not managed by this app."]);
        Assert.Single(lines);
        Assert.Equal(ConfigLineKind.Comment, lines[0].Kind);
        Assert.Equal(1, lines[0].LineNumber);
        Assert.Equal("# Not managed by this app.", lines[0].RawLine);
    }

    [Fact]
    public void ParseFile_OtherDirective_OtherKind()
    {
        var lines = DnsmasqConfigParser.ParseFile(["domain=local"]);
        Assert.Single(lines);
        Assert.Equal(ConfigLineKind.Other, lines[0].Kind);
        Assert.Equal("domain=local", lines[0].RawLine);
    }

    [Fact]
    public void ParseFile_DhcpHostLine_DhcpHostKind()
    {
        var lines = DnsmasqConfigParser.ParseFile(["dhcp-host=aa:bb:cc:dd:ee:ff,192.168.1.10,testpc,infinite"]);
        Assert.Single(lines);
        Assert.Equal(ConfigLineKind.DhcpHost, lines[0].Kind);
        var dhcp0 = lines[0].DhcpHost;
        Assert.NotNull(dhcp0);
        Assert.Single(dhcp0.MacAddresses);
        Assert.Equal("aa:bb:cc:dd:ee:ff", dhcp0.MacAddresses[0]);
        Assert.Equal("192.168.1.10", dhcp0.Address);
    }

    [Fact]
    public void ParseFile_CommentedDhcpHost_TreatedAsComment()
    {
        // Parser treats any line starting with # as Comment (does not parse dhcp-host content)
        var lines = DnsmasqConfigParser.ParseFile(["#dhcp-host=aa:bb:cc:dd:ee:ff,192.168.1.10,oldpc"]);
        Assert.Single(lines);
        Assert.Equal(ConfigLineKind.Comment, lines[0].Kind);
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
        var lines = DnsmasqConfigParser.ParseFile(input);
        Assert.Equal(5, lines.Count);

        Assert.Equal(ConfigLineKind.Comment, lines[0].Kind);
        Assert.Equal(ConfigLineKind.Blank, lines[1].Kind);
        Assert.Equal(ConfigLineKind.Other, lines[2].Kind);
        Assert.Equal("domain=local", lines[2].RawLine);
        Assert.Equal(ConfigLineKind.DhcpHost, lines[3].Kind);
        var dhcp3 = lines[3].DhcpHost;
        Assert.NotNull(dhcp3);
        Assert.Equal("192.168.1.11", dhcp3.Address);
        Assert.Equal(ConfigLineKind.Other, lines[4].Kind);
        Assert.Equal("dhcp-range=192.168.1.100,192.168.1.200", lines[4].RawLine);
    }

    [Fact]
    public void ToLine_Blank_PreservesRawLine()
    {
        var line = new ConfigLine { Kind = ConfigLineKind.Blank, LineNumber = 1, RawLine = "  " };
        Assert.Equal("  ", DnsmasqConfigParser.ToLine(line));
    }

    [Fact]
    public void ToLine_Comment_PreservesRawLine()
    {
        var line = new ConfigLine { Kind = ConfigLineKind.Comment, LineNumber = 1, RawLine = "# comment" };
        Assert.Equal("# comment", DnsmasqConfigParser.ToLine(line));
    }

    [Fact]
    public void ToLine_Other_PreservesRawLine()
    {
        var line = new ConfigLine { Kind = ConfigLineKind.Other, LineNumber = 1, RawLine = "domain=local" };
        Assert.Equal("domain=local", DnsmasqConfigParser.ToLine(line));
    }

    [Fact]
    public void ToLine_DhcpHost_SerializesViaDhcpHostParser()
    {
        var dhcp = new DhcpHostEntry
        {
            LineNumber = 1,
            MacAddresses = ["aa:bb:cc:dd:ee:ff"],
            Address = "192.168.1.10",
            Name = "testpc",
            Lease = "infinite"
        };
        var line = new ConfigLine { Kind = ConfigLineKind.DhcpHost, LineNumber = 1, DhcpHost = dhcp };
        var back = DnsmasqConfigParser.ToLine(line);
        Assert.Equal("dhcp-host=aa:bb:cc:dd:ee:ff, testpc, 192.168.1.10, infinite", back);
    }

    [Fact]
    public void ParseFile_LineNumbers_Sequential()
    {
        var input = new[] { "a", "b", "c" };
        var lines = DnsmasqConfigParser.ParseFile(input);
        Assert.Equal(1, lines[0].LineNumber);
        Assert.Equal(2, lines[1].LineNumber);
        Assert.Equal(3, lines[2].LineNumber);
    }

    [Fact]
    public void ParseFile_testdata_dhcp_conf_ParsesCommentAndDhcpHost()
    {
        var input = TestDataHelper.ReadAllLines("dnsmasq.d/dhcp.conf");
        var lines = DnsmasqConfigParser.ParseFile(input);
        Assert.Equal(2, lines.Count);
        Assert.Equal(ConfigLineKind.Comment, lines[0].Kind);
        Assert.Equal(ConfigLineKind.DhcpHost, lines[1].Kind);
        var dhcp = lines[1].DhcpHost;
        Assert.NotNull(dhcp);
        Assert.Equal("aa:bb:cc:dd:ee:ff", dhcp.MacAddresses[0]);
        Assert.Equal("192.168.1.10", dhcp.Address);
        Assert.Equal("testpc", dhcp.Name);
    }

    [Fact]
    public void ParseFile_testdata_01_other_conf_ParsesCommentAndOther()
    {
        var input = TestDataHelper.ReadAllLines("dnsmasq.d/01-other.conf");
        var lines = DnsmasqConfigParser.ParseFile(input);
        Assert.Equal(2, lines.Count);
        Assert.Equal(ConfigLineKind.Comment, lines[0].Kind);
        Assert.Equal(ConfigLineKind.Other, lines[1].Kind);
        Assert.Equal("domain=local", lines[1].RawLine);
    }
}
