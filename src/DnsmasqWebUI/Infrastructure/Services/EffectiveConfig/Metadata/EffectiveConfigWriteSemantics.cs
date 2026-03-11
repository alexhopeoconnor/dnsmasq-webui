namespace DnsmasqWebUI.Infrastructure.Services.EffectiveConfig.Metadata;

/// <summary>
/// How to serialize an option when writing the managed config file.
/// This models write intent, not parse behavior.
/// </summary>
public enum EffectiveConfigWriteBehavior
{
    Flag,
    SingleValue,
    MultiValue,
    KeyOnlyOrValue,
    MultiKeyOnlyOrValue,
    InversePair,
}

/// <summary>
/// Source of truth for write semantics used by managed-config writing and readonly hints.
/// Special options come from <see cref="EffectiveConfigSpecialOptionSemantics"/>; all others
/// derive from parser behavior (Flag/Multi/LastWins).
/// </summary>
public static class EffectiveConfigWriteSemantics
{
    /// <summary>Returns write behavior for an option.</summary>
    public static EffectiveConfigWriteBehavior GetBehavior(string optionName)
    {
        if (EffectiveConfigSpecialOptionSemantics.GetWriteBehavior(optionName) is { } special)
            return special;
        return EffectiveConfigParserBehaviorMap.GetBehavior(optionName) switch
        {
            EffectiveConfigParserBehavior.Flag => EffectiveConfigWriteBehavior.Flag,
            EffectiveConfigParserBehavior.Multi => EffectiveConfigWriteBehavior.MultiValue,
            _ => EffectiveConfigWriteBehavior.SingleValue,
        };
    }

    /// <summary>Returns inverse-pair keys (enabled, disabled) for InversePair options; null otherwise.</summary>
    public static (string KeyA, string KeyB)? GetInversePairKeys(string optionName) =>
        EffectiveConfigSpecialOptionSemantics.GetInversePairKeys(optionName);
}

