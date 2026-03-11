using DnsmasqWebUI.Infrastructure.Serialization.Parsers.EffectiveConfig;
using DnsmasqWebUI.Infrastructure.Services.Dnsmasq.Config;

namespace DnsmasqWebUI.Tests.Serialization.Parsers.DnsmasqConfig;

public class DnsmasqDhcpRangeValueParserTests
{
    [Fact]
    public void TryParse_ValidRange_ReturnsStructuredTokens()
    {
        var ok = DnsmasqDhcpRangeValueParser.TryParse(
            "tag:guest,set:known,192.168.1.50,192.168.1.150,12h",
            out var parsed,
            out var error);

        Assert.True(ok);
        Assert.Null(error);
        Assert.NotNull(parsed);
        Assert.Equal(["tag:guest", "set:known"], parsed!.Tags);
        Assert.Equal("192.168.1.50", parsed.StartToken);
        Assert.Equal("192.168.1.150", parsed.SecondToken);
        Assert.Equal(["12h"], parsed.RemainingTokens);
    }

    [Fact]
    public void TryParse_MalformedTrailingComma_ReturnsError()
    {
        var ok = DnsmasqDhcpRangeValueParser.TryParse("172.28.0.10,", out var parsed, out var error);

        Assert.False(ok);
        Assert.Null(parsed);
        Assert.Equal("dhcp-range contains an empty comma-separated segment.", error);
    }

    [Fact]
    public void GetIPv4StartEnd_ReturnsParsedIpv4Range()
    {
        var result = DnsmasqDhcpRangeValueParser.GetIPv4StartEnd("tag:guest,192.168.1.50,192.168.1.150,12h");

        Assert.Equal(("192.168.1.50", "192.168.1.150"), result);
    }
}
