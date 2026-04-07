using Microsoft.AspNetCore.Components;

namespace DnsmasqWebUI.Models.Ui;

public sealed record GroupedSelectAllOptionContext<TValue>(
    GroupedSelectOption<TValue> Option,
    bool IsSelected,
    EventCallback OnSelect);

public sealed record GroupedSelectOptionSelectContext<TValue>(
    GroupedSelectOption<TValue> Option,
    bool IsSelected,
    EventCallback OnSelect);
