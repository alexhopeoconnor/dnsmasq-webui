using DnsmasqWebUI.Models.Config;
using DnsmasqWebUI.Models.Dhcp;
using DnsmasqWebUI.Infrastructure.Parsers;

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
        var raw = "";
        var lines = DnsmasqConfFileLineParser.ParseFile([raw]);
        Assert.Single(lines);
        var blank = Assert.IsType<BlankLine>(lines[0]);
        Assert.Equal(1, blank.LineNumber);
        Assert.Equal(raw, blank.RawLine);
    }

    [Fact]
    public void ParseFile_BlankLineWithSpaces_BlankKind()
    {
        var raw = "   \t  ";
        var lines = DnsmasqConfFileLineParser.ParseFile([raw]);
        Assert.Single(lines);
        var blank = Assert.IsType<BlankLine>(lines[0]);
        Assert.Equal(raw, blank.RawLine);
    }

    [Fact]
    public void ParseFile_CommentLine_CommentKind()
    {
        var commentLine = "# Not managed by this app.";
        var lines = DnsmasqConfFileLineParser.ParseFile([commentLine]);
        Assert.Single(lines);
        var comment = Assert.IsType<CommentLine>(lines[0]);
        Assert.Equal(1, comment.LineNumber);
        Assert.Equal(commentLine, comment.RawLine);
    }

    [Fact]
    public void ParseFile_OtherDirective_OtherKind()
    {
        var raw = "domain=local";
        var lines = DnsmasqConfFileLineParser.ParseFile([raw]);
        Assert.Single(lines);
        var other = Assert.IsType<OtherLine>(lines[0]);
        Assert.Equal(raw, other.RawLine);
    }

    [Fact]
    public void ParseFile_AddnHostsLine_AddnHostsKind()
    {
        var path = "/var/lib/dnsmasq/hosts";
        var line = $"addn-hosts={path}";
        var lines = DnsmasqConfFileLineParser.ParseFile([line]);
        Assert.Single(lines);
        var addn = Assert.IsType<AddnHostsLine>(lines[0]);
        Assert.Equal(path, addn.AddnHostsPath);
    }

    [Fact]
    public void ToLine_AddnHosts_EmitsAddnHostsLine()
    {
        var path = "/etc/hosts";
        var line = new AddnHostsLine { LineNumber = 1, AddnHostsPath = path };
        Assert.Equal($"addn-hosts={path}", DnsmasqConfFileLineParser.ToLine(line));
    }

    [Fact]
    public void ParseFile_DhcpHostLine_DhcpHostKind()
    {
        string mac = "aa:bb:cc:dd:ee:ff";
        string address = "192.168.1.10";
        string name = "testpc";
        string lease = "infinite";
        var line = $"dhcp-host={mac},{address},{name},{lease}";
        var lines = DnsmasqConfFileLineParser.ParseFile([line]);
        Assert.Single(lines);
        var dhcpLine = Assert.IsType<DhcpHostLine>(lines[0]);
        Assert.Single(dhcpLine.DhcpHost.MacAddresses);
        Assert.Equal(mac, dhcpLine.DhcpHost.MacAddresses[0]);
        Assert.Equal(address, dhcpLine.DhcpHost.Address);
    }

    [Fact]
    public void ParseFile_CommentedDhcpHost_TreatedAsComment()
    {
        // Parser treats any line starting with # as Comment (does not parse dhcp-host content)
        string mac = "aa:bb:cc:dd:ee:ff";
        string address = "192.168.1.10";
        string name = "oldpc";
        var line = $"#dhcp-host={mac},{address},{name}";
        var lines = DnsmasqConfFileLineParser.ParseFile([line]);
        Assert.Single(lines);
        Assert.Equal(DnsmasqConfLineKind.Comment, lines[0].Kind);
    }

    [Fact]
    public void ParseFile_MixedFile_ParsesAll()
    {
        var domainLine = "domain=local";
        string dhcpMac = "11:22:33:44:55:66";
        string dhcpAddress = "192.168.1.11";
        string dhcpName = "laptop";
        string dhcpLease = "infinite";
        var rangeLine = "dhcp-range=192.168.1.100,192.168.1.200";
        var dhcpLine = $"dhcp-host={dhcpMac},{dhcpAddress},{dhcpName},{dhcpLease}";

        var input = new[]
        {
            "# Sample config",
            "",
            domainLine,
            dhcpLine,
            rangeLine
        };
        var lines = DnsmasqConfFileLineParser.ParseFile(input);
        Assert.Equal(5, lines.Count);

        Assert.IsType<CommentLine>(lines[0]);
        Assert.IsType<BlankLine>(lines[1]);
        var other2 = Assert.IsType<OtherLine>(lines[2]);
        Assert.Equal(domainLine, other2.RawLine);
        var dhcp3 = Assert.IsType<DhcpHostLine>(lines[3]);
        Assert.Equal(dhcpAddress, dhcp3.DhcpHost.Address);
        var other4 = Assert.IsType<OtherLine>(lines[4]);
        Assert.Equal(rangeLine, other4.RawLine);
    }

    [Fact]
    public void ToLine_Blank_PreservesRawLine()
    {
        var raw = "  ";
        var line = new BlankLine { LineNumber = 1, RawLine = raw };
        Assert.Equal(raw, DnsmasqConfFileLineParser.ToLine(line));
    }

    [Fact]
    public void ToLine_Comment_PreservesRawLine()
    {
        var raw = "# comment";
        var line = new CommentLine { LineNumber = 1, RawLine = raw };
        Assert.Equal(raw, DnsmasqConfFileLineParser.ToLine(line));
    }

    [Fact]
    public void ToLine_Other_PreservesRawLine()
    {
        var raw = "domain=local";
        var line = new OtherLine { LineNumber = 1, RawLine = raw };
        Assert.Equal(raw, DnsmasqConfFileLineParser.ToLine(line));
    }

    [Fact]
    public void ToLine_DhcpHost_SerializesViaDhcpHostLineParser()
    {
        string mac = "aa:bb:cc:dd:ee:ff";
        string address = "192.168.1.10";
        string name = "testpc";
        string lease = "infinite";
        var dhcp = new DhcpHostEntry
        {
            LineNumber = 1,
            MacAddresses = [mac],
            Address = address,
            Name = name,
            Lease = lease
        };
        var line = new DhcpHostLine { LineNumber = 1, DhcpHost = dhcp };
        var back = DnsmasqConfFileLineParser.ToLine(line);
        Assert.Equal($"dhcp-host={mac}, {name}, {address}, {lease}", back);
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
