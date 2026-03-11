using DnsmasqWebUI.Models.Dnsmasq;

namespace DnsmasqWebUI.Infrastructure.Services.EffectiveConfig.Metadata;

/// <summary>Dnsmasq compile-time feature required for certain options.</summary>
public enum DnsmasqFeature
{
    Dhcp,
    Tftp,
    Dnssec,
    Dbus
}

/// <summary>
/// Maps effective-config option names to the dnsmasq compile-time feature they require.
/// Used to disable option editors and block writes when the running dnsmasq binary does not support the feature.
/// </summary>
public static class EffectiveConfigFeatureRequirements
{
    private static readonly IReadOnlyDictionary<string, DnsmasqFeature> RequiredByOption =
        new Dictionary<string, DnsmasqFeature>(StringComparer.Ordinal)
        {
            // DNSSEC
            [DnsmasqConfKeys.Dnssec] = DnsmasqFeature.Dnssec,
            [DnsmasqConfKeys.DnssecCheckUnsigned] = DnsmasqFeature.Dnssec,
            [DnsmasqConfKeys.ProxyDnssec] = DnsmasqFeature.Dnssec,
            [DnsmasqConfKeys.TrustAnchor] = DnsmasqFeature.Dnssec,
            [DnsmasqConfKeys.DnssecNoTimecheck] = DnsmasqFeature.Dnssec,
            [DnsmasqConfKeys.DnssecDebug] = DnsmasqFeature.Dnssec,
            [DnsmasqConfKeys.DnssecTimestamp] = DnsmasqFeature.Dnssec,
            [DnsmasqConfKeys.DnssecLimits] = DnsmasqFeature.Dnssec,
            // DHCP
            [DnsmasqConfKeys.DhcpLeasefile] = DnsmasqFeature.Dhcp,
            [DnsmasqConfKeys.DhcpLeaseMax] = DnsmasqFeature.Dhcp,
            [DnsmasqConfKeys.DhcpRange] = DnsmasqFeature.Dhcp,
            [DnsmasqConfKeys.DhcpHost] = DnsmasqFeature.Dhcp,
            [DnsmasqConfKeys.DhcpOption] = DnsmasqFeature.Dhcp,
            [DnsmasqConfKeys.DhcpOptionForce] = DnsmasqFeature.Dhcp,
            [DnsmasqConfKeys.DhcpAuthoritative] = DnsmasqFeature.Dhcp,
            [DnsmasqConfKeys.DhcpRapidCommit] = DnsmasqFeature.Dhcp,
            [DnsmasqConfKeys.DhcpGenerateNames] = DnsmasqFeature.Dhcp,
            [DnsmasqConfKeys.DhcpBroadcast] = DnsmasqFeature.Dhcp,
            [DnsmasqConfKeys.DhcpBoot] = DnsmasqFeature.Dhcp,
            [DnsmasqConfKeys.DhcpScript] = DnsmasqFeature.Dhcp,
            [DnsmasqConfKeys.DhcpHostsfile] = DnsmasqFeature.Dhcp,
            [DnsmasqConfKeys.DhcpOptsfile] = DnsmasqFeature.Dhcp,
            [DnsmasqConfKeys.DhcpHostsdir] = DnsmasqFeature.Dhcp,
            [DnsmasqConfKeys.DhcpOptsdir] = DnsmasqFeature.Dhcp,
            [DnsmasqConfKeys.Leasequery] = DnsmasqFeature.Dhcp,
            [DnsmasqConfKeys.LeasefileRo] = DnsmasqFeature.Dhcp,
            [DnsmasqConfKeys.DhcpTtl] = DnsmasqFeature.Dhcp,
            [DnsmasqConfKeys.DhcpMatch] = DnsmasqFeature.Dhcp,
            [DnsmasqConfKeys.DhcpIgnore] = DnsmasqFeature.Dhcp,
            [DnsmasqConfKeys.DhcpIgnoreNames] = DnsmasqFeature.Dhcp,
            [DnsmasqConfKeys.DhcpRelay] = DnsmasqFeature.Dhcp,
            [DnsmasqConfKeys.DhcpSequentialIp] = DnsmasqFeature.Dhcp,
            [DnsmasqConfKeys.DhcpIgnoreClid] = DnsmasqFeature.Dhcp,
            [DnsmasqConfKeys.BootpDynamic] = DnsmasqFeature.Dhcp,
            [DnsmasqConfKeys.NoPing] = DnsmasqFeature.Dhcp,
            [DnsmasqConfKeys.ScriptArp] = DnsmasqFeature.Dhcp,
            [DnsmasqConfKeys.ScriptOnRenewal] = DnsmasqFeature.Dhcp,
            [DnsmasqConfKeys.DhcpNoOverride] = DnsmasqFeature.Dhcp,
            [DnsmasqConfKeys.DhcpAlternatePort] = DnsmasqFeature.Dhcp,
            [DnsmasqConfKeys.DhcpDuid] = DnsmasqFeature.Dhcp,
            [DnsmasqConfKeys.DhcpLuascript] = DnsmasqFeature.Dhcp,
            [DnsmasqConfKeys.DhcpScriptuser] = DnsmasqFeature.Dhcp,
            [DnsmasqConfKeys.DhcpMac] = DnsmasqFeature.Dhcp,
            [DnsmasqConfKeys.DhcpNameMatch] = DnsmasqFeature.Dhcp,
            [DnsmasqConfKeys.DhcpVendorclass] = DnsmasqFeature.Dhcp,
            [DnsmasqConfKeys.DhcpUserclass] = DnsmasqFeature.Dhcp,
            [DnsmasqConfKeys.RaParam] = DnsmasqFeature.Dhcp,
            [DnsmasqConfKeys.Slaac] = DnsmasqFeature.Dhcp,
            [DnsmasqConfKeys.DhcpCircuitid] = DnsmasqFeature.Dhcp,
            [DnsmasqConfKeys.DhcpRemoteid] = DnsmasqFeature.Dhcp,
            [DnsmasqConfKeys.DhcpSubscrid] = DnsmasqFeature.Dhcp,
            [DnsmasqConfKeys.DhcpProxy] = DnsmasqFeature.Dhcp,
            [DnsmasqConfKeys.DhcpPxeVendor] = DnsmasqFeature.Dhcp,
            [DnsmasqConfKeys.DhcpOptionPxe] = DnsmasqFeature.Dhcp,
            // TFTP
            [DnsmasqConfKeys.EnableTftp] = DnsmasqFeature.Tftp,
            [DnsmasqConfKeys.TftpSecure] = DnsmasqFeature.Tftp,
            [DnsmasqConfKeys.TftpNoFail] = DnsmasqFeature.Tftp,
            [DnsmasqConfKeys.TftpNoBlocksize] = DnsmasqFeature.Tftp,
            [DnsmasqConfKeys.TftpRoot] = DnsmasqFeature.Tftp,
            [DnsmasqConfKeys.PxePrompt] = DnsmasqFeature.Tftp,
            [DnsmasqConfKeys.PxeService] = DnsmasqFeature.Tftp,
            // DBus
            [DnsmasqConfKeys.EnableDbus] = DnsmasqFeature.Dbus,
        };

