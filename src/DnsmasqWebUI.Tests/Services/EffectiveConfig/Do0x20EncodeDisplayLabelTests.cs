using DnsmasqWebUI.Models.Dnsmasq.EffectiveConfig;

namespace DnsmasqWebUI.Tests.Services.EffectiveConfig;

/// <summary>
/// Documents and guards the Do0x20 display label rule: Default and Disabled both show "Disabled" (product choice).
/// Do not "fix" the display to show "Default" for unset without updating this test and the component comment.
/// </summary>
public class Do0x20EncodeDisplayLabelTests
{
    /// <summary>Same logic as Do0x20EncodeDisplay.razor view mode: Enabled → "Enabled", else "Disabled".</summary>
    private static string GetDisplayLabel(ExplicitToggleState state) =>
        state == ExplicitToggleState.Enabled ? "Enabled" : "Disabled";

    [Fact]
    public void Do0x20_Default_ShowsDisabled()
    {
        Assert.Equal("Disabled", GetDisplayLabel(ExplicitToggleState.Default));
    }

    [Fact]
    public void Do0x20_Disabled_ShowsDisabled()
    {
        Assert.Equal("Disabled", GetDisplayLabel(ExplicitToggleState.Disabled));
    }

    [Fact]
    public void Do0x20_Enabled_ShowsEnabled()
    {
        Assert.Equal("Enabled", GetDisplayLabel(ExplicitToggleState.Enabled));
    }
}
