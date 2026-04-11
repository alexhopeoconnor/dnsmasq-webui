using DnsmasqWebUI.Infrastructure.Services.Dnsmasq.DnsRecords;
using DnsmasqWebUI.Infrastructure.Services.Dnsmasq.DnsRecords.Abstractions;
using DnsmasqWebUI.Infrastructure.Services.EffectiveConfig;
using DnsmasqWebUI.Infrastructure.Services.EffectiveConfig.Metadata;
using DnsmasqWebUI.Infrastructure.Services.EffectiveConfig.Validation.Abstractions;
using DnsmasqWebUI.Models.Dnsmasq;
using DnsmasqWebUI.Models.Dnsmasq.EffectiveConfig;
using DnsmasqWebUI.Models.DnsRecords;

namespace DnsmasqWebUI.Tests.Services.Dnsmasq.DnsRecords;

public class DnsRecordsPageProjectionServiceTests
{
    private readonly DnsRecordsPageProjectionService _sut = new(
        new DnsRecordDirectiveCodecProvider(),
        new NoopOptionSemanticValidator(),
        new EffectiveMultiValueProjectionService());

    [Fact]
    public void BuildRows_ReturnsEmpty_WhenEffectiveConfigNull()
    {
        var status = MinimalStatus() with { EffectiveConfig = null };
        Assert.Empty(_sut.BuildRows(status));
    }

    [Fact]
    public void FilterRows_HidesReadOnly_WhenShowReadOnlyFalse()
    {
        var rows = new[]
        {
            Row("a", DnsmasqConfKeys.Cname, DnsRecordFamily.Cname, "x,y", editable: true),
            Row("b", DnsmasqConfKeys.Cname, DnsRecordFamily.Cname, "p,q", editable: false),
        };
        var q = new DnsRecordsQueryState("", DnsRecordsUiFamily.All, null, ShowReadOnly: false, OnlyWithIssues: false);
        var filtered = _sut.FilterRows(rows, q);
        Assert.Single(filtered);
        Assert.Equal("a", filtered[0].Id);
    }

    [Fact]
    public void FilterRows_SourcePathFilter_MatchesFilePath()
    {
        var src = new ConfigValueSource("/etc/extra.conf", "extra.conf", IsManaged: false);
        var rows = new[]
        {
            Row("a", DnsmasqConfKeys.Cname, DnsRecordFamily.Cname, "x,y", editable: true, source: src),
            Row("b", DnsmasqConfKeys.Cname, DnsRecordFamily.Cname, "p,q", editable: true, source: null),
        };
        var q = new DnsRecordsQueryState("", DnsRecordsUiFamily.All, "/etc/extra.conf", true, false);
        var filtered = _sut.FilterRows(rows, q);
        Assert.Single(filtered);
        Assert.Equal("a", filtered[0].Id);
    }

    [Fact]
    public void FilterRows_Search_MatchesSummaryCaseInsensitive()
    {
        var rows = new[]
        {
            Row("a", DnsmasqConfKeys.Cname, DnsRecordFamily.Cname, "x,y", editable: true, summary: "Alpha summary"),
            Row("b", DnsmasqConfKeys.Cname, DnsRecordFamily.Cname, "p,q", editable: true, summary: "Beta"),
        };
        var q = new DnsRecordsQueryState("alpha", DnsRecordsUiFamily.All, null, true, false);
        var filtered = _sut.FilterRows(rows, q);
        Assert.Single(filtered);
        Assert.Equal("a", filtered[0].Id);
    }

    [Fact]
    public void FilterRows_UiFamily_Advanced_KeepsOnlyAdvancedFamilies()
    {
        var rows = new[]
        {
            Row("c", DnsmasqConfKeys.Cname, DnsRecordFamily.Cname, "x,y", true),
            Row("n", DnsmasqConfKeys.NaptrRecord, DnsRecordFamily.Naptr, "1", true, payload: new NaptrPayload("e.com", "1", "2", "s", "SIP", "!^.*$!", "_sip._udp.example.com")),
        };
        var q = new DnsRecordsQueryState("", DnsRecordsUiFamily.Advanced, null, true, false);
        var filtered = _sut.FilterRows(rows, q);
        Assert.Single(filtered);
        Assert.Equal("n", filtered[0].Id);
    }

