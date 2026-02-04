using DnsmasqWebUI.Models.Config;
using DnsmasqWebUI.Parsers;

namespace DnsmasqWebUI.Tests;

/// <summary>
/// Tests for DnsmasqConfDirectiveParser. Key/value parsing (TryParseKeyValue) and comment stripping (StripComment)
/// used by DnsmasqConfIncludeParser to build effective config.
/// </summary>
public class DnsmasqConfDirectiveParserTests
{
    [Fact]
    public void TryParseKeyValue_Empty_ReturnsNull()
    {
        Assert.Null(DnsmasqConfDirectiveParser.TryParseKeyValue(""));
        Assert.Null(DnsmasqConfDirectiveParser.TryParseKeyValue("   "));
    }

    [Fact]
    public void TryParseKeyValue_Comment_ReturnsNull()
    {
        Assert.Null(DnsmasqConfDirectiveParser.TryParseKeyValue("# comment"));
        Assert.Null(DnsmasqConfDirectiveParser.TryParseKeyValue("# addn-hosts=/etc/hosts"));
    }

    [Fact]
    public void TryParseKeyValue_KeyValue_ReturnsKeyAndValue()
    {
        var kv = DnsmasqConfDirectiveParser.TryParseKeyValue("port=53");
        Assert.NotNull(kv);
        Assert.Equal(DnsmasqConfKeys.Port, kv!.Value.key);
        Assert.Equal("53", kv.Value.value);
    }

    [Fact]
    public void TryParseKeyValue_KeyOnly_ReturnsEmptyValue()
    {
        var kv = DnsmasqConfDirectiveParser.TryParseKeyValue("domain-needed");
        Assert.NotNull(kv);
        Assert.Equal(DnsmasqConfKeys.DomainNeeded, kv!.Value.key);
        Assert.Equal("", kv.Value.value);
    }

    [Fact]
    public void TryParseKeyValue_CommentedLine_ReturnsNull()
    {
        Assert.Null(DnsmasqConfDirectiveParser.TryParseKeyValue("#addn-hosts=/etc/hosts"));
    }

    [Fact]
    public void StripComment_CommentAfterSpace_StripsToEnd()
    {
        Assert.Equal("port=53", DnsmasqConfDirectiveParser.StripComment("port=53 # DNS port"));
        Assert.Equal("port=53", DnsmasqConfDirectiveParser.StripComment("port=53  # comment"));
    }

    [Fact]
    public void TryParseKeyValue_LineWithComment_ValueExcludesComment()
    {
        var kv = DnsmasqConfDirectiveParser.TryParseKeyValue("port=53 # DNS port");
        Assert.NotNull(kv);
        Assert.Equal(DnsmasqConfKeys.Port, kv!.Value.key);
        Assert.Equal("53", kv.Value.value);
    }
}
