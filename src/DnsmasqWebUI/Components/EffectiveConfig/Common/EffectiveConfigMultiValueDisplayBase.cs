using Microsoft.AspNetCore.Components;
using DnsmasqWebUI.Models.Dnsmasq.EffectiveConfig;

namespace DnsmasqWebUI.Components.EffectiveConfig;

/// <summary>
/// Base for custom effective-config multi-value displays; mirrors <see cref="EffectiveConfigValueDisplayBase"/> for singles.
/// </summary>
public abstract class EffectiveConfigMultiValueDisplayBase : ComponentBase
{
    [Parameter]
    public EffectiveConfigMultiDisplayContext Context { get; set; } = null!;

    [Parameter]
    public EventCallback<IReadOnlyList<string>> OnValuesChanged { get; set; }

    protected IReadOnlyList<ValueWithSource> EffectiveItems => Context.EffectiveItems;

    protected IReadOnlyList<string> EffectiveValues => Context.EffectiveValues;

    protected Task NotifyValuesChanged(IReadOnlyList<string> values) => OnValuesChanged.InvokeAsync(values);
}
