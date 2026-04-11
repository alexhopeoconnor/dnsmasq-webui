using DnsmasqWebUI.Infrastructure.Services.Dnsmasq.DnsRecords;
using DnsmasqWebUI.Infrastructure.Services.Dnsmasq.DnsRecords.Abstractions;
using DnsmasqWebUI.Infrastructure.Services.EffectiveConfig.Metadata;
using static DnsmasqWebUI.Infrastructure.Services.EffectiveConfig.Metadata.EffectiveConfigSections;
using DnsmasqWebUI.Models.Dnsmasq.EffectiveConfig;
using DnsmasqWebUI.Models.DnsRecords;

namespace DnsmasqWebUI.Tests.Services.Dnsmasq.DnsRecords;

public class DnsRecordDirectiveCodecProviderTests
{
    private readonly IDnsRecordDirectiveCodecProvider _provider = new DnsRecordDirectiveCodecProvider();

    [Fact]
    public void CnameCodec_RoundTrip_PreservesValue()
    {
        var codec = _provider.Get(DnsmasqConfKeys.Cname);
        const string raw = "alias1,alias2,target.example.com,3600";
        var row = codec.Parse(new ValueWithSource(raw, null), 0);
        Assert.Equal(raw, codec.Serialize(row));
        var p = Assert.IsType<CnamePayload>(row.Payload);
        Assert.Equal(new[] { "alias1", "alias2" }, p.Aliases);
        Assert.Equal("target.example.com", p.Target);
        Assert.Equal(3600, p.Ttl);
    }

    [Fact]
    public void AuthSoaCodec_RoundTrip_KeepsRefreshInCorrectSlotWhenHostmasterBlank()
    {
        var codec = _provider.Get(DnsmasqConfKeys.AuthSoa);
        const string raw = "42,,3600";
        var row = codec.Parse(new ValueWithSource(raw, null), 0);
        Assert.Equal(raw, codec.Serialize(row));
        var p = Assert.IsType<AuthSoaPayload>(row.Payload);
        Assert.Equal("42", p.Serial);
        Assert.Equal("3600", p.Refresh);
    }

    [Fact]
    public void AuthSoaCodec_RoundTrip_FullLineWithHostmaster()
    {
        var codec = _provider.Get(DnsmasqConfKeys.AuthSoa);
        const string raw = "1,hostmaster.example.com,3600,600,86400";
        var row = codec.Parse(new ValueWithSource(raw, null), 0);
        Assert.Equal(raw, codec.Serialize(row));
        var p = Assert.IsType<AuthSoaPayload>(row.Payload);
        Assert.Equal("1", p.Serial);
        Assert.Equal("hostmaster.example.com", p.Hostmaster);
        Assert.Equal("3600", p.Refresh);
        Assert.Equal("600", p.Retry);
        Assert.Equal("86400", p.Expiry);
    }

    [Fact]
    public void AuthSoaCodec_RoundTrip_SerialOnly()
    {
        var codec = _provider.Get(DnsmasqConfKeys.AuthSoa);
        const string raw = "99";
        var row = codec.Parse(new ValueWithSource(raw, null), 0);
        Assert.Equal(raw, codec.Serialize(row));
    }

    [Fact]
    public void HostRecordCodec_RoundTrip_OwnersIpv4AndTtl()
    {
        var codec = _provider.Get(DnsmasqConfKeys.HostRecord);
        const string raw = "host.example.com,www.host.example.com,192.168.1.10,120";
        var row = codec.Parse(new ValueWithSource(raw, null), 0);
        Assert.Equal(raw, codec.Serialize(row));
        var p = Assert.IsType<HostRecordPayload>(row.Payload);
        Assert.Equal(new[] { "host.example.com", "www.host.example.com" }, p.Owners);
        Assert.Equal("192.168.1.10", p.IPv4);
        Assert.Equal(120, p.Ttl);
    }

    [Fact]
    public void SrvCodec_RoundTrip_PreservesValue()
    {
        var codec = _provider.Get(DnsmasqConfKeys.Srv);
        const string raw = "_sip._tcp.example.com,host.example.com,5060,10,5";
        var row = codec.Parse(new ValueWithSource(raw, null), 0);
        Assert.Equal(raw, codec.Serialize(row));
        var p = Assert.IsType<SrvPayload>(row.Payload);
        Assert.Equal("_sip._tcp.example.com", p.ServiceName);
        Assert.Equal("host.example.com", p.Target);
        Assert.Equal(5060, p.Port);
        Assert.Equal(10, p.Priority);
        Assert.Equal(5, p.Weight);
    }

    [Fact]
    public void SrvCodec_RoundTrip_ServiceAndTargetOnly_NoTrailingZeros()
    {
        var codec = _provider.Get(DnsmasqConfKeys.Srv);
        const string raw = "_sip._tcp.example.com,host.example.com";
        var row = codec.Parse(new ValueWithSource(raw, null), 0);
        Assert.Equal(raw, codec.Serialize(row));
        var p = Assert.IsType<SrvPayload>(row.Payload);
        Assert.Null(p.Port);
        Assert.Null(p.Priority);
        Assert.Null(p.Weight);
    }

    [Fact]
    public void SrvCodec_RoundTrip_ServiceTargetPort_OmitsPriorityAndWeight()
    {
        var codec = _provider.Get(DnsmasqConfKeys.Srv);
        const string raw = "_http._tcp.example.com,host.example.com,80";
        var row = codec.Parse(new ValueWithSource(raw, null), 0);
        Assert.Equal(raw, codec.Serialize(row));
        var p = Assert.IsType<SrvPayload>(row.Payload);
        Assert.Equal(80, p.Port);
        Assert.Null(p.Priority);
        Assert.Null(p.Weight);
    }

    [Fact]
    public void Provider_ListsEveryDnsRecordsSectionOptionExceptAdjunctFlags()
    {
        var sectionOptions = Sections
            .First(s => s.SectionId == SectionDnsRecords)
            .OptionNames;

        foreach (var name in sectionOptions)
        {
            if (name is DnsmasqConfKeys.MxTarget or DnsmasqConfKeys.Localmx or DnsmasqConfKeys.Selfmx)
                continue;
            Assert.True(_provider.TryGet(name, out var c) && c != null, $"Missing codec for {name}");
        }
    }
}
