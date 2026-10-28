using System.Reflection;
using DnsmasqWebUI.Components.EffectiveConfig;
using DnsmasqWebUI.Components.EffectiveConfig.CustomDisplays;
using DnsmasqWebUI.Models.Dnsmasq.EffectiveConfig;

namespace DnsmasqWebUI.Tests.Components.EffectiveConfig;

#pragma warning disable BL0005 // Direct component instance is intentional for focused commit-path coverage.
public class ManagedOverrideFieldDisplayTests
{
    [Fact]
    public async Task CommitIfChangedAsync_ClearingExternalOverride_RevertsPendingOverrideInsteadOfPreviewingUnset()
    {
        var source = new ConfigValueSource(
            "/etc/dnsmasq.d/10-base.conf",
            "10-base.conf",
            IsManaged: false,
            LineNumber: 4);
        var component = new EffectiveConfigFieldDisplay
        {
            Descriptor = new EffectiveConfigFieldDescriptor(
                "resolver",
                "port",
                IsMultiValue: false,
                Status: null,
                ResolveValue: _ => 53,
                ResolveSource: _ => source,
                ResolveItems: null),
            FieldKey = "resolver:port"
        };

        var reverted = false;
        var committed = false;
        SetPrivateProperty(component, "Ui", new EffectiveConfigUiContext
        {
            RequestOptionHelpAsync = _ => Task.CompletedTask,
            RequestReadonlyPopoverAsync = _ => Task.CompletedTask,
            ScheduleReadonlyPopoverCloseAsync = () => Task.CompletedTask,
            ActivateFieldAsync = _ => Task.CompletedTask,
            DeactivateFieldAsync = () => Task.CompletedTask,
            CommitFieldAsync = _ =>
            {
                committed = true;
                return Task.CompletedTask;
            },
            RevertFieldAsync = (_, _) =>
            {
                reverted = true;
                return Task.CompletedTask;
            }
        });
        SetPrivateField(component, "_valueAtActivation", 53);
        SetPrivateField(component, "_draftValue", null);
        SetPrivateField(component, "_hasDraftValue", true);
        SetPrivateField(component, "_hasPendingChange", true);

        var result = await InvokeCommitIfChangedAsync(component);

        Assert.True(result);
        Assert.True(reverted);
        Assert.False(committed);
    }

    [Fact]
    public void LogQueriesDisplay_KeyOnlyEffectiveValue_IsShownAndEditedAsOn()
    {
        var component = new LogQueriesDisplay
        {
            Descriptor = new EffectiveConfigFieldDescriptor(
                "resolver",
                "log-queries",
                IsMultiValue: false,
                Status: null,
                ResolveValue: _ => "",
                ResolveSource: _ => null,
                ResolveItems: null)
        };

        Assert.Equal("On", InvokePrivateString(component, "GetViewText"));
        Assert.Equal("on", InvokePrivateString(component, "GetEditValue"));
    }

    private static async Task<bool> InvokeCommitIfChangedAsync(EffectiveConfigFieldDisplay component)
    {
        var method = typeof(EffectiveConfigFieldDisplay).GetMethod(
            "CommitIfChangedAsync",
            BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(method);
        return await Assert.IsType<Task<bool>>(method!.Invoke(component, null));
    }

    private static void SetPrivateField(EffectiveConfigFieldDisplay component, string name, object? value)
    {
        var field = typeof(EffectiveConfigFieldDisplay).GetField(
            name,
            BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(field);
        field!.SetValue(component, value);
    }

    private static void SetPrivateProperty(EffectiveConfigFieldDisplay component, string name, object? value)
    {
        var property = typeof(EffectiveConfigFieldDisplay).GetProperty(
            name,
            BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(property);
        property!.SetValue(component, value);
    }

    private static string InvokePrivateString(LogQueriesDisplay component, string name)
    {
        var method = typeof(LogQueriesDisplay).GetMethod(
            name,
            BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(method);
        return Assert.IsType<string>(method!.Invoke(component, null));
    }
}
#pragma warning restore BL0005
