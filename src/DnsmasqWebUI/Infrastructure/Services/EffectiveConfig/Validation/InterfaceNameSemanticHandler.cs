using DnsmasqWebUI.Infrastructure.Helpers.Config;

namespace DnsmasqWebUI.Infrastructure.Services.EffectiveConfig.Validation;

/// <summary>
/// Semantic validation for interface-name options: <c>interface</c>, <c>except-interface</c>,
/// <c>no-dhcp-interface</c>, <c>no-dhcpv4-interface</c>, <c>no-dhcpv6-interface</c>.
/// Each value is an interface name with optional trailing <c>*</c> wildcard.
/// </summary>
public sealed class InterfaceNameSemanticHandler : IOptionSemanticHandler
{
    private static readonly HashSet<string> HandledOptions = new(StringComparer.Ordinal)
    {
        DnsmasqConfKeys.Interface,
        DnsmasqConfKeys.ExceptInterface,
        DnsmasqConfKeys.NoDhcpInterface,
        DnsmasqConfKeys.NoDhcpv4Interface,
        DnsmasqConfKeys.NoDhcpv6Interface,
    };

    public bool CanHandle(string optionName) => HandledOptions.Contains(optionName);

    public string? ValidateSingle(object? value) => null;

    public string? ValidateMultiItem(string? value)
    {
        var s = (value ?? "").Trim();
        if (s.Length == 0)
            return "Interface name cannot be empty.";
        return DnsmasqRelaySyntax.IsInterfaceNameWithOptionalTrailingWildcard(s)
            ? null
            : "Must be an interface name (e.g. eth0) with optional trailing * wildcard.";
    }
}
