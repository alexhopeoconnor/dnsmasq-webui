using DnsmasqWebUI.Components.DnsRecords.Editors;
using DnsmasqWebUI.Infrastructure.Services.EffectiveConfig.Metadata;
using DnsmasqWebUI.Models.DnsRecords;

namespace DnsmasqWebUI.Tests.Components.DnsRecords;

public class DnsRecordEditorSyncGuardTests
{
    [Fact]
    public void AddMode_SameEmptyKey_ReseedsOnce()
    {
        string? k = null;
        Assert.True(DnsRecordEditorSyncGuard.ShouldReseedFromExisting(ref k, null));
        Assert.False(DnsRecordEditorSyncGuard.ShouldReseedFromExisting(ref k, null));
    }

    [Fact]
    public void AddMode_DifferentAddOption_ReseedsAgain()
    {
        string? k = null;
        Assert.True(DnsRecordEditorSyncGuard.ShouldReseedFromExisting(ref k, null, "add:naptr-record"));
        Assert.False(DnsRecordEditorSyncGuard.ShouldReseedFromExisting(ref k, null, "add:naptr-record"));
        Assert.True(DnsRecordEditorSyncGuard.ShouldReseedFromExisting(ref k, null, "add:caa-record"));
    }

    [Fact]
    public void EditMode_UsesRowId()
    {
        var row = new DnsRecordRow(
            "cname:0",
            "cname:0",
            DnsmasqConfKeys.Cname,
            DnsRecordFamily.Cname,
            0,
            "a,b",
            null,
            null,
            null,
            false,
            true,
            new CnamePayload(["a"], "b", null),
            [],
            "s");
        string? k = null;
        Assert.True(DnsRecordEditorSyncGuard.ShouldReseedFromExisting(ref k, row));
        Assert.False(DnsRecordEditorSyncGuard.ShouldReseedFromExisting(ref k, row));
    }

    [Fact]
    public void TransitionFromAddToEdit_Reseeds()
    {
        string? k = null;
        DnsRecordEditorSyncGuard.ShouldReseedFromExisting(ref k, null);
        var row = new DnsRecordRow(
            "txt:1",
            "txt:1",
            DnsmasqConfKeys.TxtRecord,
            DnsRecordFamily.Txt,
            1,
            "x,y",
            null,
            null,
            null,
            false,
            true,
            new TxtPayload("x", "y"),
            [],
            "s");
        Assert.True(DnsRecordEditorSyncGuard.ShouldReseedFromExisting(ref k, row));
    }
}
