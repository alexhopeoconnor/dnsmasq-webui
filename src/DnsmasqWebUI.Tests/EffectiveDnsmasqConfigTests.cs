using DnsmasqWebUI.Models.Dnsmasq.EffectiveConfig;

namespace DnsmasqWebUI.Tests;

/// <summary>
/// Tests for EffectiveDnsmasqConfig, including GetLogsPath (log-facility as file path).
/// </summary>
public class EffectiveDnsmasqConfigTests
{
    private static EffectiveDnsmasqConfig CreateConfig(string? logFacility = null) =>
        new(
            NoHosts: false,
            AddnHostsPaths: Array.Empty<string>(),
            HostsdirPath: null,
            ServerLocalValues: Array.Empty<string>(),
            RevServerValues: Array.Empty<string>(),
            AddressValues: Array.Empty<string>(),
            Interfaces: Array.Empty<string>(),
            ListenAddresses: Array.Empty<string>(),
            ExceptInterfaces: Array.Empty<string>(),
            DhcpRanges: Array.Empty<string>(),
            DhcpHostLines: Array.Empty<string>(),
            DhcpOptionLines: Array.Empty<string>(),
            DhcpMatchValues: Array.Empty<string>(),
            DhcpBootValues: Array.Empty<string>(),
            DhcpIgnoreValues: Array.Empty<string>(),
            DhcpVendorclassValues: Array.Empty<string>(),
            DhcpUserclassValues: Array.Empty<string>(),
            RaParamValues: Array.Empty<string>(),
            SlaacValues: Array.Empty<string>(),
            PxeServiceValues: Array.Empty<string>(),
            TrustAnchorValues: Array.Empty<string>(),
            ResolvFiles: Array.Empty<string>(),
            RebindDomainOkValues: Array.Empty<string>(),
            BogusNxdomainValues: Array.Empty<string>(),
            IgnoreAddressValues: Array.Empty<string>(),
            AliasValues: Array.Empty<string>(),
            FilterRrValues: Array.Empty<string>(),
            CacheRrValues: Array.Empty<string>(),
            AuthServerValues: Array.Empty<string>(),
            NoDhcpInterfaceValues: Array.Empty<string>(),
            NoDhcpv4InterfaceValues: Array.Empty<string>(),
            NoDhcpv6InterfaceValues: Array.Empty<string>(),
            DomainValues: Array.Empty<string>(), CnameValues: Array.Empty<string>(), MxHostValues: Array.Empty<string>(), SrvValues: Array.Empty<string>(),
            PtrRecordValues: Array.Empty<string>(), TxtRecordValues: Array.Empty<string>(), NaptrRecordValues: Array.Empty<string>(),
            HostRecordValues: Array.Empty<string>(), DynamicHostValues: Array.Empty<string>(), InterfaceNameValues: Array.Empty<string>(),
            DhcpOptionForceLines: Array.Empty<string>(),
            IpsetValues: Array.Empty<string>(),
            NftsetValues: Array.Empty<string>(),
            DhcpMacValues: Array.Empty<string>(),
            DhcpNameMatchValues: Array.Empty<string>(),
            DhcpIgnoreNamesValues: Array.Empty<string>(),
            ExpandHosts: false,
            BogusPriv: false,
            StrictOrder: false,
            AllServers: false,
            NoResolv: false,
            DomainNeeded: false,
            NoPoll: false,
            BindInterfaces: false,
            BindDynamic: false,
            NoNegcache: false,
            DnsLoopDetect: false,
            StopDnsRebind: false,
            RebindLocalhostOk: false,
            ClearOnReload: false,
            Filterwin2k: false,
            FilterA: false,
            FilterAaaa: false,
            LocaliseQueries: false,
            LogDebug: false,
            DhcpAuthoritative: false,
            LeasefileRo: false,
            EnableTftp: false,
            TftpSecure: false,
            TftpNoFail: false,
            TftpNoBlocksize: false,
            Dnssec: false,
            DnssecCheckUnsigned: false,
            ReadEthers: false,
            DhcpRapidCommit: false,
            Localmx: false,
            Selfmx: false,
            EnableRa: false,
            LogDhcp: false,
            DhcpLeaseFilePath: null,
            CacheSize: null,
            Port: null,
            LocalTtl: null,
            PidFilePath: null,
            User: null,
            Group: null,
            LogFacility: logFacility,
            LogQueries: null,
            AuthTtl: null,
            EdnsPacketMax: null,
            QueryPort: null,
            PortLimit: null,
            MinPort: null,
            MaxPort: null,
            LogAsync: null,
            LocalService: null,
            DhcpLeaseMax: null,
            NegTtl: null,
            MaxTtl: null,
            MaxCacheTtl: null,
            MinCacheTtl: null,
            DhcpTtl: null,
            TftpRootPath: null,
            PxePrompt: null,
            EnableDbus: null,
            EnableUbus: null,
            FastDnsRetry: null,
            DhcpScriptPath: null,
            MxTarget: null
        );

    [Fact]
    public void GetLogsPath_NullConfig_ReturnsNull()
    {
        Assert.Null(EffectiveDnsmasqConfig.GetLogsPath(null));
    }

    [Fact]
    public void GetLogsPath_NullLogFacility_ReturnsNull()
    {
        var config = CreateConfig(logFacility: null);
        Assert.Null(EffectiveDnsmasqConfig.GetLogsPath(config));
    }

    [Fact]
    public void GetLogsPath_EmptyLogFacility_ReturnsNull()
    {
        var config = CreateConfig(logFacility: "");
        Assert.Null(EffectiveDnsmasqConfig.GetLogsPath(config));
    }

    [Fact]
    public void GetLogsPath_WhitespaceLogFacility_ReturnsNull()
    {
        var config = CreateConfig(logFacility: "   ");
        Assert.Null(EffectiveDnsmasqConfig.GetLogsPath(config));
    }

    [Fact]
    public void GetLogsPath_AbsolutePath_ReturnsPath()
    {
        var path = "/data/dnsmasq.log";
        var config = CreateConfig(logFacility: path);
        Assert.Equal(path, EffectiveDnsmasqConfig.GetLogsPath(config));
    }

    [Fact]
    public void GetLogsPath_AbsolutePathWithTrim_ReturnsPath()
    {
        var path = "/var/log/dnsmasq.log";
        var pathWithSpaces = $"  {path}  ";
        var config = CreateConfig(logFacility: pathWithSpaces);
        Assert.Equal(path, EffectiveDnsmasqConfig.GetLogsPath(config));
    }

    [Fact]
    public void GetLogsPath_RelativePathWithSlash_ReturnsPath()
    {
        var path = "logs/dnsmasq.log";
        var config = CreateConfig(logFacility: path);
        Assert.Equal(path, EffectiveDnsmasqConfig.GetLogsPath(config));
    }

    [Fact]
    public void GetLogsPath_SyslogFacilityName_ReturnsNull()
    {
        var facility = "local0";
        var config = CreateConfig(logFacility: facility);
        Assert.Null(EffectiveDnsmasqConfig.GetLogsPath(config));
    }

    [Fact]
    public void GetLogsPath_Stderr_ReturnsNull()
    {
        var facility = "-";
        var config = CreateConfig(logFacility: facility);
        Assert.Null(EffectiveDnsmasqConfig.GetLogsPath(config));
    }
}
