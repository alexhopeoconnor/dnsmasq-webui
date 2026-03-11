using DnsmasqWebUI.Infrastructure.Serialization;
using DnsmasqWebUI.Infrastructure.Services.EffectiveConfig.Metadata;
using DnsmasqWebUI.Models.Dnsmasq.EffectiveConfig;

namespace DnsmasqWebUI.Tests.Serialization;

public class EffectiveConfigDirectiveSerializerTests
{
    private readonly EffectiveConfigDirectiveSerializer _sut = new();

    [Fact]
    public void SerializeSingle_Flag_True_WritesBareKey()
    {
        var result = _sut.SerializeSingle(DnsmasqConfKeys.Conntrack, true);
        Assert.Equal(DnsmasqConfKeys.Conntrack, result);
    }

    [Fact]
    public void SerializeSingle_Flag_False_WritesEmpty()
    {
        var result = _sut.SerializeSingle(DnsmasqConfKeys.Conntrack, false);
        Assert.Equal("", result);
    }

    [Fact]
    public void SerializeSingle_SingleValue_WritesKeyEqualsValue()
    {
        var result = _sut.SerializeSingle(DnsmasqConfKeys.Port, 5353);
        Assert.Equal("port=5353", result);
    }

    [Fact]
    public void SerializeSingle_KeyOnlyOrValue_Empty_WritesBareKey()
    {
        var result = _sut.SerializeSingle(DnsmasqConfKeys.UseStaleCache, "");
        Assert.Equal(DnsmasqConfKeys.UseStaleCache, result);
    }

    [Fact]
    public void SerializeMulti_MultiValue_WritesEachDirective()
    {
        var result = _sut.SerializeMulti(DnsmasqConfKeys.Server, ["1.1.1.1", "8.8.8.8"]);
        Assert.Equal(["server=1.1.1.1", "server=8.8.8.8"], result);
    }

    [Fact]
    public void SerializeMulti_MultiKeyOnlyOrValue_WritesMixedForms()
    {
        var result = _sut.SerializeMulti(DnsmasqConfKeys.Leasequery, ["", "net:tag"]);
        Assert.Equal(["leasequery", "leasequery=net:tag"], result);
    }

    [Fact]
    public void SerializeSingle_InversePair_Enabled_WritesPositiveKey()
    {
        var result = _sut.SerializeSingle(DnsmasqConfKeys.Do0x20Encode, ExplicitToggleState.Enabled);
        Assert.Equal(DnsmasqConfKeys.Do0x20Encode, result);
    }

    [Fact]
    public void SerializeSingle_InversePair_Disabled_WritesNegativeKey()
    {
        var result = _sut.SerializeSingle(DnsmasqConfKeys.Do0x20Encode, ExplicitToggleState.Disabled);
        Assert.Equal(DnsmasqConfKeys.No0x20Encode, result);
    }
}
