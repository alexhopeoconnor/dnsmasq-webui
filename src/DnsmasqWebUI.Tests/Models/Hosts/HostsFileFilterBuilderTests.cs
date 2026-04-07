using DnsmasqWebUI.Models.Hosts;
using DnsmasqWebUI.Models.Ui;

namespace DnsmasqWebUI.Tests.Models.Hosts;

public sealed class HostsFileFilterBuilderTests
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
    public void Build_InjectsManagedPath_WhenNotInRows()
    {
        var managed = "/data/managed.hosts";
        var builder = new HostsFileFilterBuilder([], managed);
        var model = builder.Build();

        Assert.NotNull(model.AllOption);
        var managedSection = model.Sections.Single();
        Assert.Equal("Managed hosts", managedSection.Label);
        Assert.Equal("managed", managedSection.Kind);
        Assert.Single(managedSection.Options);
        Assert.Equal(managed, managedSection.Options[0].Value);
        Assert.Equal(0, managedSection.Options[0].Count);
    }

    [Fact]
    public void Build_OrdersGroupsManagedSystemAddn_AndCountsNonComments()
    {
        var rows = new[]
        {
            Row(HostsRowSourceKind.SystemHosts, "/etc/hosts"),
            Row(HostsRowSourceKind.Managed, "/m/a.hosts"),
            Row(HostsRowSourceKind.Managed, "/m/a.hosts", isComment: true),
            Row(HostsRowSourceKind.AddnHosts, "/extra/b.hosts"),
        };

        var builder = new HostsFileFilterBuilder(rows, managedHostsPath: null);
        var model = builder.Build();

        Assert.Equal(3, model.Sections.Count);
        Assert.Equal("managed", model.Sections[0].Kind);
        Assert.Equal("system", model.Sections[1].Kind);
        Assert.Equal("addn", model.Sections[2].Kind);

        Assert.Single(model.Sections[0].Options);
        Assert.Equal(1, model.Sections[0].Options[0].Count);
    }

    [Fact]
    public void GetSummaryText_AllFiles_ShowsRecordCount()
    {
        var rows = new[] { Row(HostsRowSourceKind.Managed, "/m/x.hosts") };
        var builder = new HostsFileFilterBuilder(rows, null);
        Assert.Equal("All files · 1 records", builder.GetSummaryText(""));
        Assert.Equal("All files · 1 records", builder.GetSummaryText("   "));
    }

    [Fact]
    public void GetSummaryText_SelectedFile_UsesShortLabelAndPrefix()
    {
        var rows = new[] { Row(HostsRowSourceKind.Managed, "/m/my.hosts") };
        var builder = new HostsFileFilterBuilder(rows, null);
        Assert.Equal("Managed · my.hosts (1)", builder.GetSummaryText("/m/my.hosts"));
    }

    [Fact]
    public void GetSummaryText_UnknownPath_FallsBackToShortLabel()
    {
        var builder = new HostsFileFilterBuilder([], null);
        Assert.Equal("orphan.txt", builder.GetSummaryText("/tmp/orphan.txt"));
    }

    [Fact]
    public void GetTriggerSummary_SingleSource_SplitsPrefixPrimaryAndCount()
    {
        var rows = new[] { Row(HostsRowSourceKind.AddnHosts, "/etc/extra.d/hosts") };
        var builder = new HostsFileFilterBuilder(rows, null);
        var s = builder.GetTriggerSummary("/etc/extra.d/hosts");

        Assert.Equal(GroupedSelectTriggerSummaryKind.SingleSource, s.Kind);
        Assert.Equal("Additional", s.CategoryPrefix);
        Assert.Equal("hosts", s.PrimaryLabel);
        Assert.Equal("1", s.SecondaryMeta);
        Assert.Equal("Additional · hosts (1)", s.AccessibleFullText);
    }

    [Fact]
    public void GetTriggerSummary_AllSources_WithRecords_HasSecondaryMeta()
    {
        var rows = new[] { Row(HostsRowSourceKind.Managed, "/m/x.hosts") };
        var builder = new HostsFileFilterBuilder(rows, null);
        var s = builder.GetTriggerSummary("");

        Assert.Equal(GroupedSelectTriggerSummaryKind.AllSources, s.Kind);
        Assert.Null(s.CategoryPrefix);
        Assert.Equal("All files", s.PrimaryLabel);
        Assert.Equal("1 records", s.SecondaryMeta);
    }
}
