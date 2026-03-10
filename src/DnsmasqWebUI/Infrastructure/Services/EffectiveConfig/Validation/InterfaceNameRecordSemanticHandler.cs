using DnsmasqWebUI.Infrastructure.Helpers.Config;

namespace DnsmasqWebUI.Infrastructure.Services.EffectiveConfig.Validation;

/// <summary>
/// Semantic validation for <c>interface-name</c> (DNS record) values: &lt;name&gt;,&lt;interface&gt;[/4|/6].
/// Name and interface required; interface may have /4 or /6 suffix.
/// </summary>
public sealed class InterfaceNameRecordSemanticHandler : IOptionSemanticHandler
{
    public bool CanHandle(string optionName) => optionName == DnsmasqConfKeys.InterfaceName;

    public string? ValidateSingle(object? value) => null;

    public string? ValidateMultiItem(string? value)
    {
        var s = (value ?? "").Trim();
        if (s.Length == 0)
            return "interface-name value cannot be empty.";
        var tokens = s.Split(',', StringSplitOptions.TrimEntries);
        if (tokens.Length != 2)
            return "interface-name must be <name>,<interface>[/4|/6].";
        if (tokens[0].Length == 0 || tokens[1].Length == 0)
            return "interface-name name and interface cannot be empty.";
        if (!DnsmasqScopedDomainSyntax.IsValidDnsName(tokens[0]))
            return "interface-name name must be a valid DNS name.";
        var iface = tokens[1];
        if (iface.EndsWith("/4", StringComparison.Ordinal) || iface.EndsWith("/6", StringComparison.Ordinal))
            iface = iface[..^2];
        if (!DnsmasqRelaySyntax.IsInterfaceName(iface))
            return "interface-name interface must be a valid interface name (optional /4 or /6).";
        return null;
    }
}
