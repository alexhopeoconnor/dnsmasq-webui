namespace DnsmasqWebUI.Models.Ui;

public sealed record GroupedSelectOption<TValue>(
    TValue Value,
    string Label,
    int? Count = null,
    string? Title = null);

public sealed record GroupedSelectSection<TValue>(
    string Label,
    IReadOnlyList<GroupedSelectOption<TValue>> Options,
    string? Kind = null,
    int Order = 0);

public sealed record GroupedSelectModel<TValue>(
    GroupedSelectOption<TValue>? AllOption,
    IReadOnlyList<GroupedSelectSection<TValue>> Sections);

public interface IGroupedSelectBuilder<TValue>
{
    GroupedSelectModel<TValue> Build();
    string GetSummaryText(TValue? selectedValue);
    string TriggerTitle { get; }
    string TriggerAriaLabel { get; }
    string MenuAriaLabel { get; }
}
