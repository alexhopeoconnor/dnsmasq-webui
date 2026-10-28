using DnsmasqWebUI.Models.Dnsmasq.EffectiveConfig;

namespace DnsmasqWebUI.Infrastructure.Services.EffectiveConfig.Metadata;

/// <summary>
/// Describes whether an effective-config field can be changed through the application's managed
/// overlay. A value originating in another config file is not automatically immutable: dnsmasq's
/// last-wins directives can safely be shadowed by the managed file, which is deliberately included
/// last by the application. Flags and multi-value directives are deliberately excluded because an
/// overlay cannot safely remove or replace the externally supplied behaviour.
/// </summary>
public static class EffectiveConfigManagedOverridePolicy
{
    /// <summary>
    /// Gets the editing state for a field and its current source.
    /// </summary>
    public static EffectiveConfigFieldEditability GetEditability(
        string optionName,
        bool isMultiValue,
        ConfigValueSource? source)
    {
        if (source is not { IsReadOnly: true })
            return EffectiveConfigFieldEditability.Editable;

        if (isMultiValue ||
            EffectiveConfigParserBehaviorMap.GetBehavior(optionName) != EffectiveConfigParserBehavior.LastWins)
        {
            return EffectiveConfigFieldEditability.ReadOnly;
        }

        return EffectiveConfigWriteSemantics.GetBehavior(optionName) is
            EffectiveConfigWriteBehavior.SingleValue or EffectiveConfigWriteBehavior.KeyOnlyOrValue
            ? EffectiveConfigFieldEditability.ManagedOverrideAvailable
            : EffectiveConfigFieldEditability.ReadOnly;
    }

    /// <summary>
    /// True when an externally sourced value can safely be shadowed by a line in the managed
    /// config. This never permits changing an external source file itself.
    /// </summary>
    public static bool CanCreateManagedOverride(
        string optionName,
        bool isMultiValue,
        ConfigValueSource? source) =>
        GetEditability(optionName, isMultiValue, source) == EffectiveConfigFieldEditability.ManagedOverrideAvailable;

    /// <summary>
    /// True when the field can be edited from the UI, either because it is already app-managed or
    /// because a safe managed override can be created.
    /// </summary>
    public static bool CanEditFromUi(
        string optionName,
        bool isMultiValue,
        ConfigValueSource? source) =>
        GetEditability(optionName, isMultiValue, source) != EffectiveConfigFieldEditability.ReadOnly;

    /// <summary>
    /// Returns source-aware wording for the managed-override action. The wording intentionally
    /// makes clear that the original source remains untouched.
    /// </summary>
    public static string? GetSourceStateTooltip(
        string optionName,
        bool isMultiValue,
        ConfigValueSource? source)
    {
        if (source is not { IsReadOnly: true })
            return source?.GetReadOnlyTooltip();

        if (!CanCreateManagedOverride(optionName, isMultiValue, source))
            return source.GetReadOnlyTooltip();

        var location = source.LineNumber.HasValue
            ? $"{source.FileName} line {source.LineNumber}"
            : source.FileName;
        return $"From {location}. Create a managed override to take precedence; {source.FilePath} will not be changed.";
    }
}

/// <summary>
/// The source-aware editing states supported by the Effective Config surface.
/// </summary>
public enum EffectiveConfigFieldEditability
{
    /// <summary>The value is absent or originates in the managed overlay.</summary>
    Editable,

    /// <summary>The value originates externally but can safely be shadowed by the managed overlay.</summary>
    ManagedOverrideAvailable,

    /// <summary>The external value cannot safely be changed through a managed overlay.</summary>
    ReadOnly,
}
