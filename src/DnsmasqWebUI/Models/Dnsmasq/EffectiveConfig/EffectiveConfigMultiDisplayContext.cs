using DnsmasqWebUI.Models.Dnsmasq;

namespace DnsmasqWebUI.Models.Dnsmasq.EffectiveConfig;

/// <summary>
/// Resolved multi-value effective-config state for default and custom multi displays (baseline, pending, draft merge).
/// </summary>
public sealed record EffectiveConfigMultiDisplayContext(
    EffectiveConfigFieldDescriptor Descriptor,
    IReadOnlyList<ValueWithSource> BaselineItems,
    IReadOnlyList<ValueWithSource> EffectiveItems,
    IReadOnlyList<string> EffectiveValues,
    bool HasPendingChange,
    bool HasDraftChange,
    bool HasPendingOrDraft,
    ConfigValueSource? RowSource,
    bool IsEditMode,
    bool IsActiveEditor,
    bool ShowEditableBadge,
    string FieldKey)
{
    /// <summary>
    /// Builds effective multi display state: draft overrides pending overrides baseline items (with source preservation).
    /// </summary>
    public static EffectiveConfigMultiDisplayContext From(
        EffectiveConfigFieldDescriptor descriptor,
        IReadOnlyList<ValueWithSource>? baselineItems,
        IReadOnlyList<string>? pendingList,
        bool hasPendingChange,
        IReadOnlyList<string>? draftList,
        bool isEditMode,
        bool isActive,
        string fieldKey)
    {
        var baseline = baselineItems ?? Array.Empty<ValueWithSource>();
        var baselineValues = baseline.Select(i => i.Value).ToList();
        var hasDraft = draftList != null && !ValuesEqual(baselineValues, draftList);

        IReadOnlyList<ValueWithSource> effectiveItems = hasDraft && draftList != null
            ? BuildItemsWithSource(baseline, draftList)
            : hasPendingChange && pendingList != null
                ? BuildItemsWithSource(baseline, pendingList)
                : baseline;

        var allReadonly = effectiveItems.Count > 0 && effectiveItems.All(i => i.Source?.IsReadOnly == true);
        var rowSource = allReadonly && !isEditMode ? effectiveItems.FirstOrDefault()?.Source : null;

        return new EffectiveConfigMultiDisplayContext(
            descriptor,
            baseline,
            effectiveItems,
            effectiveItems.Select(i => i.Value).ToList(),
            hasPendingChange,
            hasDraft,
            hasPendingChange || hasDraft,
            rowSource,
            isEditMode,
            isEditMode && isActive && !descriptor.IsCapabilityDisabled,
            !descriptor.IsCapabilityDisabled,
            fieldKey);
    }

    private static IReadOnlyList<ValueWithSource> BuildItemsWithSource(
        IReadOnlyList<ValueWithSource> descriptorItems,
        IReadOnlyList<string> values)
    {
        var pool = descriptorItems.ToList();
        var list = new List<ValueWithSource>(values.Count);
        foreach (var s in values)
        {
            var idx = pool.FindIndex(x => string.Equals(x.Value, s, StringComparison.Ordinal));
            if (idx >= 0)
            {
                list.Add(pool[idx]);
                pool.RemoveAt(idx);
            }
            else
                list.Add(new ValueWithSource(s, null));
        }
        return list;
    }

    private static bool ValuesEqual(IReadOnlyList<string> a, IReadOnlyList<string>? b)
    {
        var bb = b ?? Array.Empty<string>();
        return a.SequenceEqual(bb, StringComparer.Ordinal);
    }
}
