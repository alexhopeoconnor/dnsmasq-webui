using DnsmasqWebUI.Infrastructure.Services.EffectiveConfig.Metadata;
using DnsmasqWebUI.Infrastructure.Services.EffectiveConfig.Validation;
using DnsmasqWebUI.Models.Dnsmasq.EffectiveConfig;

namespace DnsmasqWebUI.Tests.Services.EffectiveConfig.CrossOption;

public class EffectiveConfigCrossOptionContextTests
{
    [Fact]
    public void GetMulti_WhenPendingNewValueIsNull_ReturnsEmpty_NotDiskValues()
    {
        var cfg = CrossOptionTestHelpers.BaselineConfig() with { ServerValues = ["1.1.1.1", "8.8.8.8"] };
        var pending = new[]
        {
            new PendingOptionChange("resolver", DnsmasqConfKeys.Server, new List<string> { "1.1.1.1" }, null, null)
        };
        var ctx = new EffectiveConfigCrossOptionContext(CrossOptionTestHelpers.Status(cfg), pending);
        Assert.Empty(ctx.GetMulti(DnsmasqConfKeys.Server, c => c.ServerValues));
    }

    [Fact]
    public void GetMulti_WhenPendingNewValueIsList_ReturnsPending_NotDisk()
    {
        var cfg = CrossOptionTestHelpers.BaselineConfig() with { ServerValues = ["9.9.9.9"] };
        var pending = new[]
        {
            new PendingOptionChange("resolver", DnsmasqConfKeys.Server, null, new List<string> { "1.1.1.1" }, null)
        };
        var ctx = new EffectiveConfigCrossOptionContext(CrossOptionTestHelpers.Status(cfg), pending);
        var v = ctx.GetMulti(DnsmasqConfKeys.Server, c => c.ServerValues);
        Assert.Single(v);
        Assert.Equal("1.1.1.1", v[0]);
    }

    [Fact]
    public void GetMulti_WhenPendingNewValueIsWrongType_ReturnsEmpty_NotDiskValues()
    {
        var cfg = CrossOptionTestHelpers.BaselineConfig() with { ServerValues = ["1.1.1.1"] };
        var pending = new[]
        {
            new PendingOptionChange("resolver", DnsmasqConfKeys.Server, null, "not-a-list", null)
        };
        var ctx = new EffectiveConfigCrossOptionContext(CrossOptionTestHelpers.Status(cfg), pending);
        Assert.Empty(ctx.GetMulti(DnsmasqConfKeys.Server, c => c.ServerValues));
    }
}
