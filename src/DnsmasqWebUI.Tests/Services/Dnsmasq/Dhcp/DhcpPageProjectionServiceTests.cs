using DnsmasqWebUI.Infrastructure.Serialization.OptionHandlers;
using DnsmasqWebUI.Infrastructure.Services.Dnsmasq.Dhcp;
using DnsmasqWebUI.Infrastructure.Services.EffectiveConfig;
using DnsmasqWebUI.Models.Dhcp;
using DnsmasqWebUI.Models.Dhcp.Ui;
using DnsmasqWebUI.Models.Dnsmasq;

namespace DnsmasqWebUI.Tests.Services.Dnsmasq.Dhcp;

public class DhcpPageProjectionServiceTests
{
    private static readonly DhcpPageProjectionService Sut = new(new EffectiveMultiValueProjectionService());
    private static readonly DhcpHostOptionValueHandler Handler = new();

    private static DhcpHostPageRow SyntheticRow(
        int effectiveIndex,
        DhcpSourceKind kind,
        string sourcePath,
        bool editable,
        string mac,
        string? name = null,
        string? address = "192.168.1.10") =>
        new(
            EffectiveIndex: effectiveIndex,
            ValueString: mac,
            RowKey: $"t:{effectiveIndex}",
            OccurrenceId: $"t:{effectiveIndex}",
            SourceKind: kind,
            SourcePath: sourcePath,
            IsDraftOnly: false,
            IsEditable: editable,
            IsActive: true,
            Entry: new DhcpHostEntry
            {
                LineNumber = effectiveIndex + 1,
                MacAddresses = new List<string> { mac },
                Name = name,
                Address = address,
                Lease = "infinite",
                Extra = new List<string>()
            },
            Conflict: null,
            LinkedLease: null,
            MatchesManagedStatic: kind == DhcpSourceKind.Managed && editable);

    [Fact]
    public void BuildHostRows_NullSources_TreatsLinesAsManagedEditable()
    {
        var status = MinimalStatus(null, "/managed.conf", "/etc/dnsmasq.conf");
        var values = new[] { "aa:bb:cc:dd:ee:ff,192.168.1.2" };

        var rows = Sut.BuildHostRows(status, values, Handler);

        Assert.Single(rows);
        Assert.True(rows[0].IsEditable);
        Assert.Equal(DhcpSourceKind.Managed, rows[0].SourceKind);
    }

    [Fact]
    public void ApplyHostConflictsAndLeases_DetectsDuplicateMac()
    {
        var entry1 = new DhcpHostEntry
        {
            LineNumber = 1,
            MacAddresses = new List<string> { "aa:bb:cc:dd:ee:ff" },
            Address = "192.168.1.2"
        };
        var entry2 = new DhcpHostEntry
        {
            LineNumber = 2,
            MacAddresses = new List<string> { "aa:bb:cc:dd:ee:ff" },
            Address = "192.168.1.3"
        };
        var rows = new List<DhcpHostPageRow>
        {
            new(0, "a", "k0", "k0", DhcpSourceKind.Managed, "/m", false, true, true, entry1, null, null, true),
            new(1, "b", "k1", "k1", DhcpSourceKind.Managed, "/m", false, true, true, entry2, null, null, true)
        };

        DhcpPageProjectionService.ApplyHostConflictsAndLeases(rows, null, null);

        Assert.NotNull(rows[0].Conflict);
        Assert.True(rows[0].Conflict!.DuplicateMac);
        Assert.NotNull(rows[1].Conflict);
        Assert.True(rows[1].Conflict!.DuplicateMac);
    }

    [Fact]
    public void BuildHostGroups_PrependsEmptyManagedGroupWhenOnlyExternalRows()
    {
        var rows = new[]
        {
            SyntheticRow(0, DhcpSourceKind.MainConfig, "/etc/dnsmasq.conf", false, "aa:bb:cc:dd:ee:01")
        };
        var query = new DhcpPageQueryState("", null, DhcpHostSortMode.LineNumber, false);
        var groups = Sut.BuildHostGroups(rows, query, "/var/lib/dnsmasq/managed.conf");

        Assert.True(groups.Count >= 2);
        Assert.Equal(DhcpSourceKind.Managed, groups[0].SourceKind);
        Assert.Empty(groups[0].Rows);
        Assert.True(groups[0].IsSourceEditable);
        Assert.Single(groups[1].Rows);
    }

