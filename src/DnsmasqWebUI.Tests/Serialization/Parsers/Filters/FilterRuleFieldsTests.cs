using DnsmasqWebUI.Infrastructure.Serialization.Parsers.Filters;
using Xunit;

namespace DnsmasqWebUI.Tests.Serialization.Parsers.Filters;

public class FilterRuleFieldsTests
{
    [Fact]
    public void Address_AllDomains_NullSinkhole_SummaryAndRoundTrip()
    {
        var raw = "/#/#";
        var p = AddressRuleFields.Parse(raw);
        Assert.False(p.IsRaw);
        Assert.Equal(AddressMatchMode.AllDomains, p.MatchMode);
        Assert.Equal(AddressResponseMode.NullSinkhole, p.ResponseMode);
        Assert.Equal("all domains return null/sinkhole", p.ToSummary());
        Assert.Equal(raw, p.ToConfigLine());
    }

    [Fact]
    public void Address_DomainPattern_Ip_Summary()
    {
        var raw = "/corp.local/10.0.0.2";
        var p = AddressRuleFields.Parse(raw);
        Assert.False(p.IsRaw);
        Assert.Equal(AddressMatchMode.DomainPattern, p.MatchMode);
        Assert.Equal("corp.local", p.DomainPath);
        Assert.Equal("queries for /corp.local/ return 10.0.0.2", p.ToSummary());
    }

    [Fact]
    public void Server_Scoped_Summary()
    {
        var raw = "/corp.local/10.0.0.2";
        var p = ServerRuleFields.Parse(raw);
        Assert.False(p.IsRaw);
        Assert.True(p.IsScoped);
        Assert.Equal("queries for /corp.local/ go to 10.0.0.2", p.ToSummary());
    }

    [Fact]
    public void RevServer_Summary_MatchesExample()
    {
        var raw = "192.168.1.0/24,10.0.0.1";
        var p = RevServerRuleFields.Parse(raw);
        Assert.False(p.IsRaw);
        Assert.Equal("reverse lookups for 192.168.1.0/24 go to 10.0.0.1", p.ToSummary());
    }

    [Fact]
    public void BogusNxdomain_Summary()
    {
        var p = BogusNxdomainRuleFields.Parse("64.94.110.11/24");
        Assert.False(p.IsRaw);
        Assert.Contains("64.94.110.11/24", p.ToSummary());
        Assert.Contains("bogus NXDOMAIN", p.ToSummary());
    }

    [Fact]
    public void FilterRr_Summary()
    {
        var p = FilterRrRuleFields.Parse("0.0.0.0,A");
        Assert.False(p.IsRaw);
        Assert.Contains("A", p.ToSummary());
        Assert.Contains("0.0.0.0", p.ToSummary());
    }

    [Fact]
    public void FilterRr_MatchMayContainCommas_SplitsOnLastComma_RoundTrips()
    {
        const string raw = "10.0.0.0,192.168.1.0,A";
        var p = FilterRrRuleFields.Parse(raw);
        Assert.False(p.IsRaw);
        Assert.Equal("10.0.0.0,192.168.1.0", p.MatchValue);
        Assert.Equal("A", p.RecordType);
        Assert.Equal(raw, p.ToConfigLine());
    }

    [Fact]
    public void Local_UnqualifiedNames_Parse_ToConfigLine_RoundTrips()
    {
        const string raw = "//";
        var p = LocalRuleFields.Parse(raw);
        Assert.False(p.IsRaw);
        Assert.Equal("", p.DomainPath);
        Assert.Equal("unqualified names are answered locally only", p.ToSummary());
        Assert.Equal(raw, p.ToConfigLine());
    }
}