    [Fact]
    public void BuildRows_CurrentValuesAccessor_UsesDraftValuesAndManagedSourcePathForUnmatchedRows()
    {
        var status = MinimalStatus() with
        {
            ManagedFilePath = "/managed.conf",
            EffectiveConfig = MinimalConfig() with { CnameValues = ["disk.example,target.example"] },
            EffectiveConfigSources = MinimalSources() with
            {
                CnameValues = [new ValueWithSource("disk.example,target.example", new ConfigValueSource("/etc/dnsmasq.d/base.conf", "base.conf", false, 12))]
            }
        };

        var rows = _sut.BuildRows(status, optionName => optionName == DnsmasqConfKeys.Cname
            ? ["draft.example,target.example"]
            : []);

        var row = Assert.Single(rows);
        Assert.Equal("draft.example,target.example", row.RawValue);
        Assert.True(row.IsDraftOnly);
        Assert.True(row.IsEditable);
        Assert.Equal("/managed.conf", row.SourcePath);
        Assert.Equal("managed.conf", row.SourceLabel);
    }

    private static DnsRecordRow Row(
        string id,
        string optionName,
        DnsRecordFamily family,
        string rawValue,
        bool editable,
        ConfigValueSource? source = null,
        DnsRecordPayload? payload = null,
        string? summary = null,
        IReadOnlyList<DnsRecordIssue>? issues = null) =>
        new(
            id,
            id,
            optionName,
            family,
            IndexInOption: 0,
            rawValue,
            source,
            source?.FilePath,
            source?.FileName,
            false,
            editable,
            payload ?? new CnamePayload(["x"], "y", null),
            issues ?? [],
            summary ?? rawValue);

