using DnsmasqWebUI.Models.Dhcp;

namespace DnsmasqWebUI.Tests.Models.Dhcp;

public class DhcpRangeDraftTests
{
    [Fact]
    public void FromRaw_Structured_RoundTrips()
    {
        const string raw = "tag:guest,192.168.1.50,192.168.1.150,12h";
        var d = DhcpRangeDraft.FromRaw(raw);
        Assert.True(d.IsStructured);
        Assert.Equal(raw, d.ToRaw());
    }

    [Fact]
    public void FromRaw_Unparseable_UsesFallback()
    {
        const string raw = "not-a-valid-range";
        var d = DhcpRangeDraft.FromRaw(raw);
        Assert.False(d.IsStructured);
        Assert.Equal(raw, d.ToRaw());
    }

    [Fact]
    public void FromRaw_ConstructorIpv4Range_IsStructured()
    {
        const string raw = "192.168.1.50,192.168.1.150,255.255.255.0,12h";
        var d = DhcpRangeDraft.FromRaw(raw);
        Assert.True(d.IsStructured);
        Assert.Equal(raw, d.ToRaw());
    }
}
