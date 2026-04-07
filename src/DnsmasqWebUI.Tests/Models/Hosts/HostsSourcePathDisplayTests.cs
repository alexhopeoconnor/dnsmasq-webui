using DnsmasqWebUI.Models.Hosts;

namespace DnsmasqWebUI.Tests.Models.Hosts;

public sealed class HostsSourcePathDisplayTests
{
    private static HostsPageRow Row(HostsRowSourceKind kind, string path, bool isComment = false) =>
        new(
            Id: "id",
            SourceKind: kind,
            SourcePath: path,
            IsEditable: false,
            IsActive: true,
            InactiveReason: null,
            Address: "127.0.0.1",
            Names: [],
            EffectiveNames: [],
            IsComment: isComment,
            LineNumber: 1);

    [Fact]
    public void BuildFileFilterMenuGroups_InjectsManagedPath_WhenNotInRows()
    {
        var managed = "/data/managed.hosts";
        var groups = HostsSourcePathDisplay.BuildFileFilterMenuGroups([], managed);

        Assert.Single(groups);
        Assert.Equal(HostsRowSourceKind.Managed, groups[0].SourceKind);
        Assert.Single(groups[0].Files);
        Assert.Equal(managed, groups[0].Files[0].Path);
        Assert.Equal(0, groups[0].Files[0].RecordCount);
    }

    [Fact]
    public void BuildFileFilterMenuGroups_OrdersGroupsManagedSystemAddn_AndCountsNonComments()
    {
        var rows = new[]
        {
            Row(HostsRowSourceKind.SystemHosts, "/etc/hosts"),
            Row(HostsRowSourceKind.Managed, "/m/a.hosts"),
            Row(HostsRowSourceKind.Managed, "/m/a.hosts", isComment: true),
            Row(HostsRowSourceKind.AddnHosts, "/extra/b.hosts"),
        };

        var groups = HostsSourcePathDisplay.BuildFileFilterMenuGroups(rows, managedHostsPathAlwaysShow: null);

        Assert.Equal(3, groups.Count);
        Assert.Equal(HostsRowSourceKind.Managed, groups[0].SourceKind);
        Assert.Equal(HostsRowSourceKind.SystemHosts, groups[1].SourceKind);
        Assert.Equal(HostsRowSourceKind.AddnHosts, groups[2].SourceKind);

        Assert.Single(groups[0].Files);
        Assert.Equal(1, groups[0].Files[0].RecordCount);
    }
}