    private static EffectiveDnsmasqConfig MinimalConfig() =>
        new(
            NoHosts: false,
            AddnHostsPaths: [],
            HostsdirPath: null,
            ServerValues: [],
            LocalValues: [],
            RevServerValues: [],
            AddressValues: [],
            Interfaces: [],
            ListenAddresses: [],
            ExceptInterfaces: [],
            DhcpRanges: [],
            DhcpHostLines: [],
            DhcpOptionLines: [],
            DhcpMatchValues: [],
            DhcpBootValues: [],
            DhcpIgnoreValues: [],
            DhcpVendorclassValues: [],
            DhcpUserclassValues: [],
            RaParamValues: [],
            SlaacValues: [],
            PxeServiceValues: [],
            TrustAnchorValues: [],
            ResolvFiles: [],
            RebindDomainOkValues: [],
            BogusNxdomainValues: [],
            IgnoreAddressValues: [],
            AliasValues: [],
            FilterRrValues: [],
            CacheRrValues: [],
            AuthServerValues: [],
            NoDhcpInterfaceValues: [],
            NoDhcpv4InterfaceValues: [],
            NoDhcpv6InterfaceValues: [],
            DomainValues: [],
            CnameValues: [],
            MxHostValues: [],
            SrvValues: [],
            PtrRecordValues: [],
            TxtRecordValues: [],
            NaptrRecordValues: [],
            HostRecordValues: [],
            DynamicHostValues: [],
            InterfaceNameValues: [],
            DhcpOptionForceLines: [],
            IpsetValues: [],
            NftsetValues: [],
            DhcpMacValues: [],
            DhcpNameMatchValues: [],
            DhcpIgnoreNamesValues: [],
            DhcpHostsfilePaths: [],
            DhcpOptsfilePaths: [],
            DhcpHostsdirPaths: [],
            DhcpOptsdirPaths: [],
            ConnmarkAllowlistValues: [],
            CaaRecordValues: [],
            DnsRrValues: [],
            SynthDomainValues: [],
            AuthZoneValues: [],
            AuthSoaValues: [],
            AuthSecServersValues: [],
            AuthPeerValues: [],
            DhcpRelayValues: [],
            DhcpCircuitidValues: [],
            DhcpRemoteidValues: [],
            DhcpSubscridValues: [],
            DhcpProxyValues: [],
            TagIfValues: [],
            BridgeInterfaceValues: [],
            SharedNetworkValues: [],
            DhcpOptionPxeValues: [],
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
            DnssecCheckUnsigned: null,
            ReadEthers: false,
            DhcpRapidCommit: false,
            Localmx: false,
            Selfmx: false,
            EnableRa: false,
            LogDhcp: false,
            KeepInForeground: false,
            NoDaemon: false,
            ProxyDnssec: false,
            ConnmarkAllowlistEnable: null,
            NoRoundRobin: false,
            DnssecNoTimecheck: false,
            DnssecDebug: false,
            LeasequeryValues: [],
            DhcpGenerateNames: null,
            DhcpBroadcast: null,
            DhcpSequentialIp: false,
            DhcpIgnoreClid: false,
            BootpDynamic: null,
            NoPing: false,
            ScriptArp: false,
            ScriptOnRenewal: false,
            DhcpNoOverride: false,
            QuietDhcp: false,
            QuietDhcp6: false,
            QuietRa: false,
            QuietTftp: false,
            DhcpLeaseFilePath: null,
            CacheSize: null,
            Port: null,
            LocalTtl: null,
            PidFilePath: null,
            User: null,
            Group: null,
            LogFacility: null,
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
            MxTarget: null,
            DnsForwardMax: null,
            DumpfilePath: null,
            Dumpmask: null,
            AddCpeId: null,
            DnssecTimestamp: null,
            DnssecLimits: null,
            DhcpAlternatePort: null,
            DhcpDuid: null,
            DhcpLuascriptPath: null,
            DhcpScriptuser: null,
            DhcpPxeVendor: null,
            UseStaleCache: null,
            AddMac: null,
            StripMac: false,
            AddSubnet: null,
            StripSubnet: false,
            Umbrella: null,
            Do0x20EncodeState: ExplicitToggleState.Default,
            Conntrack: false);

