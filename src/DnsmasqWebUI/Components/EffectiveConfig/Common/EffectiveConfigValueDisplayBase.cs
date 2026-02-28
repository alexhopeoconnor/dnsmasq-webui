using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using DnsmasqWebUI.Models.Dnsmasq.EffectiveConfig;
using DnsmasqWebUI.Infrastructure.Services.EffectiveConfig.Abstractions;

namespace DnsmasqWebUI.Components.EffectiveConfig;

/// <summary>
/// Base for effective-config custom value displays. Provides shared parameters,
/// <see cref="NotifyValueChanged"/>, and <see cref="GetEffectiveDisplayValue"/> so pending changes are shown before save.
/// </summary>
public abstract class EffectiveConfigValueDisplayBase : ComponentBase
{
    [Parameter]
    public EffectiveConfigFieldDescriptor Descriptor { get; set; } = null!;

    [CascadingParameter]
    public bool IsActiveEditor { get; set; }

    [CascadingParameter]
    public EventCallback<FocusEventArgs> OnCommitRequested { get; set; }

    [Parameter]
    public EventCallback<object?> ValueChanged { get; set; }

    [CascadingParameter]
    public IEffectiveConfigEditSession? Session { get; set; }

    protected Task NotifyValueChanged(object? value) => ValueChanged.InvokeAsync(value);

    /// <summary>Returns the value to display: pending change NewValue if this field has one, else descriptor value (not yet written to disk).</summary>
    protected object? GetEffectiveDisplayValue()
    {
        var pending = Session?.PendingChanges.FirstOrDefault(c =>
            string.Equals(c.SectionId, Descriptor.SectionId, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(c.OptionName, Descriptor.OptionName, StringComparison.OrdinalIgnoreCase));
        return pending != null ? pending.NewValue : Descriptor.GetValue();
    }
}
