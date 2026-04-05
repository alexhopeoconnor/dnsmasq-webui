using DnsmasqWebUI.Infrastructure.Services.EffectiveConfig.Editing;
using DnsmasqWebUI.Infrastructure.Services.EffectiveConfig.Metadata;
using DnsmasqWebUI.Models.Dnsmasq;
using DnsmasqWebUI.Models.Dnsmasq.EffectiveConfig;

namespace DnsmasqWebUI.Tests.Models.EffectiveConfig;

public class EffectiveConfigMultiDisplayContextTests
{
    private static EffectiveMultiValueConfigFieldDescriptor DomainDescriptor(
        IReadOnlyList<ValueWithSource> baseline) =>
        new(
            EffectiveConfigSections.SectionHosts,
            DnsmasqConfKeys.Domain,
            Status: null,
            ResolveItems: _ => baseline,
            Behavior: new DefaultMultiValueEditBehavior(),
            Validator: null);

    private static ConfigValueSource ReadonlySource() =>
        new("/etc/dnsmasq.conf", "dnsmasq.conf", IsManaged: false, LineNumber: 1);

    [Fact]
    public void From_BaselineOnly_UsesBaselineItems()
    {
        var baseline = new List<ValueWithSource>
        {
            new("a.example", null),
            new("b.example", null)
        };
        var d = DomainDescriptor(baseline);

        var ctx = EffectiveConfigMultiDisplayContext.From(
            d, baseline, pendingList: null, hasPendingChange: false, draftList: null,
            isEditMode: false, isActive: false, fieldKey: "k");

        Assert.Same(baseline, ctx.BaselineItems);
        Assert.Equal(["a.example", "b.example"], ctx.EffectiveValues);
        Assert.False(ctx.HasPendingChange);
        Assert.False(ctx.HasDraftChange);
        Assert.False(ctx.HasPendingOrDraft);
    }

    [Fact]
    public void From_PendingOverridesBaseline_PreservesSourcesWhereValuesMatch()
    {
        var ro = ReadonlySource();
        var baseline = new List<ValueWithSource>
        {
            new("a.example", ro),
            new("b.example", null)
        };
        var d = DomainDescriptor(baseline);
        var pending = new List<string> { "a.example", "c.example" };

        var ctx = EffectiveConfigMultiDisplayContext.From(
            d, baseline, pending, hasPendingChange: true, draftList: null,
            isEditMode: false, isActive: false, fieldKey: "k");

        Assert.Equal(["a.example", "c.example"], ctx.EffectiveValues);
        Assert.True(ctx.HasPendingChange);
        Assert.Equal(ro, ctx.EffectiveItems[0].Source);
        Assert.Null(ctx.EffectiveItems[1].Source);
    }

    [Fact]
    public void From_DraftOverridesPending()
    {
        var baseline = new List<ValueWithSource> { new("a.example", null) };
        var d = DomainDescriptor(baseline);
        var pending = new List<string> { "b.example" };
        var draft = new List<string> { "c.example" };

        var ctx = EffectiveContext(d, baseline, pending, draft);

        Assert.Equal(["c.example"], ctx.EffectiveValues);
        Assert.True(ctx.HasDraftChange);
        Assert.True(ctx.HasPendingOrDraft);
    }

    [Fact]
    public void From_AllReadonlyNotEditMode_SetsRowSourceFromFirstItem()
    {
        var ro = ReadonlySource();
        var baseline = new List<ValueWithSource>
        {
            new("a.example", ro)
        };
        var d = DomainDescriptor(baseline);

        var ctx = EffectiveConfigMultiDisplayContext.From(
            d, baseline, null, false, null,
            isEditMode: false, isActive: false, fieldKey: "k");

        Assert.Equal(ro, ctx.RowSource);
    }

    [Fact]
    public void From_EditModeOrMixedWritable_ClearsRowSource()
    {
        var ro = ReadonlySource();
        var baseline = new List<ValueWithSource>
        {
            new("a.example", ro)
        };
        var d = DomainDescriptor(baseline);

        var ctxEdit = EffectiveConfigMultiDisplayContext.From(
            d, baseline, null, false, null,
            isEditMode: true, isActive: false, fieldKey: "k");

        Assert.Null(ctxEdit.RowSource);

        var mixed = new List<ValueWithSource>
        {
            new("a.example", ro),
            new("b.example", null)
        };
        var d2 = DomainDescriptor(mixed);
        var ctxMixed = EffectiveConfigMultiDisplayContext.From(
            d2, mixed, null, false, null,
            isEditMode: false, isActive: false, fieldKey: "k");

        Assert.Null(ctxMixed.RowSource);
    }

    [Fact]
    public void From_IsActiveEditor_RespectsCapabilityDisabled()
    {
        var baseline = new List<ValueWithSource> { new("a.example", null) };
        var d = DomainDescriptor(baseline) with { IsCapabilityDisabled = true };

        var ctx = EffectiveConfigMultiDisplayContext.From(
            d, baseline, null, false, null,
            isEditMode: true, isActive: true, fieldKey: "k");

        Assert.False(ctx.IsActiveEditor);
        Assert.False(ctx.ShowEditableBadge);
    }

    private static EffectiveConfigMultiDisplayContext EffectiveContext(
        EffectiveMultiValueConfigFieldDescriptor d,
        IReadOnlyList<ValueWithSource> baseline,
        IReadOnlyList<string>? pending,
        IReadOnlyList<string>? draft) =>
        EffectiveConfigMultiDisplayContext.From(
            d, baseline, pending, pending != null, draft,
            isEditMode: false, isActive: false, fieldKey: "k");
}
