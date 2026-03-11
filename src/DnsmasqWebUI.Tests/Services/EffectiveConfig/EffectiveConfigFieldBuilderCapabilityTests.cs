using DnsmasqWebUI.Infrastructure.Services.EffectiveConfig.Metadata;
using DnsmasqWebUI.Models.Dnsmasq;
using DnsmasqWebUI.Models.Dnsmasq.EffectiveConfig;

namespace DnsmasqWebUI.Tests.Services.EffectiveConfig;

/// <summary>
/// Tests that field descriptors get IsCapabilityDisabled set when the status indicates the option is not supported by the dnsmasq build.
/// </summary>
public class EffectiveConfigFieldBuilderCapabilityTests
{
    [Fact]
    public void GetSingleDescriptor_WhenDnssecOptionAndStatusHasNoDnssec_ReturnsDescriptorWithCapabilityDisabled()
    {
        var status = CreateStatusWithCapabilities(dnssec: false);
        var descriptor = EffectiveConfigFieldBuilder.GetSingleDescriptor(
            null,
            DnsmasqConfKeys.DnssecCheckUnsigned,
            status,
            _ => null,
            _ => null,
            null);

        Assert.True(descriptor.IsCapabilityDisabled);
        Assert.NotNull(descriptor.CapabilityDisabledReason);
        Assert.Contains("DNSSEC", descriptor.CapabilityDisabledReason, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void GetSingleDescriptor_WhenDnssecOptionAndStatusHasDnssec_ReturnsDescriptorWithCapabilityNotDisabled()
    {
        var status = CreateStatusWithCapabilities(dnssec: true);
        var descriptor = EffectiveConfigFieldBuilder.GetSingleDescriptor(
            null,
            DnsmasqConfKeys.DnssecCheckUnsigned,
            status,
            _ => null,
            _ => null,
            null);

        Assert.False(descriptor.IsCapabilityDisabled);
        Assert.Null(descriptor.CapabilityDisabledReason);
    }

    [Fact]
    public void GetSingleDescriptor_WhenOptionWithNoFeatureRequirement_ReturnsDescriptorWithCapabilityNotDisabled()
    {
        var status = CreateStatusWithCapabilities(dnssec: false);
        var descriptor = EffectiveConfigFieldBuilder.GetSingleDescriptor(
            null,
            DnsmasqConfKeys.Port,
            status,
            _ => null,
            _ => null,
            null);

        Assert.False(descriptor.IsCapabilityDisabled);
        Assert.Null(descriptor.CapabilityDisabledReason);
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
