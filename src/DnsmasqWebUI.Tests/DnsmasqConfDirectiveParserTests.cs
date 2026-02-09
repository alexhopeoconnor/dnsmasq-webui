using DnsmasqWebUI.Infrastructure.Helpers.Config;
using DnsmasqWebUI.Infrastructure.Parsers;

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
        var value = "53";
        var line = $"port={value}";
        var kv = DnsmasqConfDirectiveParser.TryParseKeyValue(line);
        Assert.NotNull(kv);
        Assert.Equal(DnsmasqConfKeys.Port, kv!.Value.key);
        Assert.Equal(value, kv.Value.value);
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
        var before = "port=53";
        var comment1 = " DNS port";
        var comment2 = " comment";
        Assert.Equal(before, DnsmasqConfDirectiveParser.StripComment(before + " #" + comment1));
        Assert.Equal(before, DnsmasqConfDirectiveParser.StripComment(before + "  #" + comment2));
    }

    [Fact]
    public void TryParseKeyValue_LineWithComment_ValueExcludesComment()
    {
        var value = "53";
        var comment = " DNS port";
        var line = $"port={value} #{comment}";
        var kv = DnsmasqConfDirectiveParser.TryParseKeyValue(line);
        Assert.NotNull(kv);
        Assert.Equal(DnsmasqConfKeys.Port, kv!.Value.key);
        Assert.Equal(value, kv.Value.value);
    }
}
