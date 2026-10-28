using DnsmasqWebUI.Infrastructure.Services.EffectiveConfig.Metadata;
using DnsmasqWebUI.Models.Dnsmasq.EffectiveConfig;

namespace DnsmasqWebUI.Tests.Services.EffectiveConfig;

public class EffectiveConfigManagedOverridePolicyTests
{
    private static readonly ConfigValueSource ExternalSource =
        new("/etc/dnsmasq.d/10-base.conf", "10-base.conf", IsManaged: false, LineNumber: 12);

    [Theory]
    [InlineData(DnsmasqConfKeys.Port)]
    [InlineData(DnsmasqConfKeys.User)]
    [InlineData(DnsmasqConfKeys.LogFacility)]
    public void GetEditability_ExternalLastWinsSingleValue_AllowsManagedOverride(string optionName)
    {
        var result = EffectiveConfigManagedOverridePolicy.GetEditability(
            optionName,
            isMultiValue: false,
            ExternalSource);

        Assert.Equal(EffectiveConfigFieldEditability.ManagedOverrideAvailable, result);
        Assert.True(EffectiveConfigManagedOverridePolicy.CanCreateManagedOverride(optionName, false, ExternalSource));
        Assert.True(EffectiveConfigManagedOverridePolicy.CanEditFromUi(optionName, false, ExternalSource));
    }

    [Theory]
    [InlineData(DnsmasqConfKeys.UseStaleCache)]
    [InlineData(DnsmasqConfKeys.AddMac)]
    [InlineData(DnsmasqConfKeys.DhcpGenerateNames)]
    public void GetEditability_ExternalLastWinsKeyOnlyOrValue_AllowsManagedOverride(string optionName)
    {
        var result = EffectiveConfigManagedOverridePolicy.GetEditability(
            optionName,
            isMultiValue: false,
            ExternalSource);

        Assert.Equal(EffectiveConfigFieldEditability.ManagedOverrideAvailable, result);
    }

    [Theory]
    [InlineData(DnsmasqConfKeys.ExpandHosts)]
    [InlineData(DnsmasqConfKeys.NoResolv)]
    [InlineData(DnsmasqConfKeys.Do0x20Encode)]
    public void GetEditability_ExternalFlagsAndInversePairs_RemainsReadOnly(string optionName)
    {
        var result = EffectiveConfigManagedOverridePolicy.GetEditability(
            optionName,
            isMultiValue: false,
            ExternalSource);

        Assert.Equal(EffectiveConfigFieldEditability.ReadOnly, result);
        Assert.False(EffectiveConfigManagedOverridePolicy.CanCreateManagedOverride(optionName, false, ExternalSource));
    }

    [Theory]
    [InlineData(DnsmasqConfKeys.Server)]
    [InlineData(DnsmasqConfKeys.Domain)]
    [InlineData(DnsmasqConfKeys.DhcpHost)]
    public void GetEditability_ExternalMultiValue_RemainsReadOnly(string optionName)
    {
        var result = EffectiveConfigManagedOverridePolicy.GetEditability(
            optionName,
            isMultiValue: true,
            ExternalSource);

        Assert.Equal(EffectiveConfigFieldEditability.ReadOnly, result);
        Assert.False(EffectiveConfigManagedOverridePolicy.CanEditFromUi(optionName, true, ExternalSource));
    }

    [Fact]
    public void GetEditability_ManagedOrUnsetValue_IsEditable()
    {
        var managed = ExternalSource with { IsManaged = true };

        Assert.Equal(
            EffectiveConfigFieldEditability.Editable,
            EffectiveConfigManagedOverridePolicy.GetEditability(DnsmasqConfKeys.Port, false, managed));
        Assert.Equal(
            EffectiveConfigFieldEditability.Editable,
            EffectiveConfigManagedOverridePolicy.GetEditability(DnsmasqConfKeys.Port, false, null));
    }

    [Fact]
    public void GetSourceStateTooltip_ManagedOverride_ExplainsPrecedenceWithoutSuggestingExternalWrite()
    {
        var tooltip = EffectiveConfigManagedOverridePolicy.GetSourceStateTooltip(
            DnsmasqConfKeys.Port,
            isMultiValue: false,
            ExternalSource);

        Assert.Equal(
            "From 10-base.conf line 12. Create a managed override to take precedence; /etc/dnsmasq.d/10-base.conf will not be changed.",
            tooltip);
    }
}
