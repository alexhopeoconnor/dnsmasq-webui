namespace DnsmasqWebUI.Models.Ui;

public enum GroupedSelectTriggerSummaryKind
{
    AllSources,
    SingleSource,
    UnknownSource
}

/// <summary>Optional structured lines for the collapsed trigger; use with <see cref="IGroupedSelectTriggerSummary{TValue}"/>.</summary>
public sealed record GroupedSelectTriggerSummary(
    string AccessibleFullText,
    GroupedSelectTriggerSummaryKind Kind,
    string? CategoryPrefix,
    string PrimaryLabel,
    string? SecondaryMeta);

/// <summary>Builders that can supply structured trigger content for richer styling.</summary>
public interface IGroupedSelectTriggerSummary<TValue>
{
    GroupedSelectTriggerSummary GetTriggerSummary(TValue? selectedValue);
}
