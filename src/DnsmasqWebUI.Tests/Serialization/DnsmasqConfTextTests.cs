using DnsmasqWebUI.Infrastructure.Serialization;
using DnsmasqWebUI.Infrastructure.Services.EffectiveConfig.Metadata;

namespace DnsmasqWebUI.Tests.Serialization;

public class DnsmasqConfTextTests
{
    [Fact]
    public void DirectivePrefix_ReturnsOptionEquals()
    {
        Assert.Equal("dhcp-host=", DnsmasqConfText.DirectivePrefix(DnsmasqConfKeys.DhcpHost));
        Assert.Equal("server=", DnsmasqConfText.DirectivePrefix(DnsmasqConfKeys.Server));
    }

    [Fact]
    public void DirectiveLine_WhenValueNullOrEmpty_ReturnsOptionOnly()
    {
        Assert.Equal("dhcp-host", DnsmasqConfText.DirectiveLine("dhcp-host", null));
        Assert.Equal("dhcp-host", DnsmasqConfText.DirectiveLine("dhcp-host", ""));
    }

    [Fact]
    public void DirectiveLine_WhenValueProvided_ReturnsOptionEqualsValue()
    {
        Assert.Equal("dhcp-host=11:22:33:44:55:66,192.168.1.1", DnsmasqConfText.DirectiveLine("dhcp-host", "11:22:33:44:55:66,192.168.1.1"));
    }

    [Fact]
    public void StripDirectivePrefix_WhenLineStartsWithPrefix_ReturnsRest()
    {
        var line = "dhcp-host=mac,ip";
        Assert.Equal("mac,ip", DnsmasqConfText.StripDirectivePrefix(DnsmasqConfKeys.DhcpHost, line));
    }

    [Fact]
    public void StripDirectivePrefix_WhenLineDoesNotStartWithPrefix_ReturnsFullLine()
    {
        var line = "other=value";
        Assert.Equal("other=value", DnsmasqConfText.StripDirectivePrefix(DnsmasqConfKeys.DhcpHost, line));
    }

    [Fact]
    public void StripDirectivePrefix_WhenLineIsCommented_PreservesLeadingHashInResult()
    {
        Assert.Equal("#mac,ip", DnsmasqConfText.StripDirectivePrefix(DnsmasqConfKeys.DhcpHost, "#dhcp-host=mac,ip"));
        Assert.Equal("##mac,ip", DnsmasqConfText.StripDirectivePrefix(DnsmasqConfKeys.DhcpHost, "##dhcp-host=mac,ip"));
    }

    [Fact]
    public void HasDirectivePrefix_MatchesOptionAndCommentedForms()
    {
        Assert.True(DnsmasqConfText.HasDirectivePrefix(DnsmasqConfKeys.DhcpHost, "dhcp-host=value"));
        Assert.True(DnsmasqConfText.HasDirectivePrefix(DnsmasqConfKeys.DhcpHost, "#dhcp-host=value"));
        Assert.True(DnsmasqConfText.HasDirectivePrefix(DnsmasqConfKeys.DhcpHost, "##dhcp-host=value"));
        Assert.False(DnsmasqConfText.HasDirectivePrefix(DnsmasqConfKeys.DhcpHost, "server=8.8.8.8"));
        Assert.False(DnsmasqConfText.HasDirectivePrefix(DnsmasqConfKeys.DhcpHost, "other"));
    }
}
