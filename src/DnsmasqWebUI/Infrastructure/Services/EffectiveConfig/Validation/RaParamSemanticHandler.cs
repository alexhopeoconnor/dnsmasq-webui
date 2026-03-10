using DnsmasqWebUI.Infrastructure.Helpers.Config;

namespace DnsmasqWebUI.Infrastructure.Services.EffectiveConfig.Validation;

/// <summary>
/// Specialized semantic behavior for <c>ra-param</c> values.
/// Uses conservative validation for interface, mtu/priority, interval, and optional lifetime fields.
/// </summary>
public sealed class RaParamSemanticHandler : IOptionSemanticHandler
{
    public bool CanHandle(string optionName) =>
        optionName == DnsmasqConfKeys.RaParam;

    public string? ValidateSingle(object? value) => null;

    public string? ValidateMultiItem(string? value)
    {
        var s = value?.Trim() ?? "";
        if (s.Length == 0)
            return "Value cannot be empty.";

        var tokens = s.Split(',').Select(t => t.Trim()).ToArray();
        if (tokens.Length == 0 || tokens.Any(t => t.Length == 0))
            return "ra-param contains an empty comma-separated segment.";

        if (!IsInterfaceName(tokens[0]))
            return "ra-param must start with an interface name.";

        var seenPriority = false;
        var seenMtu = false;
        var numericCount = 0;
        for (var i = 1; i < tokens.Length; i++)
        {
            var token = tokens[i];
            if (token is "high" or "low")
            {
                if (seenPriority)
                    return "ra-param can only include one priority token.";
                seenPriority = true;
                continue;
            }

            if (token.Equals("off", StringComparison.OrdinalIgnoreCase) ||
                token.StartsWith("mtu:", StringComparison.OrdinalIgnoreCase))
            {
                if (seenMtu)
                    return "ra-param can only include one mtu token.";
                seenMtu = true;
                if (token.StartsWith("mtu:", StringComparison.OrdinalIgnoreCase))
                {
                    var mtuValue = token["mtu:".Length..];
                    if (mtuValue.Length == 0)
                        return "ra-param mtu: value cannot be empty.";
                }
                continue;
            }

            if (int.TryParse(token, out _))
            {
                numericCount++;
                if (numericCount > 2)
                    return "ra-param can include at most interval and lifetime numeric values.";
                continue;
            }

            return $"Invalid ra-param segment '{token}'.";
        }

        return null;
    }

    private static bool IsInterfaceName(string value) =>
        value.Length > 0 && value.All(c => char.IsLetterOrDigit(c) || c is '-' or '_' or '.' or '*');
}
