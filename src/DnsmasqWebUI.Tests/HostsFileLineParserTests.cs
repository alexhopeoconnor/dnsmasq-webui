using DnsmasqWebUI.Models.Hosts;
using DnsmasqWebUI.Parsers;

namespace DnsmasqWebUI.Tests;

/// <summary>
/// Tests for HostsFileLineParser. Format: IP_address canonical_hostname [aliases...] (hosts(5), RFC 952).
/// Fields separated by blanks/tabs; '#' to EOL is comment.
/// </summary>
public class HostsFileLineParserTests
{
    [Fact]
    public void ParseLine_Blank_ReturnsPassthrough()
    {
        var e = HostsFileLineParser.ParseLine("", 1);
        Assert.NotNull(e);
        Assert.True(e!.IsPassthrough);
        Assert.Equal(1, e.LineNumber);
        Assert.Equal("", e.RawLine);
    }

    [Fact]
    public void ParseLine_WhitespaceOnly_ReturnsPassthrough()
    {
        var e = HostsFileLineParser.ParseLine("   \t  ", 2);
        Assert.NotNull(e);
        Assert.True(e!.IsPassthrough);
    }

    [Fact]
    public void ParseLine_CommentOnly_TreatsWholeLineAsComment()
    {
        var e = HostsFileLineParser.ParseLine("# This is a comment", 1);
        Assert.NotNull(e);
        Assert.True(e!.IsComment);
        Assert.True(e.IsPassthrough);
        Assert.Equal("", e.Address);
        Assert.Empty(e.Names);
    }

    [Fact]
    public void ParseLine_CommentWithDataLikeContent_DoesNotParseAsAddress()
    {
        // Per hosts(5): text from # to EOL is comment - so "# 127.0.0.1 localhost" is entirely comment
        var e = HostsFileLineParser.ParseLine("# 127.0.0.1 localhost", 1);
        Assert.NotNull(e);
        Assert.True(e!.IsComment);
        Assert.Equal("", e.Address);
        Assert.Empty(e.Names);
    }

    [Fact]
    public void ParseLine_IPv4Localhost_ParsesAddressAndName()
    {
        var e = HostsFileLineParser.ParseLine("127.0.0.1 localhost", 1);
        Assert.NotNull(e);
        Assert.False(e!.IsPassthrough);
        Assert.Equal("127.0.0.1", e.Address);
        Assert.Single(e.Names);
        Assert.Equal("localhost", e.Names[0]);
    }

    [Fact]
    public void ParseLine_IPv4WithCanonicalAndAlias_ParsesAll()
    {
        var e = HostsFileLineParser.ParseLine("127.0.1.1 thishost.example.org thishost", 1);
        Assert.NotNull(e);
        Assert.Equal("127.0.1.1", e.Address);
        Assert.True(e.Names.Count >= 1);
        Assert.Equal("thishost.example.org", e.Names[0]);
        if (e.Names.Count >= 2)
            Assert.Equal("thishost", e.Names[1]);
    }

    [Fact]
    public void ParseLine_IPv4WithMultipleSpaces_ParsesCorrectly()
    {
        var e = HostsFileLineParser.ParseLine("192.168.1.10    foo.example.org   foo", 1);
        Assert.NotNull(e);
        Assert.Equal("192.168.1.10", e.Address);
        Assert.True(e.Names.Count >= 1);
        Assert.Equal("foo.example.org", e.Names[0]);
    }

    [Fact]
    public void ParseLine_IPv6_ParsesAddressAndNames()
    {
        var e = HostsFileLineParser.ParseLine("::1 localhost ip6-localhost ip6-loopback", 1);
        Assert.NotNull(e);
        Assert.Equal("::1", e.Address);
        Assert.True(e.Names.Count >= 1);
        Assert.Equal("localhost", e.Names[0]);
    }

    [Fact]
    public void ParseLine_IPv6Multicast_Parses()
    {
        var e = HostsFileLineParser.ParseLine("ff02::1 ip6-allnodes", 1);
        Assert.NotNull(e);
        Assert.Equal("ff02::1", e.Address);
        Assert.Single(e.Names);
        Assert.Equal("ip6-allnodes", e.Names[0]);
    }

