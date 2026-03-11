using DnsmasqWebUI.Infrastructure.Services.EffectiveConfig.Metadata;
using DnsmasqWebUI.Models.Dnsmasq;
using DnsmasqWebUI.Models.Dnsmasq.EffectiveConfig;

namespace DnsmasqWebUI.Tests.Services.EffectiveConfig;

/// <summary>
/// Tests for option -> feature mapping and capability checks.
/// </summary>
public class EffectiveConfigFeatureRequirementsTests
{
    [Theory]
    [InlineData(DnsmasqConfKeys.Dnssec, DnsmasqFeature.Dnssec)]
    [InlineData(DnsmasqConfKeys.DnssecCheckUnsigned, DnsmasqFeature.Dnssec)]
    [InlineData(DnsmasqConfKeys.TrustAnchor, DnsmasqFeature.Dnssec)]
    [InlineData(DnsmasqConfKeys.DhcpRange, DnsmasqFeature.Dhcp)]
    [InlineData(DnsmasqConfKeys.EnableTftp, DnsmasqFeature.Tftp)]
    [InlineData(DnsmasqConfKeys.EnableDbus, DnsmasqFeature.Dbus)]
    public void GetRequiredFeature_ReturnsExpectedFeature(string optionName, DnsmasqFeature expected)
    {
        var feature = EffectiveConfigFeatureRequirements.GetRequiredFeature(optionName);
        Assert.NotNull(feature);
        Assert.Equal(expected, feature.Value);
    }

    [Fact]
    public void GetRequiredFeature_ForUnknownOption_ReturnsNull()
    {
        var feature = EffectiveConfigFeatureRequirements.GetRequiredFeature("no-such-option");
        Assert.Null(feature);
    }

    [Fact]
    public void IsSupportedByCapabilities_WhenCapabilityPresent_ReturnsTrue()
    {
        var caps = new DnsmasqCompileCapabilities(true, true, true, false,
            new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "DHCP", "TFTP", "DNSSEC" });
        Assert.True(EffectiveConfigFeatureRequirements.IsSupportedByCapabilities(DnsmasqConfKeys.DnssecCheckUnsigned, caps));
        Assert.True(EffectiveConfigFeatureRequirements.IsSupportedByCapabilities(DnsmasqConfKeys.DhcpRange, caps));
    }

    [Fact]
    public void IsSupportedByCapabilities_WhenDnssecMissing_ReturnsFalseForDnssecOptions()
    {
        var caps = new DnsmasqCompileCapabilities(true, true, false, false,
            new HashSet<string>(StringComparer.OrdinalIgnoreCase));
        Assert.False(EffectiveConfigFeatureRequirements.IsSupportedByCapabilities(DnsmasqConfKeys.Dnssec, caps));
        Assert.False(EffectiveConfigFeatureRequirements.IsSupportedByCapabilities(DnsmasqConfKeys.DnssecCheckUnsigned, caps));
    }

    [Fact]
    public void IsSupportedByCapabilities_ForOptionWithNoRequiredFeature_ReturnsTrue()
    {
        var caps = new DnsmasqCompileCapabilities(false, false, false, false,
            new HashSet<string>(StringComparer.OrdinalIgnoreCase));
        Assert.True(EffectiveConfigFeatureRequirements.IsSupportedByCapabilities(DnsmasqConfKeys.Port, caps));
    }

    [Fact]
    public void GetCapabilityDisabled_WhenStatusSupportsFeature_ReturnsNotDisabled()
    {
        var status = CreateStatusWithCapabilities(dnssec: true);
        var (isDisabled, reason) = EffectiveConfigFeatureRequirements.GetCapabilityDisabled(DnsmasqConfKeys.DnssecCheckUnsigned, status);
        Assert.False(isDisabled);
        Assert.Null(reason);
    }

    [Fact]
    public void GetCapabilityDisabled_WhenStatusMissingDnssec_ReturnsDisabledWithReason()
    {
        var status = CreateStatusWithCapabilities(dnssec: false);
        var (isDisabled, reason) = EffectiveConfigFeatureRequirements.GetCapabilityDisabled(DnsmasqConfKeys.DnssecCheckUnsigned, status);
        Assert.True(isDisabled);
        Assert.NotNull(reason);
        Assert.Contains("DNSSEC", reason, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void GetCapabilityDisabled_WhenStatusNull_ReturnsNotDisabled()
    {
        var (isDisabled, _) = EffectiveConfigFeatureRequirements.GetCapabilityDisabled(DnsmasqConfKeys.DnssecCheckUnsigned, null);
        Assert.False(isDisabled);
    }

    /// <summary>
    /// Save path uses IsSupportedByCapabilities(optionName, version.Capabilities) to reject changes before write.
    /// This test asserts that logic: a build without DNSSEC would reject dnssec-check-unsigned.
    /// </summary>
    [Fact]
    public void SaveGuard_WhenBuildHasNoDnssec_DnssecOptionChangesWouldBeRejected()
    {
        var noDnssecCaps = new DnsmasqCompileCapabilities(true, true, false, false,
            new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "DHCP", "TFTP" });
        Assert.False(EffectiveConfigFeatureRequirements.IsSupportedByCapabilities(DnsmasqConfKeys.DnssecCheckUnsigned, noDnssecCaps));
        var reason = EffectiveConfigFeatureRequirements.GetUnsupportedReason(DnsmasqFeature.Dnssec);
        Assert.Contains("DNSSEC", reason, StringComparison.OrdinalIgnoreCase);
    }

    private static DnsmasqServiceStatus CreateStatusWithCapabilities(bool dhcp = true, bool tftp = true, bool dnssec = true, bool dbus = false)
    {
        return new DnsmasqServiceStatus(
            SystemHostsPath: null,
            SystemHostsPathExists: false,
            ManagedHostsFilePath: null,
            ManagedHostsPathExists: false,
            NoHosts: false,
            AddnHostsPaths: Array.Empty<string>(),
            EffectiveConfig: null,
            EffectiveConfigSources: null,
            MainConfigPath: null,
            ManagedFilePath: null,
            LeasesPath: null,
            MainConfigPathExists: false,
            ManagedFilePathExists: false,
            LeasesPathConfigured: false,
            LeasesPathExists: false,
            ConfigFiles: null,
            ReloadCommandConfigured: false,
            StatusCommandConfigured: false,
            StatusShowConfigured: false,
            LogsConfigured: false,
            LogsPath: null,
            StatusShowCommand: null,
            LogsCommand: null,
            DnsmasqStatus: "active",
            StatusCommandExitCode: null,
            StatusCommandStdout: null,
            StatusCommandStderr: null,
            StatusShowOutput: null,
            LogsOutput: null,
            DhcpRangeStart: null,
            DhcpRangeEnd: null,
            DnsmasqVersion: "2.91",
            MinimumSupportedDnsmasqVersion: "2.91",
            DnsmasqVersionSupported: true,
            DnsmasqVersionError: null,
            DnsmasqSupportsDhcp: dhcp,
            DnsmasqSupportsTftp: tftp,
            DnsmasqSupportsDnssec: dnssec,
            DnsmasqSupportsDbus: dbus);
    }
}
