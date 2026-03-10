using System.Collections.Generic;
using DnsmasqWebUI.Infrastructure.Services.EffectiveConfig.Validation;
using DnsmasqWebUI.Models.Dnsmasq.EffectiveConfig;

namespace DnsmasqWebUI.Infrastructure.Helpers.Config;

/// <summary>
/// Single source of truth for options with special parse/write/validation semantics.
/// Prevents drift between parse, write, hint, and validation for UseStaleCache, AddMac, AddSubnet, Umbrella, Do0x20Encode.
/// Inverse-pair key names are not stored here; see <see cref="EffectiveConfigSpecialOptionSemantics.GetInversePairKeys"/>.
/// </summary>
public sealed record OptionSemantics(
    string OptionName,
    EffectiveConfigParserBehavior ParserBehavior,
    EffectiveConfigWriteBehavior WriteBehavior,
    EffectiveConfigSingleValueValidator? SingleValueValidator,
    EffectiveConfigMultiItemValidator? MultiItemValidator
);

/// <summary>
/// Delegate for validating one item in a multi-value option editor.
/// </summary>
public delegate string? EffectiveConfigMultiItemValidator(string? value);

/// <summary>
/// Lookup for special-option semantics. Used by EffectiveConfigWriteSemantics and registry for validators.
/// Inverse-pair options (e.g. Do0x20Encode) have their (enabled key, disabled key) in a separate table.
/// </summary>
public static class EffectiveConfigSpecialOptionSemantics
{
    private static readonly IReadOnlyDictionary<string, OptionSemantics> ByOptionName =
        new Dictionary<string, OptionSemantics>(StringComparer.Ordinal)
        {
            [DnsmasqConfKeys.UseStaleCache] = new OptionSemantics(
                DnsmasqConfKeys.UseStaleCache,
                EffectiveConfigParserBehavior.LastWins,
                EffectiveConfigWriteBehavior.KeyOnlyOrValue,
                SpecialOptionValidators.ValidateUseStaleCache,
                MultiItemValidator: null),
            [DnsmasqConfKeys.AddMac] = new OptionSemantics(
                DnsmasqConfKeys.AddMac,
                EffectiveConfigParserBehavior.LastWins,
                EffectiveConfigWriteBehavior.KeyOnlyOrValue,
                SpecialOptionValidators.ValidateAddMac,
                MultiItemValidator: null),
            [DnsmasqConfKeys.AddSubnet] = new OptionSemantics(
                DnsmasqConfKeys.AddSubnet,
                EffectiveConfigParserBehavior.LastWins,
                EffectiveConfigWriteBehavior.KeyOnlyOrValue,
                SpecialOptionValidators.ValidateAddSubnet,
                MultiItemValidator: null),
            [DnsmasqConfKeys.Umbrella] = new OptionSemantics(
                DnsmasqConfKeys.Umbrella,
                EffectiveConfigParserBehavior.LastWins,
                EffectiveConfigWriteBehavior.KeyOnlyOrValue,
                SpecialOptionValidators.ValidateUmbrella,
                MultiItemValidator: null),
            [DnsmasqConfKeys.Do0x20Encode] = new OptionSemantics(
                DnsmasqConfKeys.Do0x20Encode,
                EffectiveConfigParserBehavior.Flag,
                EffectiveConfigWriteBehavior.InversePair,
                SingleValueValidator: null,
                MultiItemValidator: null),
            [DnsmasqConfKeys.ConnmarkAllowlistEnable] = new OptionSemantics(
                DnsmasqConfKeys.ConnmarkAllowlistEnable,
                EffectiveConfigParserBehavior.LastWins,
                EffectiveConfigWriteBehavior.KeyOnlyOrValue,
                SpecialOptionValidators.ValidateConnmarkAllowlistEnable,
                MultiItemValidator: null),
            [DnsmasqConfKeys.DnssecCheckUnsigned] = new OptionSemantics(
                DnsmasqConfKeys.DnssecCheckUnsigned,
                EffectiveConfigParserBehavior.LastWins,
                EffectiveConfigWriteBehavior.KeyOnlyOrValue,
                SpecialOptionValidators.ValidateDnssecCheckUnsigned,
                MultiItemValidator: null),
            [DnsmasqConfKeys.Leasequery] = new OptionSemantics(
                DnsmasqConfKeys.Leasequery,
                EffectiveConfigParserBehavior.Multi,
                EffectiveConfigWriteBehavior.MultiKeyOnlyOrValue,
                SingleValueValidator: null,
                SpecialOptionValidators.ValidateLeasequeryValue),
            [DnsmasqConfKeys.DhcpGenerateNames] = new OptionSemantics(
                DnsmasqConfKeys.DhcpGenerateNames,
                EffectiveConfigParserBehavior.LastWins,
                EffectiveConfigWriteBehavior.KeyOnlyOrValue,
                SingleValueValidator: null,
                MultiItemValidator: null),
            [DnsmasqConfKeys.DhcpBroadcast] = new OptionSemantics(
                DnsmasqConfKeys.DhcpBroadcast,
                EffectiveConfigParserBehavior.LastWins,
                EffectiveConfigWriteBehavior.KeyOnlyOrValue,
                SingleValueValidator: null,
                MultiItemValidator: null),
            [DnsmasqConfKeys.BootpDynamic] = new OptionSemantics(
                DnsmasqConfKeys.BootpDynamic,
                EffectiveConfigParserBehavior.LastWins,
                EffectiveConfigWriteBehavior.KeyOnlyOrValue,
                SingleValueValidator: null,
                MultiItemValidator: null),
        };

    /// <summary>Keys (enabled, disabled) for InversePair options only. Used by write path and readonly hints.</summary>
    private static readonly IReadOnlyDictionary<string, (string KeyA, string KeyB)> InversePairKeysByOptionName =
        new Dictionary<string, (string, string)>(StringComparer.Ordinal)
        {
            [DnsmasqConfKeys.Do0x20Encode] = (DnsmasqConfKeys.Do0x20Encode, DnsmasqConfKeys.No0x20Encode),
        };

    /// <summary>Returns semantics for a special option; null if not a special option.</summary>
    public static OptionSemantics? TryGetSemantics(string optionName) =>
        ByOptionName.TryGetValue(optionName, out var s) ? s : null;

    /// <summary>Returns write behavior from semantics for special options; otherwise null (caller uses general map).</summary>
    public static EffectiveConfigWriteBehavior? GetWriteBehavior(string optionName) =>
        TryGetSemantics(optionName)?.WriteBehavior;

    /// <summary>Returns the pair of config keys (enabled, disabled) for an InversePair option; null otherwise.</summary>
    public static (string KeyA, string KeyB)? GetInversePairKeys(string optionName) =>
        InversePairKeysByOptionName.TryGetValue(optionName, out var pair) ? pair : null;

    /// <summary>Returns validator from semantics for special options; otherwise null.</summary>
    public static EffectiveConfigSingleValueValidator? GetValidator(string optionName) =>
        TryGetSemantics(optionName)?.SingleValueValidator;

    /// <summary>Returns per-item multi validator from semantics for special options; otherwise null.</summary>
    public static EffectiveConfigMultiItemValidator? GetMultiItemValidator(string optionName) =>
        TryGetSemantics(optionName)?.MultiItemValidator;
}