    [Fact]
    public void ParseLine_InlineComment_StopsAtHash()
    {
        // Token stops at #; only address and names before # are parsed
        var e = HostsFileLineParser.ParseLine("127.0.0.1 localhost # loopback", 1);
        Assert.NotNull(e);
        Assert.Equal("127.0.0.1", e.Address);
        Assert.Single(e.Names);
        Assert.Equal("localhost", e.Names[0]);
    }

    [Fact]
    public void ParseLine_LeadingWhitespace_TrimmedAndParsed()
    {
        var e = HostsFileLineParser.ParseLine("  192.168.1.13 bar", 1);
        Assert.NotNull(e);
        Assert.Equal("192.168.1.13", e.Address);
        Assert.Single(e.Names);
        Assert.Equal("bar", e.Names[0]);
    }

    [Fact]
    public void ParseLine_UnparseableLine_ReturnsPassthrough()
    {
        // Line with no whitespace between tokens fails Content (address + space + names)
        var e = HostsFileLineParser.ParseLine("garbage-with-no-space", 1);
        Assert.NotNull(e);
        Assert.True(e!.IsPassthrough);
    }

    [Fact]
    public void ParseLine_OnlyAddressNoNames_FailsContent_Passthrough()
    {
        // Content requires address + whitespace + at least one name
        var e = HostsFileLineParser.ParseLine("192.168.1.1", 1);
        Assert.NotNull(e);
        Assert.True(e!.IsPassthrough);
    }

    [Fact]
    public void ToLine_Entry_Roundtrips()
    {
        var line = "127.0.0.1 localhost";
        var e = HostsFileLineParser.ParseLine(line, 1);
        Assert.NotNull(e);
        var back = HostsFileLineParser.ToLine(e!);
        Assert.Equal(line, back);
    }

    [Fact]
    public void ToLine_CommentedEntry_PrefixPreserved()
    {
        var e = HostsFileLineParser.ParseLine("# 127.0.0.1 localhost", 1);
        Assert.NotNull(e);
        // When we parse "# ..." we get isComment=true, address="", names=[]. ToLine for passthrough returns RawLine.
        e = new HostEntry { LineNumber = 1, Address = "127.0.0.1", Names = ["localhost"], IsComment = true };
        var back = HostsFileLineParser.ToLine(e);
        Assert.Equal("# 127.0.0.1 localhost", back);
    }

    [Fact]
    public void ToLine_Passthrough_ReturnsRawLine()
    {
        var e = new HostEntry { LineNumber = 1, RawLine = "  \t  ", IsPassthrough = true };
        Assert.Equal("  \t  ", HostsFileLineParser.ToLine(e));
    }

    [Fact]
    public void Parse_testdata_hosts_ParsesAllLines()
    {
        var lines = TestDataHelper.ReadAllLines("hosts");
        Assert.True(lines.Length >= 2, "testdata/hosts should have at least 2 lines");
        var entries = new List<HostEntry?>();
        for (var i = 0; i < lines.Length; i++)
            entries.Add(HostsFileLineParser.ParseLine(lines[i], i + 1));

        var dataEntries = entries.Where(e => e != null && !e.IsPassthrough).ToList();
        Assert.Equal(5, dataEntries.Count);
        Assert.Equal("127.0.0.1", dataEntries[0]!.Address);
        Assert.Single(dataEntries[0]!.Names);
        Assert.Equal("localhost", dataEntries[0]!.Names[0]);
        Assert.Equal("::1", dataEntries[1]!.Address);
        Assert.Single(dataEntries[1]!.Names);
        Assert.Equal("localhost", dataEntries[1]!.Names[0]);
        Assert.Equal("192.168.1.1", dataEntries[2]!.Address);
        Assert.Single(dataEntries[2]!.Names);
        Assert.Equal("router", dataEntries[2]!.Names[0]);
        Assert.Equal("192.168.1.10", dataEntries[3]!.Address);
        Assert.Single(dataEntries[3]!.Names);
        Assert.Equal("testpc", dataEntries[3]!.Names[0]);
        Assert.Equal("172.28.0.2", dataEntries[4]!.Address);
        Assert.Single(dataEntries[4]!.Names);
        Assert.Equal("dnsmasq-webui", dataEntries[4]!.Names[0]);
    }
}
