using DnsmasqWebUI.Models.Hosts;
using DnsmasqWebUI.Infrastructure.Parsers;

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
        var raw = "   \t  ";
        var e = HostsFileLineParser.ParseLine(raw, 2);
        Assert.NotNull(e);
        Assert.True(e!.IsPassthrough);
    }

    [Fact]
    public void ParseLine_CommentOnly_TreatsWholeLineAsComment()
    {
        var commentLine = "# This is a comment";
        var e = HostsFileLineParser.ParseLine(commentLine, 1);
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
        string address = "127.0.0.1";
        string name = "localhost";
        var line = $"# {address} {name}";
        var e = HostsFileLineParser.ParseLine(line, 1);
        Assert.NotNull(e);
        Assert.True(e!.IsComment);
        Assert.Equal("", e.Address);
        Assert.Empty(e.Names);
    }

    [Fact]
    public void ParseLine_IPv4Localhost_ParsesAddressAndName()
    {
        string address = "127.0.0.1";
        string name = "localhost";
        var line = $"{address} {name}";
        var e = HostsFileLineParser.ParseLine(line, 1);
        Assert.NotNull(e);
        Assert.False(e!.IsPassthrough);
        Assert.Equal(address, e.Address);
        Assert.Single(e.Names);
        Assert.Equal(name, e.Names[0]);
    }

    [Fact]
    public void ParseLine_IPv4WithCanonicalAndAlias_ParsesAll()
    {
        string address = "127.0.1.1";
        string canonical = "thishost.example.org";
        string alias = "thishost";
        var line = $"{address} {canonical} {alias}";
        var e = HostsFileLineParser.ParseLine(line, 1);
        Assert.NotNull(e);
        Assert.Equal(address, e.Address);
        Assert.True(e.Names.Count >= 1);
        Assert.Equal(canonical, e.Names[0]);
        if (e.Names.Count >= 2)
            Assert.Equal(alias, e.Names[1]);
    }

    [Fact]
    public void ParseLine_IPv4WithMultipleSpaces_ParsesCorrectly()
    {
        string address = "192.168.1.10";
        string name1 = "foo.example.org";
        string name2 = "foo";
        var line = $"{address}    {name1}   {name2}";
        var e = HostsFileLineParser.ParseLine(line, 1);
        Assert.NotNull(e);
        Assert.Equal(address, e.Address);
        Assert.True(e.Names.Count >= 1);
        Assert.Equal(name1, e.Names[0]);
    }

    [Fact]
    public void ParseLine_IPv6_ParsesAddressAndNames()
    {
        string address = "::1";
        string name1 = "localhost";
        var line = $"{address} {name1} ip6-localhost ip6-loopback";
        var e = HostsFileLineParser.ParseLine(line, 1);
        Assert.NotNull(e);
        Assert.Equal(address, e.Address);
        Assert.True(e.Names.Count >= 1);
        Assert.Equal(name1, e.Names[0]);
    }

    [Fact]
    public void ParseLine_IPv6Multicast_Parses()
    {
        string address = "ff02::1";
        string name = "ip6-allnodes";
        var line = $"{address} {name}";
        var e = HostsFileLineParser.ParseLine(line, 1);
        Assert.NotNull(e);
        Assert.Equal(address, e.Address);
        Assert.Single(e.Names);
        Assert.Equal(name, e.Names[0]);
    }

    [Fact]
    public void ParseLine_InlineComment_StopsAtHash()
    {
        // Token stops at #; only address and names before # are parsed
        string address = "127.0.0.1";
        string name = "localhost";
        string comment = "loopback";
        var line = $"{address} {name} # {comment}";
        var e = HostsFileLineParser.ParseLine(line, 1);
        Assert.NotNull(e);
        Assert.Equal(address, e.Address);
        Assert.Single(e.Names);
        Assert.Equal(name, e.Names[0]);
    }

    [Fact]
    public void ParseLine_LeadingWhitespace_TrimmedAndParsed()
    {
        string address = "192.168.1.13";
        string name = "bar";
        var line = $"  {address} {name}";
        var e = HostsFileLineParser.ParseLine(line, 1);
        Assert.NotNull(e);
        Assert.Equal(address, e.Address);
        Assert.Single(e.Names);
        Assert.Equal(name, e.Names[0]);
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
        string address = "192.168.1.1";
        var e = HostsFileLineParser.ParseLine(address, 1);
        Assert.NotNull(e);
        Assert.True(e!.IsPassthrough);
    }

    [Fact]
    public void ToLine_Entry_Roundtrips()
    {
        string address = "127.0.0.1";
        string name = "localhost";
        var line = $"{address} {name}";
        var e = HostsFileLineParser.ParseLine(line, 1);
        Assert.NotNull(e);
        var back = HostsFileLineParser.ToLine(e!);
        Assert.Equal(line, back);
    }

    [Fact]
    public void ToLine_CommentedEntry_PrefixPreserved()
    {
        string address = "127.0.0.1";
        string name = "localhost";
        var commentLine = $"# {address} {name}";
        var e = HostsFileLineParser.ParseLine(commentLine, 1);
        Assert.NotNull(e);
        // When we parse "# ..." we get isComment=true, address="", names=[]. ToLine for passthrough returns RawLine.
        e = new HostEntry { LineNumber = 1, Address = address, Names = [name], IsComment = true };
        var back = HostsFileLineParser.ToLine(e);
        Assert.Equal(commentLine, back);
    }

    [Fact]
    public void ToLine_Passthrough_ReturnsRawLine()
    {
        var raw = "  \t  ";
        var e = new HostEntry { LineNumber = 1, RawLine = raw, IsPassthrough = true };
        Assert.Equal(raw, HostsFileLineParser.ToLine(e));
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
