using DnsmasqWebUI.Infrastructure.Helpers.Config;

namespace DnsmasqWebUI.Tests;

/// <summary>
/// Ensures effective-config editable options list includes supported cache flags.
/// </summary>
public class EffectiveConfigEditableOptionsTests
{
    /// <summary>
    /// strip-mac and strip-subnet are supported in the effective-config UI and should be listed in cache options.
    /// </summary>
    [Fact]
    public void EditableOptions_Include_StripMac_And_StripSubnet()
    {
        var allOptionNames = EffectiveConfigSections.GetSectionsInOrder()
            .SelectMany(t => EffectiveConfigSections.GetOptionsInSection(t.SectionId))
            .ToHashSet(StringComparer.Ordinal);
        Assert.Contains(DnsmasqConfKeys.StripMac, allOptionNames);
        Assert.Contains(DnsmasqConfKeys.StripSubnet, allOptionNames);
    }
}
