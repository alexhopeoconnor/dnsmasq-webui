using System.Collections.Generic;
using System.Linq;

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
    OptionValidationSemantics Validation
);

/// <summary>
/// Lookup for special-option semantics. Used by EffectiveConfigWriteSemantics and registry for validators.
/// Inverse-pair options (e.g. Do0x20Encode) have their (enabled key, disabled key) in a separate table.
/// </summary>
public static class EffectiveConfigSpecialOptionSemantics
{
    private static readonly OptionValidationSemantics KeyOnlyOrValue = new(OptionValidationKind.KeyOnlyOrValue, allowEmpty: true);
    private static readonly OptionValidationSemantics InversePair = new(OptionValidationKind.InversePair);
    private static readonly OptionValidationSemantics ComplexMulti = new(OptionValidationKind.Complex, allowEmpty: true);
    private static readonly OptionValidationSemantics IpAddressMulti = new(
        OptionValidationKind.IpAddress,
        allowEmpty: false);
    private static readonly OptionValidationSemantics PathFileMulti = new(
        OptionValidationKind.PathFile,
        allowEmpty: true,
        pathPolicy: PathExistencePolicy.MustExist);
    private static readonly OptionValidationSemantics PathDirectoryMulti = new(
        OptionValidationKind.PathDirectory,
        allowEmpty: true,
        pathPolicy: PathExistencePolicy.MustExist);
    private static readonly OptionValidationSemantics PathFileSingleMustExist = new(
        OptionValidationKind.PathFile,
        allowEmpty: true,
        pathPolicy: PathExistencePolicy.MustExist);
    private static readonly OptionValidationSemantics PathDirectorySingleMustExist = new(
        OptionValidationKind.PathDirectory,
        allowEmpty: true,
        pathPolicy: PathExistencePolicy.MustExist);
    private static readonly OptionValidationSemantics PathFileSingleParentMustExist = new(
        OptionValidationKind.PathFile,
        allowEmpty: true,
        pathPolicy: PathExistencePolicy.ParentMustExist);

