using DnsmasqWebUI.Infrastructure.Services.Dnsmasq.DnsRecords;
using DnsmasqWebUI.Infrastructure.Services.Dnsmasq.DnsRecords.Abstractions;
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
        new NoopOptionSemanticValidator());

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
            optionName,
            family,
            IndexInOption: 0,
            rawValue,
            source,
            editable,
            payload ?? new CnamePayload(["x"], "y", null),
            issues ?? [],
            summary ?? rawValue);

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
