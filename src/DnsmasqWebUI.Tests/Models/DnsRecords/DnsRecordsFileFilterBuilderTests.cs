using DnsmasqWebUI.Models.DnsRecords;

namespace DnsmasqWebUI.Tests.Models.DnsRecords;

public sealed class DnsRecordsFileFilterBuilderTests
{
    private static DnsRecordRow Row(
        string id,
        string sourcePath,
        bool isEditable,
        bool isDraftOnly = false) =>
        new(
            Id: id,
            OccurrenceId: id,
            OptionName: "cname",
            Family: DnsRecordFamily.Cname,
            IndexInOption: 0,
            RawValue: "alias,target",
            Source: null,
            SourcePath: sourcePath,
            SourceLabel: Path.GetFileName(sourcePath),
            IsDraftOnly: isDraftOnly,
            IsEditable: isEditable,
            Payload: new CnamePayload(["alias"], "target", null),
            Issues: [],
            Summary: "alias -> target");

    [Fact]
    public void Build_ClassifiesOnlyManagedConfigPathAsManaged()
    {
        var managed = "/etc/dnsmasq-webui/managed.conf";
        var rows = new[]
        {
            Row("managed", managed, isEditable: true),
            Row("draft-other", "/tmp/custom-writable.conf", isEditable: true, isDraftOnly: true)
        };

        var builder = new DnsRecordsFileFilterBuilder(rows, managed);
        var model = builder.Build();

        Assert.Equal(2, model.Sections.Count);
        Assert.Equal("managed", model.Sections[0].Kind);
        Assert.Equal(managed, model.Sections[0].Options[0].Value);
        Assert.Equal("other", model.Sections[1].Kind);
        Assert.Equal("/tmp/custom-writable.conf", model.Sections[1].Options[0].Value);
    }
}
