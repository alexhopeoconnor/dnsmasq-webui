using DnsmasqWebUI.Infrastructure.Services.EffectiveConfig.Metadata;
using DnsmasqWebUI.Infrastructure.Services.EffectiveConfig.Readonly;
using DnsmasqWebUI.Models.Dnsmasq.EffectiveConfig;

namespace DnsmasqWebUI.Tests.Services.EffectiveConfig;

public class ReadonlyConfigCommandGeneratorTests
{
    [Theory]
    [InlineData(DnsmasqConfKeys.Port, 5353, "port=5353")]
    [InlineData(DnsmasqConfKeys.User, "dnsmasq", "user=dnsmasq")]
    [InlineData(DnsmasqConfKeys.LogQueries, "", "log-queries")]
    public void GetOverrideLine_LastWinsSingleValue_ReturnsManagedOverride(
        string optionName,
        object value,
        string expected)
    {
        var line = ReadonlyConfigCommandGenerator.GetOverrideLine(
            optionName,
            value,
            "/etc/dnsmasq.d/zz-dnsmasq-webui.conf");

        Assert.Equal(expected, line);
    }

    [Theory]
    [InlineData(DnsmasqConfKeys.UseStaleCache, "", "use-stale-cache")]
    [InlineData(DnsmasqConfKeys.UseStaleCache, "60", "use-stale-cache=60")]
    [InlineData(DnsmasqConfKeys.DhcpGenerateNames, "tag:known", "dhcp-generate-names=tag:known")]
    public void GetOverrideLine_LastWinsKeyOnlyOrValue_ReturnsManagedOverride(
        string optionName,
        string value,
        string expected)
    {
        var line = ReadonlyConfigCommandGenerator.GetOverrideLine(
            optionName,
            value,
            "/etc/dnsmasq.d/zz-dnsmasq-webui.conf");

        Assert.Equal(expected, line);
    }

    [Fact]
    public void GetRemoveLineCommand_KeyOnlyOrValue_RemovesBareAndValuedForms()
    {
        var command = ReadonlyConfigCommandGenerator.GetRemoveLineCommand(
            new ConfigValueSource("/etc/dnsmasq.d/external.conf", "external.conf", IsManaged: false),
            DnsmasqConfKeys.LogQueries);

        Assert.Equal(
            "sed -i -e '|^log-queries$|d' -e '|^log-queries=.*$|d' /etc/dnsmasq.d/external.conf",
            command);
    }

    [Theory]
    [InlineData(DnsmasqConfKeys.ExpandHosts, true)]
    [InlineData(DnsmasqConfKeys.Server, "1.1.1.1")]
    [InlineData(DnsmasqConfKeys.Do0x20Encode, ExplicitToggleState.Enabled)]
    public void GetOverrideLine_NonOverridableSourceState_ReturnsNoMisleadingManualOverride(
        string optionName,
        object value)
    {
        var line = ReadonlyConfigCommandGenerator.GetOverrideLine(
            optionName,
            value,
            "/etc/dnsmasq.d/zz-dnsmasq-webui.conf");

        Assert.Equal("", line);
        Assert.Equal("", ReadonlyConfigCommandGenerator.GetOverrideHint(
            optionName,
            value,
            "/etc/dnsmasq.d/zz-dnsmasq-webui.conf"));
    }
}
