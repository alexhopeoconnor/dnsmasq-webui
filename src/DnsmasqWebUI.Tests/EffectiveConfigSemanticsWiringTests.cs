using DnsmasqWebUI.Infrastructure.Helpers.Config;
using DnsmasqWebUI.Infrastructure.Services.EffectiveConfig;

namespace DnsmasqWebUI.Tests;

/// <summary>
/// Wiring tests that ensure special-option semantics actually feed parser and registry behavior.
/// </summary>
public class EffectiveConfigSemanticsWiringTests
{
    [Fact]
    public void ParserBehaviorMap_UsesSpecialSemantics_ForSpecialOptions()
    {
        var specialOptions = new[]
        {
            DnsmasqConfKeys.UseStaleCache,
            DnsmasqConfKeys.AddMac,
            DnsmasqConfKeys.AddSubnet,
            DnsmasqConfKeys.Umbrella,
            DnsmasqConfKeys.Do0x20Encode,
            DnsmasqConfKeys.ConnmarkAllowlistEnable,
            DnsmasqConfKeys.DnssecCheckUnsigned,
            DnsmasqConfKeys.Leasequery,
            DnsmasqConfKeys.DhcpGenerateNames,
            DnsmasqConfKeys.DhcpBroadcast,
            DnsmasqConfKeys.BootpDynamic,
        };

        foreach (var option in specialOptions)
        {
            var semantics = EffectiveConfigSpecialOptionSemantics.TryGetSemantics(option);
            Assert.NotNull(semantics);
            Assert.Equal(semantics!.ParserBehavior, EffectiveConfigParserBehaviorMap.GetBehavior(option));
        }
    }

    [Fact]
    public void Registry_WiresLeasequeryMultiValidator_FromSpecialSemantics()
    {
        var registry = new EffectiveConfigRenderFragmentRegistry();
        var factory = registry.GetMultiDescriptorFactory(EffectiveConfigFieldBuilder.SectionDhcp, DnsmasqConfKeys.Leasequery);
        Assert.NotNull(factory);

        var descriptor = factory!(
            EffectiveConfigFieldBuilder.SectionDhcp,
            DnsmasqConfKeys.Leasequery,
            status: null,
            getItems: _ => null);

        Assert.NotNull(descriptor.Validator);
        Assert.NotNull(descriptor.Validator!.ValidateItem("not-an-ip", Array.Empty<string>()));
        Assert.Null(descriptor.Validator.ValidateItem("10.0.0.0/24", Array.Empty<string>()));
    }
}
