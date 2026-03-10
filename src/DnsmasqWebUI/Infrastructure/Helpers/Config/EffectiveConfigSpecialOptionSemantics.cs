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
            [DnsmasqConfKeys.Local] = new OptionSemantics(
                DnsmasqConfKeys.Local,
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
            [DnsmasqConfKeys.RebindDomainOk] = new OptionSemantics(
                DnsmasqConfKeys.RebindDomainOk,
                EffectiveConfigParserBehavior.Multi,
                EffectiveConfigWriteBehavior.MultiValue,
                ComplexMulti),
            [DnsmasqConfKeys.BogusNxdomain] = new OptionSemantics(
                DnsmasqConfKeys.BogusNxdomain,
                EffectiveConfigParserBehavior.Multi,
                EffectiveConfigWriteBehavior.MultiValue,
                ComplexMulti),
            [DnsmasqConfKeys.IgnoreAddress] = new OptionSemantics(
                DnsmasqConfKeys.IgnoreAddress,
                EffectiveConfigParserBehavior.Multi,
                EffectiveConfigWriteBehavior.MultiValue,
                ComplexMulti),
            [DnsmasqConfKeys.Alias] = new OptionSemantics(
                DnsmasqConfKeys.Alias,
                EffectiveConfigParserBehavior.Multi,
                EffectiveConfigWriteBehavior.MultiValue,
                ComplexMulti),
            [DnsmasqConfKeys.Ipset] = new OptionSemantics(
                DnsmasqConfKeys.Ipset,
                EffectiveConfigParserBehavior.Multi,
                EffectiveConfigWriteBehavior.MultiValue,
                ComplexMulti),
            [DnsmasqConfKeys.Nftset] = new OptionSemantics(
                DnsmasqConfKeys.Nftset,
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
            [DnsmasqConfKeys.DhcpRange] = new OptionSemantics(
                DnsmasqConfKeys.DhcpRange,
                EffectiveConfigParserBehavior.Multi,
                EffectiveConfigWriteBehavior.MultiValue,
                ComplexMulti),
            [DnsmasqConfKeys.DhcpHost] = new OptionSemantics(
                DnsmasqConfKeys.DhcpHost,
                EffectiveConfigParserBehavior.Multi,
                EffectiveConfigWriteBehavior.MultiValue,
                ComplexMulti),
            [DnsmasqConfKeys.DhcpOption] = new OptionSemantics(
                DnsmasqConfKeys.DhcpOption,
                EffectiveConfigParserBehavior.Multi,
                EffectiveConfigWriteBehavior.MultiValue,
                ComplexMulti),
            [DnsmasqConfKeys.DhcpOptionForce] = new OptionSemantics(
                DnsmasqConfKeys.DhcpOptionForce,
                EffectiveConfigParserBehavior.Multi,
                EffectiveConfigWriteBehavior.MultiValue,
                ComplexMulti),
            [DnsmasqConfKeys.DhcpMatch] = new OptionSemantics(
                DnsmasqConfKeys.DhcpMatch,
                EffectiveConfigParserBehavior.Multi,
                EffectiveConfigWriteBehavior.MultiValue,
                ComplexMulti),
            [DnsmasqConfKeys.DhcpMac] = new OptionSemantics(
                DnsmasqConfKeys.DhcpMac,
                EffectiveConfigParserBehavior.Multi,
                EffectiveConfigWriteBehavior.MultiValue,
                ComplexMulti),
            [DnsmasqConfKeys.DhcpIgnoreNames] = new OptionSemantics(
                DnsmasqConfKeys.DhcpIgnoreNames,
                EffectiveConfigParserBehavior.Multi,
                EffectiveConfigWriteBehavior.MultiValue,
                ComplexMulti),
            [DnsmasqConfKeys.DhcpNameMatch] = new OptionSemantics(
                DnsmasqConfKeys.DhcpNameMatch,
                EffectiveConfigParserBehavior.Multi,
                EffectiveConfigWriteBehavior.MultiValue,
                ComplexMulti),
            [DnsmasqConfKeys.DhcpIgnore] = new OptionSemantics(
                DnsmasqConfKeys.DhcpIgnore,
                EffectiveConfigParserBehavior.Multi,
                EffectiveConfigWriteBehavior.MultiValue,
                ComplexMulti),
            [DnsmasqConfKeys.DhcpVendorclass] = new OptionSemantics(
                DnsmasqConfKeys.DhcpVendorclass,
                EffectiveConfigParserBehavior.Multi,
                EffectiveConfigWriteBehavior.MultiValue,
                ComplexMulti),
            [DnsmasqConfKeys.DhcpUserclass] = new OptionSemantics(
                DnsmasqConfKeys.DhcpUserclass,
                EffectiveConfigParserBehavior.Multi,
                EffectiveConfigWriteBehavior.MultiValue,
                ComplexMulti),
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
            [DnsmasqConfKeys.DhcpRelay] = new OptionSemantics(
                DnsmasqConfKeys.DhcpRelay,
                EffectiveConfigParserBehavior.Multi,
                EffectiveConfigWriteBehavior.MultiValue,
                ComplexMulti),
            [DnsmasqConfKeys.DhcpProxy] = new OptionSemantics(
                DnsmasqConfKeys.DhcpProxy,
                EffectiveConfigParserBehavior.Multi,
                EffectiveConfigWriteBehavior.MultiValue,
                ComplexMulti),
            [DnsmasqConfKeys.RaParam] = new OptionSemantics(
                DnsmasqConfKeys.RaParam,
                EffectiveConfigParserBehavior.Multi,
                EffectiveConfigWriteBehavior.MultiValue,
                ComplexMulti),
            [DnsmasqConfKeys.TagIf] = new OptionSemantics(
                DnsmasqConfKeys.TagIf,
                EffectiveConfigParserBehavior.Multi,
                EffectiveConfigWriteBehavior.MultiValue,
                ComplexMulti),
            [DnsmasqConfKeys.BridgeInterface] = new OptionSemantics(
                DnsmasqConfKeys.BridgeInterface,
                EffectiveConfigParserBehavior.Multi,
                EffectiveConfigWriteBehavior.MultiValue,
                ComplexMulti),
            [DnsmasqConfKeys.SharedNetwork] = new OptionSemantics(
                DnsmasqConfKeys.SharedNetwork,
                EffectiveConfigParserBehavior.Multi,
                EffectiveConfigWriteBehavior.MultiValue,
                ComplexMulti),
            [DnsmasqConfKeys.DhcpBoot] = new OptionSemantics(
                DnsmasqConfKeys.DhcpBoot,
                EffectiveConfigParserBehavior.Multi,
                EffectiveConfigWriteBehavior.MultiValue,
                ComplexMulti),
            [DnsmasqConfKeys.DhcpOptionPxe] = new OptionSemantics(
                DnsmasqConfKeys.DhcpOptionPxe,
                EffectiveConfigParserBehavior.Multi,
                EffectiveConfigWriteBehavior.MultiValue,
                ComplexMulti),
            [DnsmasqConfKeys.PxeService] = new OptionSemantics(
                DnsmasqConfKeys.PxeService,
                EffectiveConfigParserBehavior.Multi,
                EffectiveConfigWriteBehavior.MultiValue,
                ComplexMulti),
            [DnsmasqConfKeys.Slaac] = new OptionSemantics(
                DnsmasqConfKeys.Slaac,
                EffectiveConfigParserBehavior.Multi,
                EffectiveConfigWriteBehavior.MultiValue,
                ComplexMulti),
            [DnsmasqConfKeys.TrustAnchor] = new OptionSemantics(
                DnsmasqConfKeys.TrustAnchor,
                EffectiveConfigParserBehavior.Multi,
                EffectiveConfigWriteBehavior.MultiValue,
                ComplexMulti),
            [DnsmasqConfKeys.ConnmarkAllowlist] = new OptionSemantics(
                DnsmasqConfKeys.ConnmarkAllowlist,
                EffectiveConfigParserBehavior.Multi,
                EffectiveConfigWriteBehavior.MultiValue,
                ComplexMulti),
            [DnsmasqConfKeys.DhcpCircuitid] = new OptionSemantics(
                DnsmasqConfKeys.DhcpCircuitid,
                EffectiveConfigParserBehavior.Multi,
                EffectiveConfigWriteBehavior.MultiValue,
                ComplexMulti),
            [DnsmasqConfKeys.DhcpRemoteid] = new OptionSemantics(
                DnsmasqConfKeys.DhcpRemoteid,
                EffectiveConfigParserBehavior.Multi,
                EffectiveConfigWriteBehavior.MultiValue,
                ComplexMulti),
            [DnsmasqConfKeys.DhcpSubscrid] = new OptionSemantics(
                DnsmasqConfKeys.DhcpSubscrid,
                EffectiveConfigParserBehavior.Multi,
                EffectiveConfigWriteBehavior.MultiValue,
                ComplexMulti),
            [DnsmasqConfKeys.FilterRr] = new OptionSemantics(
                DnsmasqConfKeys.FilterRr,
                EffectiveConfigParserBehavior.Multi,
                EffectiveConfigWriteBehavior.MultiValue,
                ComplexMulti),
            [DnsmasqConfKeys.CacheRr] = new OptionSemantics(
                DnsmasqConfKeys.CacheRr,
                EffectiveConfigParserBehavior.Multi,
                EffectiveConfigWriteBehavior.MultiValue,
                ComplexMulti),
            [DnsmasqConfKeys.Interface] = new OptionSemantics(
                DnsmasqConfKeys.Interface,
                EffectiveConfigParserBehavior.Multi,
                EffectiveConfigWriteBehavior.MultiValue,
                ComplexMulti),
            [DnsmasqConfKeys.ExceptInterface] = new OptionSemantics(
                DnsmasqConfKeys.ExceptInterface,
                EffectiveConfigParserBehavior.Multi,
                EffectiveConfigWriteBehavior.MultiValue,
                ComplexMulti),
            [DnsmasqConfKeys.NoDhcpInterface] = new OptionSemantics(
                DnsmasqConfKeys.NoDhcpInterface,
                EffectiveConfigParserBehavior.Multi,
                EffectiveConfigWriteBehavior.MultiValue,
                ComplexMulti),
            [DnsmasqConfKeys.NoDhcpv4Interface] = new OptionSemantics(
                DnsmasqConfKeys.NoDhcpv4Interface,
                EffectiveConfigParserBehavior.Multi,
                EffectiveConfigWriteBehavior.MultiValue,
                ComplexMulti),
            [DnsmasqConfKeys.NoDhcpv6Interface] = new OptionSemantics(
                DnsmasqConfKeys.NoDhcpv6Interface,
                EffectiveConfigParserBehavior.Multi,
                EffectiveConfigWriteBehavior.MultiValue,
                ComplexMulti),
            [DnsmasqConfKeys.AuthServer] = new OptionSemantics(
                DnsmasqConfKeys.AuthServer,
                EffectiveConfigParserBehavior.Multi,
                EffectiveConfigWriteBehavior.MultiValue,
                ComplexMulti),
            [DnsmasqConfKeys.Cname] = new OptionSemantics(
                DnsmasqConfKeys.Cname,
                EffectiveConfigParserBehavior.Multi,
                EffectiveConfigWriteBehavior.MultiValue,
                ComplexMulti),
            [DnsmasqConfKeys.MxHost] = new OptionSemantics(
                DnsmasqConfKeys.MxHost,
                EffectiveConfigParserBehavior.Multi,
                EffectiveConfigWriteBehavior.MultiValue,
                ComplexMulti),
            [DnsmasqConfKeys.PtrRecord] = new OptionSemantics(
                DnsmasqConfKeys.PtrRecord,
                EffectiveConfigParserBehavior.Multi,
                EffectiveConfigWriteBehavior.MultiValue,
                ComplexMulti),
            [DnsmasqConfKeys.InterfaceName] = new OptionSemantics(
                DnsmasqConfKeys.InterfaceName,
                EffectiveConfigParserBehavior.Multi,
                EffectiveConfigWriteBehavior.MultiValue,
                ComplexMulti),
            [DnsmasqConfKeys.CaaRecord] = new OptionSemantics(
                DnsmasqConfKeys.CaaRecord,
                EffectiveConfigParserBehavior.Multi,
                EffectiveConfigWriteBehavior.MultiValue,
                ComplexMulti),
            [DnsmasqConfKeys.Srv] = new OptionSemantics(
                DnsmasqConfKeys.Srv,
                EffectiveConfigParserBehavior.Multi,
                EffectiveConfigWriteBehavior.MultiValue,
                ComplexMulti),
            [DnsmasqConfKeys.NaptrRecord] = new OptionSemantics(
                DnsmasqConfKeys.NaptrRecord,
                EffectiveConfigParserBehavior.Multi,
                EffectiveConfigWriteBehavior.MultiValue,
                ComplexMulti),
            [DnsmasqConfKeys.Domain] = new OptionSemantics(
                DnsmasqConfKeys.Domain,
                EffectiveConfigParserBehavior.Multi,
                EffectiveConfigWriteBehavior.MultiValue,
                ComplexMulti),
            [DnsmasqConfKeys.TxtRecord] = new OptionSemantics(
                DnsmasqConfKeys.TxtRecord,
                EffectiveConfigParserBehavior.Multi,
                EffectiveConfigWriteBehavior.MultiValue,
                ComplexMulti),
            [DnsmasqConfKeys.HostRecord] = new OptionSemantics(
                DnsmasqConfKeys.HostRecord,
                EffectiveConfigParserBehavior.Multi,
                EffectiveConfigWriteBehavior.MultiValue,
                ComplexMulti),
            [DnsmasqConfKeys.DynamicHost] = new OptionSemantics(
                DnsmasqConfKeys.DynamicHost,
                EffectiveConfigParserBehavior.Multi,
                EffectiveConfigWriteBehavior.MultiValue,
                ComplexMulti),
            [DnsmasqConfKeys.DnsRr] = new OptionSemantics(
                DnsmasqConfKeys.DnsRr,
                EffectiveConfigParserBehavior.Multi,
                EffectiveConfigWriteBehavior.MultiValue,
                ComplexMulti),
            [DnsmasqConfKeys.SynthDomain] = new OptionSemantics(
                DnsmasqConfKeys.SynthDomain,
                EffectiveConfigParserBehavior.Multi,
                EffectiveConfigWriteBehavior.MultiValue,
                ComplexMulti),
            [DnsmasqConfKeys.AuthZone] = new OptionSemantics(
                DnsmasqConfKeys.AuthZone,
                EffectiveConfigParserBehavior.Multi,
                EffectiveConfigWriteBehavior.MultiValue,
                ComplexMulti),
            [DnsmasqConfKeys.AuthSoa] = new OptionSemantics(
                DnsmasqConfKeys.AuthSoa,
                EffectiveConfigParserBehavior.Multi,
                EffectiveConfigWriteBehavior.MultiValue,
                ComplexMulti),
            [DnsmasqConfKeys.AuthSecServers] = new OptionSemantics(
                DnsmasqConfKeys.AuthSecServers,
                EffectiveConfigParserBehavior.Multi,
                EffectiveConfigWriteBehavior.MultiValue,
                ComplexMulti),
            [DnsmasqConfKeys.AuthPeer] = new OptionSemantics(
                DnsmasqConfKeys.AuthPeer,
                EffectiveConfigParserBehavior.Multi,
                EffectiveConfigWriteBehavior.MultiValue,
                ComplexMulti),
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
