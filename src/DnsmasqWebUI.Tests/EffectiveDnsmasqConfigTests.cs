using DnsmasqWebUI.Models.EffectiveConfig;

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
            ServerLocalValues: Array.Empty<string>(),
            AddressValues: Array.Empty<string>(),
            Interfaces: Array.Empty<string>(),
            ListenAddresses: Array.Empty<string>(),
            ExceptInterfaces: Array.Empty<string>(),
            DhcpRanges: Array.Empty<string>(),
            DhcpHostLines: Array.Empty<string>(),
            DhcpOptionLines: Array.Empty<string>(),
            ResolvFiles: Array.Empty<string>(),
            ExpandHosts: false,
            BogusPriv: false,
            StrictOrder: false,
            NoResolv: false,
            DomainNeeded: false,
            NoPoll: false,
            BindInterfaces: false,
            NoNegcache: false,
            DhcpAuthoritative: false,
            LeasefileRo: false,
            DhcpLeaseFilePath: null,
            CacheSize: null,
            Port: null,
            LocalTtl: null,
            PidFilePath: null,
            User: null,
            Group: null,
            LogFacility: logFacility,
            DhcpLeaseMax: null,
            NegTtl: null,
            MaxTtl: null,
            MaxCacheTtl: null,
            MinCacheTtl: null,
            DhcpTtl: null
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
        var config = CreateConfig(logFacility: "/data/dnsmasq.log");
        Assert.Equal("/data/dnsmasq.log", EffectiveDnsmasqConfig.GetLogsPath(config));
    }

    [Fact]
    public void GetLogsPath_AbsolutePathWithTrim_ReturnsPath()
    {
        var config = CreateConfig(logFacility: "  /var/log/dnsmasq.log  ");
        Assert.Equal("/var/log/dnsmasq.log", EffectiveDnsmasqConfig.GetLogsPath(config));
    }

    [Fact]
    public void GetLogsPath_RelativePathWithSlash_ReturnsPath()
    {
        var config = CreateConfig(logFacility: "logs/dnsmasq.log");
        Assert.Equal("logs/dnsmasq.log", EffectiveDnsmasqConfig.GetLogsPath(config));
    }

    [Fact]
    public void GetLogsPath_SyslogFacilityName_ReturnsNull()
    {
        var config = CreateConfig(logFacility: "local0");
        Assert.Null(EffectiveDnsmasqConfig.GetLogsPath(config));
    }

    [Fact]
    public void GetLogsPath_Stderr_ReturnsNull()
    {
        var config = CreateConfig(logFacility: "-");
        Assert.Null(EffectiveDnsmasqConfig.GetLogsPath(config));
    }
}