    public static DnsmasqFeature? GetRequiredFeature(string optionName) =>
        RequiredByOption.TryGetValue(optionName, out var f) ? f : null;

    public static bool IsSupported(DnsmasqFeature feature, DnsmasqServiceStatus? status)
    {
        if (status == null) return true;
        return feature switch
        {
            DnsmasqFeature.Dhcp => status.DnsmasqSupportsDhcp,
            DnsmasqFeature.Tftp => status.DnsmasqSupportsTftp,
            DnsmasqFeature.Dnssec => status.DnsmasqSupportsDnssec,
            DnsmasqFeature.Dbus => status.DnsmasqSupportsDbus,
            _ => true
        };
    }

    /// <summary>User-facing tooltip when an option is disabled because the running dnsmasq was built without the required feature.</summary>
    public static string GetUnsupportedReason(DnsmasqFeature feature) => feature switch
    {
        DnsmasqFeature.Dhcp => "Your dnsmasq was built without DHCP support, so this option can't be used. Install or build dnsmasq with DHCP enabled to change it.",
        DnsmasqFeature.Tftp => "Your dnsmasq was built without TFTP support, so this option can't be used. Install or build dnsmasq with TFTP enabled to change it.",
        DnsmasqFeature.Dnssec => "Your dnsmasq was built without DNSSEC support, so this option can't be used. Install or build dnsmasq with DNSSEC enabled to change it.",
        DnsmasqFeature.Dbus => "Your dnsmasq was built without DBus support, so this option can't be used. Install or build dnsmasq with DBus enabled to change it.",
        _ => "This option isn't available with your current dnsmasq build."
    };

    /// <summary>Returns (isDisabled, reason) for the option given current status. Used to set descriptor capability flags.</summary>
    public static (bool IsDisabled, string? Reason) GetCapabilityDisabled(string optionName, DnsmasqServiceStatus? status)
    {
        var feature = GetRequiredFeature(optionName);
        if (feature == null || status == null) return (false, null);
        if (IsSupported(feature.Value, status)) return (false, null);
        return (true, GetUnsupportedReason(feature.Value));
    }

    /// <summary>True when the option is supported by the given compile-time capabilities (for save-path guard).</summary>
    public static bool IsSupportedByCapabilities(string optionName, DnsmasqCompileCapabilities capabilities)
    {
        var feature = GetRequiredFeature(optionName);
        if (feature == null) return true;
        return feature.Value switch
        {
            DnsmasqFeature.Dhcp => capabilities.Dhcp,
            DnsmasqFeature.Tftp => capabilities.Tftp,
            DnsmasqFeature.Dnssec => capabilities.Dnssec,
            DnsmasqFeature.Dbus => capabilities.Dbus,
            _ => true
        };
    }
}
