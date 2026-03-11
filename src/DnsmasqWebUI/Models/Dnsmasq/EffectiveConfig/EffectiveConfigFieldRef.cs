namespace DnsmasqWebUI.Models.Dnsmasq.EffectiveConfig;

/// <summary>
/// Typed identity for an effective-config field (section + option).
/// Use for activation, revert, and field issues instead of loose string pairs.
/// </summary>
public readonly record struct EffectiveConfigFieldRef(string SectionId, string OptionName)
{
    /// <summary>Key used by session (ActivateField, SetFieldIssues, etc.).</summary>
    public string FieldKey => $"{SectionId}:{OptionName}";

    public static EffectiveConfigFieldRef For(string sectionId, string optionName)
        => new(sectionId, optionName);
}
