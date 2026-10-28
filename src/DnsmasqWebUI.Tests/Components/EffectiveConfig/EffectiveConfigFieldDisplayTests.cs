using System.Reflection;
using DnsmasqWebUI.Components.EffectiveConfig;
using DnsmasqWebUI.Models.Dnsmasq.EffectiveConfig;

namespace DnsmasqWebUI.Tests.Components.EffectiveConfig;

public class EffectiveConfigFieldDisplayTests
{
    [Fact]
    public async Task CommitIfChangedAsync_PreservesDuplicateMultiValueOccurrences()
    {
        var component = new EffectiveConfigFieldDisplay();
        SetPrivateProperty(component, "Descriptor", new EffectiveConfigFieldDescriptor(
            "test",
            "repeatable-option",
            IsMultiValue: true,
            Status: null,
            ResolveValue: null,
            ResolveSource: null,
            ResolveItems: _ => []));
        SetPrivateProperty(component, "FieldKey", "test:repeatable-option");
        EffectiveConfigEditCommittedArgs? committed = null;
        SetPrivateProperty(component, "Ui", new EffectiveConfigUiContext
        {
            RequestOptionHelpAsync = _ => Task.CompletedTask,
            RequestReadonlyPopoverAsync = _ => Task.CompletedTask,
            ScheduleReadonlyPopoverCloseAsync = () => Task.CompletedTask,
            ActivateFieldAsync = _ => Task.CompletedTask,
            DeactivateFieldAsync = () => Task.CompletedTask,
            CommitFieldAsync = args =>
            {
                committed = args;
                return Task.CompletedTask;
            }
        });
        SetPrivateField(component, "_valueAtActivation", new List<string> { "baseline" });
        SetPrivateField(component, "_draftValue", new List<string> { "duplicate", "duplicate" });
        SetPrivateField(component, "_hasDraftValue", true);

        var result = await InvokeCommitIfChangedAsync(component);

        Assert.True(result);
        Assert.NotNull(committed);
        Assert.Equal(
            ["duplicate", "duplicate"],
            Assert.IsAssignableFrom<IReadOnlyList<string>>(committed!.NewValue));
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
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        Assert.NotNull(property);
        property!.SetValue(component, value);
    }
}
