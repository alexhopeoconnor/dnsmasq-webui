using System.Linq;
using DnsmasqWebUI.Models.Dhcp;

namespace DnsmasqWebUI.Tests.Models.Dhcp;

public class DhcpOptionDraftTests
{
    [Fact]
    public void FromRaw_WithPrefixes_RoundTrips()
    {
        const string raw = "tag:guest,option:router,192.168.1.1";
        var d = DhcpOptionDraft.FromRaw(raw);
        Assert.True(d.IsStructured);
        Assert.Equal("option:router", d.Selector);
        Assert.Equal(raw, d.ToRaw());
    }

    [Fact]
    public void Preset_Router_HasExpectedSelector()
    {
        var p = DhcpOptionPresets.All.First(x => x.Key == "router");
        Assert.Equal("option:router", p.Selector);
    }

    [Fact]
    public void ToRaw_WithTagPrefix_PreservesOrder()
    {
        var d = new DhcpOptionDraft
        {
            PrefixTokens = new[] { "tag:guest" },
            Selector = "option:dns-server",
            ValueTokens = new List<string> { "8.8.8.8", "8.8.4.4" }
        };
        const string expected = "tag:guest,option:dns-server,8.8.8.8,8.8.4.4";
        Assert.Equal(expected, d.ToRaw());
        var roundTrip = DhcpOptionDraft.FromRaw(expected);
        Assert.True(roundTrip.IsStructured);
        Assert.Equal(expected, roundTrip.ToRaw());
    }
}
