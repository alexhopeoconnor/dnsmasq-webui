using System.Text.RegularExpressions;
using DnsmasqWebUI.Infrastructure.Helpers.Config;

namespace DnsmasqWebUI.Infrastructure.Services.EffectiveConfig.Validation;

/// <summary>
/// Specialized semantic behavior for <c>dhcp-mac</c> values.
/// Requires a leading set:&lt;tag&gt; and a MAC pattern with wildcard support.
/// </summary>
public sealed partial class DhcpMacSemanticHandler : IOptionSemanticHandler
{
    [GeneratedRegex(@"^([0-9A-Fa-f*]{1,2}:){5}[0-9A-Fa-f*]{1,2}$", RegexOptions.CultureInvariant)]
    private static partial Regex MacPattern();

    public bool CanHandle(string optionName) =>
        optionName == DnsmasqConfKeys.DhcpMac;

    public string? ValidateSingle(object? value) => null;

    public string? ValidateMultiItem(string? value)
    {
        var s = value?.Trim() ?? "";
        if (s.Length == 0)
            return "Value cannot be empty.";

        var tokens = s.Split(',', StringSplitOptions.TrimEntries);
        if (tokens.Length != 2 || tokens.Any(t => t.Length == 0))
            return "dhcp-mac must be set:<tag>,<MAC pattern>.";

        if (!tokens[0].StartsWith("set:", StringComparison.OrdinalIgnoreCase) || tokens[0].Length <= 4)
            return "dhcp-mac must start with set:<tag>.";

        return MacPattern().IsMatch(tokens[1])
            ? null
            : "dhcp-mac must end with a valid MAC pattern.";
    }
}