    [Fact]
    public void BuildHostGroups_OrdersManagedGroupBeforeMainConfig()
    {
        var rows = new[]
        {
            SyntheticRow(1, DhcpSourceKind.MainConfig, "/etc/dnsmasq.conf", false, "aa:bb:cc:dd:ee:02"),
            SyntheticRow(0, DhcpSourceKind.Managed, "/m.conf", true, "aa:bb:cc:dd:ee:01")
        };
        var query = new DhcpPageQueryState("", null, DhcpHostSortMode.LineNumber, false);
        var groups = Sut.BuildHostGroups(rows, query, "/m.conf");

        Assert.Equal(2, groups.Count);
        Assert.Equal(DhcpSourceKind.Managed, groups[0].SourceKind);
        Assert.Equal(DhcpSourceKind.MainConfig, groups[1].SourceKind);
    }

    [Fact]
    public void BuildHostGroups_SearchKeepsOnlyMatchingRows()
    {
        var rows = new[]
        {
            SyntheticRow(0, DhcpSourceKind.Managed, "/m.conf", true, "aa:bb:cc:dd:ee:01", name: "printer-one"),
            SyntheticRow(1, DhcpSourceKind.Managed, "/m.conf", true, "aa:bb:cc:dd:ee:02", name: "router-fw")
        };
        var query = new DhcpPageQueryState("printer", null, DhcpHostSortMode.LineNumber, false);
        var groups = Sut.BuildHostGroups(rows, query, "/m.conf");

        Assert.Single(groups);
        Assert.Single(groups[0].Rows);
        Assert.Contains("printer", groups[0].Rows[0].Entry.Name, StringComparison.Ordinal);
    }

    [Fact]
    public void BuildLeaseRows_FlagsManagedStaticWhenMacMatchesEditableRow()
    {
        var status = MinimalStatus(null, "/m.conf", "/etc/dnsmasq.conf");
        var hostRows = new[]
        {
            SyntheticRow(0, DhcpSourceKind.Managed, "/m.conf", true, "11:22:33:44:55:66")
        };
        var leases = new List<LeaseEntry>
        {
            new()
            {
                Epoch = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                Mac = "11:22:33:44:55:66",
                Address = "192.168.1.50",
                Name = "x",
                ClientId = ""
            }
        };

        var vms = Sut.BuildLeaseRows(status, leases, hostRows);

        Assert.Single(vms);
        Assert.True(vms[0].HasManagedStaticByMac);
        Assert.True(vms[0].HasAnyStaticByMac);
    }

    [Fact]
    public void ApplyHostConflictsAndLeases_DetectsDuplicateAddress()
    {
        var entry1 = new DhcpHostEntry
        {
            LineNumber = 1,
            MacAddresses = new List<string> { "aa:bb:cc:dd:ee:01" },
            Address = "192.168.1.5"
        };
        var entry2 = new DhcpHostEntry
        {
            LineNumber = 2,
            MacAddresses = new List<string> { "aa:bb:cc:dd:ee:02" },
            Address = "192.168.1.5"
        };
        var rows = new List<DhcpHostPageRow>
        {
            new(0, "a", "k0", "k0", DhcpSourceKind.Managed, "/m", false, true, true, entry1, null, null, true),
            new(1, "b", "k1", "k1", DhcpSourceKind.Managed, "/m", false, true, true, entry2, null, null, true)
        };

        DhcpPageProjectionService.ApplyHostConflictsAndLeases(rows, null, null);

        Assert.NotNull(rows[0].Conflict);
        Assert.True(rows[0].Conflict!.DuplicateAddress);
        Assert.True(rows[1].Conflict!.DuplicateAddress);
    }

    private static DnsmasqServiceStatus MinimalStatus(
        DnsmasqWebUI.Models.Dnsmasq.EffectiveConfig.EffectiveConfigSources? sources,
        string managedPath,
        string mainPath) =>
        new(
            SystemHostsPath: null, SystemHostsPathExists: false, ManagedHostsFilePath: null, ManagedHostsPathExists: false,
            NoHosts: false, AddnHostsPaths: [],
            EffectiveConfig: null,
            EffectiveConfigSources: sources,
            MainConfigPath: mainPath, ManagedFilePath: managedPath, LeasesPath: null,
            MainConfigPathExists: true, ManagedFilePathExists: true, LeasesPathConfigured: false, LeasesPathExists: false,
            ConfigFiles: null, ReloadCommandConfigured: false, StatusCommandConfigured: false, StatusShowConfigured: false,
            LogsConfigured: false, LogsPath: null, StatusShowCommand: null, LogsCommand: null,
            DnsmasqStatus: "active", StatusCommandExitCode: null, StatusCommandStdout: null, StatusCommandStderr: null,
            StatusShowOutput: null, LogsOutput: null, DhcpRangeStart: null, DhcpRangeEnd: null,
            DnsmasqVersion: null, MinimumSupportedDnsmasqVersion: "2.90", DnsmasqVersionSupported: true, DnsmasqVersionError: null,
            DnsmasqSupportsDhcp: true, DnsmasqSupportsTftp: true, DnsmasqSupportsDnssec: false, DnsmasqSupportsDbus: false);
}