    private static readonly IReadOnlyDictionary<string, OptionSemantics> ByOptionName =
        new Dictionary<string, OptionSemantics>(StringComparer.Ordinal)
        {
            [DnsmasqConfKeys.UseStaleCache] = new OptionSemantics(
                DnsmasqConfKeys.UseStaleCache,
                EffectiveConfigParserBehavior.LastWins,
                EffectiveConfigWriteBehavior.KeyOnlyOrValue,
                KeyOnlyOrValue),
            [DnsmasqConfKeys.AddMac] = new OptionSemantics(
                DnsmasqConfKeys.AddMac,
                EffectiveConfigParserBehavior.LastWins,
                EffectiveConfigWriteBehavior.KeyOnlyOrValue,
                KeyOnlyOrValue),
            [DnsmasqConfKeys.AddSubnet] = new OptionSemantics(
                DnsmasqConfKeys.AddSubnet,
                EffectiveConfigParserBehavior.LastWins,
                EffectiveConfigWriteBehavior.KeyOnlyOrValue,
                KeyOnlyOrValue),
            [DnsmasqConfKeys.Umbrella] = new OptionSemantics(
                DnsmasqConfKeys.Umbrella,
                EffectiveConfigParserBehavior.LastWins,
                EffectiveConfigWriteBehavior.KeyOnlyOrValue,
                KeyOnlyOrValue),
            [DnsmasqConfKeys.Do0x20Encode] = new OptionSemantics(
                DnsmasqConfKeys.Do0x20Encode,
                EffectiveConfigParserBehavior.Flag,
                EffectiveConfigWriteBehavior.InversePair,
                InversePair),
            [DnsmasqConfKeys.ConnmarkAllowlistEnable] = new OptionSemantics(
                DnsmasqConfKeys.ConnmarkAllowlistEnable,
                EffectiveConfigParserBehavior.LastWins,
                EffectiveConfigWriteBehavior.KeyOnlyOrValue,
                KeyOnlyOrValue),
            [DnsmasqConfKeys.DnssecCheckUnsigned] = new OptionSemantics(
                DnsmasqConfKeys.DnssecCheckUnsigned,
                EffectiveConfigParserBehavior.LastWins,
                EffectiveConfigWriteBehavior.KeyOnlyOrValue,
                KeyOnlyOrValue),
            [DnsmasqConfKeys.Leasequery] = new OptionSemantics(
                DnsmasqConfKeys.Leasequery,
                EffectiveConfigParserBehavior.Multi,
                EffectiveConfigWriteBehavior.MultiKeyOnlyOrValue,
                ComplexMulti),
            [DnsmasqConfKeys.Server] = new OptionSemantics(
                DnsmasqConfKeys.Server,
                EffectiveConfigParserBehavior.Multi,
                EffectiveConfigWriteBehavior.MultiValue,
                ComplexMulti),
            [DnsmasqConfKeys.RevServer] = new OptionSemantics(
                DnsmasqConfKeys.RevServer,
                EffectiveConfigParserBehavior.Multi,
                EffectiveConfigWriteBehavior.MultiValue,
                ComplexMulti),
            [DnsmasqConfKeys.Address] = new OptionSemantics(
                DnsmasqConfKeys.Address,
                EffectiveConfigParserBehavior.Multi,
                EffectiveConfigWriteBehavior.MultiValue,
                ComplexMulti),
            [DnsmasqConfKeys.ListenAddress] = new OptionSemantics(
                DnsmasqConfKeys.ListenAddress,
                EffectiveConfigParserBehavior.Multi,
                EffectiveConfigWriteBehavior.MultiValue,
                IpAddressMulti),
            [DnsmasqConfKeys.DhcpGenerateNames] = new OptionSemantics(
                DnsmasqConfKeys.DhcpGenerateNames,
                EffectiveConfigParserBehavior.LastWins,
                EffectiveConfigWriteBehavior.KeyOnlyOrValue,
                KeyOnlyOrValue),
            [DnsmasqConfKeys.DhcpBroadcast] = new OptionSemantics(
                DnsmasqConfKeys.DhcpBroadcast,
                EffectiveConfigParserBehavior.LastWins,
                EffectiveConfigWriteBehavior.KeyOnlyOrValue,
                KeyOnlyOrValue),
            [DnsmasqConfKeys.BootpDynamic] = new OptionSemantics(
                DnsmasqConfKeys.BootpDynamic,
                EffectiveConfigParserBehavior.LastWins,
                EffectiveConfigWriteBehavior.KeyOnlyOrValue,
                KeyOnlyOrValue),
            [DnsmasqConfKeys.AddnHosts] = new OptionSemantics(
                DnsmasqConfKeys.AddnHosts,
                EffectiveConfigParserBehavior.Multi,
                EffectiveConfigWriteBehavior.MultiValue,
                PathFileMulti),
            [DnsmasqConfKeys.ResolvFile] = new OptionSemantics(
                DnsmasqConfKeys.ResolvFile,
                EffectiveConfigParserBehavior.Multi,
                EffectiveConfigWriteBehavior.MultiValue,
                PathFileMulti),
            [DnsmasqConfKeys.Hostsdir] = new OptionSemantics(
                DnsmasqConfKeys.Hostsdir,
                EffectiveConfigParserBehavior.LastWins,
                EffectiveConfigWriteBehavior.SingleValue,
                PathDirectorySingleMustExist),
            [DnsmasqConfKeys.DhcpLeasefile] = new OptionSemantics(
                DnsmasqConfKeys.DhcpLeasefile,
                EffectiveConfigParserBehavior.LastWins,
                EffectiveConfigWriteBehavior.SingleValue,
                PathFileSingleParentMustExist),
            [DnsmasqConfKeys.TftpRoot] = new OptionSemantics(
                DnsmasqConfKeys.TftpRoot,
                EffectiveConfigParserBehavior.LastWins,
                EffectiveConfigWriteBehavior.SingleValue,
                PathDirectorySingleMustExist),
            [DnsmasqConfKeys.PidFile] = new OptionSemantics(
                DnsmasqConfKeys.PidFile,
                EffectiveConfigParserBehavior.LastWins,
                EffectiveConfigWriteBehavior.SingleValue,
                PathFileSingleParentMustExist),
            [DnsmasqConfKeys.Dumpfile] = new OptionSemantics(
                DnsmasqConfKeys.Dumpfile,
                EffectiveConfigParserBehavior.LastWins,
                EffectiveConfigWriteBehavior.SingleValue,
                PathFileSingleParentMustExist),
            [DnsmasqConfKeys.DnssecTimestamp] = new OptionSemantics(
                DnsmasqConfKeys.DnssecTimestamp,
                EffectiveConfigParserBehavior.LastWins,
                EffectiveConfigWriteBehavior.SingleValue,
                PathFileSingleParentMustExist),
            [DnsmasqConfKeys.DhcpScript] = new OptionSemantics(
                DnsmasqConfKeys.DhcpScript,
                EffectiveConfigParserBehavior.LastWins,
                EffectiveConfigWriteBehavior.SingleValue,
                PathFileSingleMustExist),
            [DnsmasqConfKeys.DhcpLuascript] = new OptionSemantics(
                DnsmasqConfKeys.DhcpLuascript,
                EffectiveConfigParserBehavior.LastWins,
                EffectiveConfigWriteBehavior.SingleValue,
                PathFileSingleMustExist),
            [DnsmasqConfKeys.DhcpHostsfile] = new OptionSemantics(
                DnsmasqConfKeys.DhcpHostsfile,
                EffectiveConfigParserBehavior.Multi,
                EffectiveConfigWriteBehavior.MultiValue,
                PathFileMulti),
            [DnsmasqConfKeys.DhcpOptsfile] = new OptionSemantics(
                DnsmasqConfKeys.DhcpOptsfile,
                EffectiveConfigParserBehavior.Multi,
                EffectiveConfigWriteBehavior.MultiValue,
                PathFileMulti),
            [DnsmasqConfKeys.DhcpHostsdir] = new OptionSemantics(
                DnsmasqConfKeys.DhcpHostsdir,
                EffectiveConfigParserBehavior.Multi,
                EffectiveConfigWriteBehavior.MultiValue,
                PathDirectoryMulti),
            [DnsmasqConfKeys.DhcpOptsdir] = new OptionSemantics(
                DnsmasqConfKeys.DhcpOptsdir,
                EffectiveConfigParserBehavior.Multi,
                EffectiveConfigWriteBehavior.MultiValue,
                PathDirectoryMulti),
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

    /// <summary>Returns all option names that have special semantics. Used by wiring tests to avoid hardcoded lists.</summary>
    public static IReadOnlyCollection<string> GetAllOptionNames() => ByOptionName.Keys.ToList();
}
