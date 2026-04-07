using DnsmasqWebUI.Infrastructure.Services.Dnsmasq.Hosts;
using DnsmasqWebUI.Models.Hosts;

namespace DnsmasqWebUI.Tests.Services.Hosts;

public class HostsPageProjectionServiceTests
{
    private readonly HostsPageProjectionService _sut = new();

    [Fact]
    public void BuildGroups_SourcePathFilter_KeepsOnlyThatFile()
    {
        var rows = new HostsPageRow[]
        {
            new(
                "m1",
                HostsRowSourceKind.Managed,
                "/data/managed.hosts",
                IsEditable: true,
                IsActive: true,
                InactiveReason: null,
                Address: "10.0.0.1",
                Names: new[] { "a" },
                EffectiveNames: new[] { "a" },
                IsComment: false,
                LineNumber: 1),
            new(
                "x1",
                HostsRowSourceKind.AddnHosts,
                "/etc/extra.hosts",
                IsEditable: false,
                IsActive: true,
                InactiveReason: null,
                Address: "10.0.0.2",
                Names: new[] { "b" },
                EffectiveNames: new[] { "b" },
                IsComment: false,
                LineNumber: 1),
        };

        var query = new HostsPageQueryState(
            Search: "",
            Grouping: HostsGroupingMode.Source,
            Sort: HostsSortMode.LineNumber,
            Descending: false,
            SourceKindFilter: null,
            SourcePathFilter: "/etc/extra.hosts",
            EditableFilter: null,
            ActiveFilter: null);

        var groups = _sut.BuildGroups(rows, query);

        Assert.Single(groups);
        Assert.Equal("/etc/extra.hosts", groups[0].Subtitle);
        Assert.Single(groups[0].Rows);
    }

    [Fact]
    public void BuildGroups_SourcePathFilter_EmptyMeansAllFiles()
    {
        var rows = new HostsPageRow[]
        {
            new("m1", HostsRowSourceKind.Managed, "/a", true, true, null, "1.1.1.1", new[] { "a" }, new[] { "a" }, false, 1),
            new("x1", HostsRowSourceKind.AddnHosts, "/b", false, true, null, "2.2.2.2", new[] { "b" }, new[] { "b" }, false, 1),
        };

        var query = new HostsPageQueryState(
            "",
            HostsGroupingMode.Source,
            HostsSortMode.LineNumber,
            false,
            null,
            SourcePathFilter: null,
            null,
            null);

        var groups = _sut.BuildGroups(rows, query);
        Assert.Equal(2, groups.Count);
    }
}
