using DnsmasqWebUI.Infrastructure.Serialization.Parsers.EffectiveConfig;

namespace DnsmasqWebUI.Tests.Serialization.Parsers.EffectiveConfig;

public class DomainRuleFieldsTests
{
    [Theory]
    [InlineData("home.arpa", "home.arpa")]
    [InlineData("#", "# (resolv.conf search)")]
    [InlineData("example.com,192.168.0.0/24", "example.com · 192.168.0.0/24")]
    [InlineData("example.com,192.168.0.0/24,local", "example.com · 192.168.0.0/24 · local DNS")]
    [InlineData("example.com,eth0", "example.com · interface eth0")]
    [InlineData("example.com,local", "example.com · local DNS")]
    public void FormatSummary_FormatsExpected(string line, string expectedContains)
    {
        var s = DomainRuleFields.FormatSummary(line);
        Assert.Equal(expectedContains, s);
    }

    [Fact]
    public void Parse_RoundTrip_Unconditional()
    {
        var f = DomainRuleFields.Parse("my.domain");
        Assert.False(f.IsRaw);
        Assert.Equal("my.domain", f.ToConfigLine());
    }

    [Fact]
    public void Parse_RoundTrip_RangeAndLocal()
    {
        var line = "my.domain,10.0.0.0/24,local";
        var f = DomainRuleFields.Parse(line);
        Assert.False(f.IsRaw);
        Assert.Equal(line, f.ToConfigLine());
    }

    [Fact]
    public void Parse_RoundTrip_Interface()
    {
        var line = "my.domain,br0";
        var f = DomainRuleFields.Parse(line);
        Assert.False(f.IsRaw);
        Assert.Equal(DomainRuleScope.InterfaceName, f.Scope);
        Assert.Equal(line, f.ToConfigLine());
    }

    [Fact]
    public void Parse_TooManyTokens_UsesRaw()
    {
        var line = "a,b,c,d";
        var f = DomainRuleFields.Parse(line);
        Assert.True(f.IsRaw);
        Assert.Equal(line, f.ToConfigLine());
    }

    [Fact]
    public void FromRawConfigLine_PreservesLine()
    {
        var f = DomainRuleFields.FromRawConfigLine("  weird,line,here  ");
        Assert.True(f.IsRaw);
        Assert.Equal("weird,line,here", f.ToConfigLine());
    }
}