    private static EffectiveConfigSources MinimalSources() =>
        new(
            NoHosts: null,
            AddnHostsPaths: [],
            HostsdirPath: null,
            ServerValues: [],
            LocalValues: [],
            RevServerValues: [],
            AddressValues: [],
            Interfaces: [],
            ListenAddresses: [],
            ExceptInterfaces: [],
            DhcpRanges: [],
            DhcpHostLines: [],
            DhcpOptionLines: [],
            DhcpMatchValues: [],
            DhcpBootValues: [],
            DhcpIgnoreValues: [],
            DhcpVendorclassValues: [],
            DhcpUserclassValues: [],
            RaParamValues: [],
            SlaacValues: [],
            PxeServiceValues: [],
            TrustAnchorValues: [],
            ResolvFiles: [],
            RebindDomainOkValues: [],
            BogusNxdomainValues: [],
            IgnoreAddressValues: [],
            AliasValues: [],
            FilterRrValues: [],
            CacheRrValues: [],
            AuthServerValues: [],
            NoDhcpInterfaceValues: [],
            NoDhcpv4InterfaceValues: [],
            NoDhcpv6InterfaceValues: [],
            DomainValues: [],
            CnameValues: [],
            MxHostValues: [],
            SrvValues: [],
            PtrRecordValues: [],
            TxtRecordValues: [],
            NaptrRecordValues: [],
            HostRecordValues: [],
            DynamicHostValues: [],
            InterfaceNameValues: [],
            DhcpOptionForceLines: [],
            IpsetValues: [],
            NftsetValues: [],
            DhcpMacValues: [],
            DhcpNameMatchValues: [],
            DhcpIgnoreNamesValues: [],
            DhcpHostsfilePaths: [],
            DhcpOptsfilePaths: [],
            DhcpHostsdirPaths: [],
            DhcpOptsdirPaths: [],
            ConnmarkAllowlistValues: [],
            CaaRecordValues: [],
            DnsRrValues: [],
            SynthDomainValues: [],
            AuthZoneValues: [],
            AuthSoaValues: [],
            AuthSecServersValues: [],
            AuthPeerValues: [],
            DhcpRelayValues: [],
            DhcpCircuitidValues: [],
            DhcpRemoteidValues: [],
            DhcpSubscridValues: [],
            DhcpProxyValues: [],
            TagIfValues: [],
            BridgeInterfaceValues: [],
            SharedNetworkValues: [],
            DhcpOptionPxeValues: [],
            ExpandHosts: null,
            BogusPriv: null,
            StrictOrder: null,
            AllServers: null,
            NoResolv: null,
            DomainNeeded: null,
            NoPoll: null,
            BindInterfaces: null,
            BindDynamic: null,
            NoNegcache: null,
            DnsLoopDetect: null,
            StopDnsRebind: null,
            RebindLocalhostOk: null,
            ClearOnReload: null,
            Filterwin2k: null,
            FilterA: null,
            FilterAaaa: null,
            LocaliseQueries: null,
            LogDebug: null,
            DhcpAuthoritative: null,
            LeasefileRo: null,
            EnableTftp: null,
            TftpSecure: null,
            TftpNoFail: null,
            TftpNoBlocksize: null,
            Dnssec: null,
            DnssecCheckUnsigned: null,
            ReadEthers: null,
            DhcpRapidCommit: null,
            Localmx: null,
            Selfmx: null,
            EnableRa: null,
            LogDhcp: null,
            KeepInForeground: null,
            NoDaemon: null,
            ProxyDnssec: null,
            ConnmarkAllowlistEnable: null,
            NoRoundRobin: null,
            DnssecNoTimecheck: null,
            DnssecDebug: null,
            LeasequeryValues: [],
            DhcpGenerateNames: null,
            DhcpBroadcast: null,
            DhcpSequentialIp: null,
            DhcpIgnoreClid: null,
            BootpDynamic: null,
            NoPing: null,
            ScriptArp: null,
            ScriptOnRenewal: null,
            DhcpNoOverride: null,
            QuietDhcp: null,
            QuietDhcp6: null,
            QuietRa: null,
            QuietTftp: null,
            DhcpLeaseFilePath: null,
            CacheSize: null,
            Port: null,
            LocalTtl: null,
            PidFilePath: null,
            User: null,
            Group: null,
            LogFacility: null,
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
            MxTarget: null,
            DnsForwardMax: null,
            DumpfilePath: null,
            Dumpmask: null,
            AddCpeId: null,
            DnssecTimestamp: null,
            DnssecLimits: null,
            DhcpAlternatePort: null,
            DhcpDuid: null,
            DhcpLuascriptPath: null,
            DhcpScriptuser: null,
            DhcpPxeVendor: null,
            UseStaleCache: null,
            AddMac: null,
            StripMac: null,
            AddSubnet: null,
            StripSubnet: null,
            Umbrella: null,
            Do0x20Encode: null,
            Conntrack: null);

    private static DnsmasqServiceStatus MinimalStatus() =>
        new(
            SystemHostsPath: null,
            SystemHostsPathExists: false,
            ManagedHostsFilePath: null,
            ManagedHostsPathExists: false,
            NoHosts: false,
            AddnHostsPaths: [],
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
            DnsmasqVersion: null,
            MinimumSupportedDnsmasqVersion: "2.91",
            DnsmasqVersionSupported: true,
            DnsmasqVersionError: null,
            DnsmasqSupportsDhcp: true,
            DnsmasqSupportsTftp: true,
            DnsmasqSupportsDnssec: true,
            DnsmasqSupportsDbus: false);

    private sealed class NoopOptionSemanticValidator : IOptionSemanticValidator
    {
        public string? ValidateSingle(string optionName, object? value, OptionValidationSemantics semantics) => null;

        public string? ValidateMultiItem(string optionName, string? value, OptionValidationSemantics semantics) => null;
    }
}
