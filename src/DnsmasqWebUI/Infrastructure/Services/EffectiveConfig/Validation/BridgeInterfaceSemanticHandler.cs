using DnsmasqWebUI.Infrastructure.Helpers.Config;

namespace DnsmasqWebUI.Infrastructure.Services.EffectiveConfig.Validation;

/// <summary>
/// Semantic validation for <c>bridge-interface</c> values.
/// </summary>
public sealed class BridgeInterfaceSemanticHandler : IOptionSemanticHandler
{
    public bool CanHandle(string optionName) => optionName == DnsmasqConfKeys.BridgeInterface;

    public string? ValidateSingle(object? value) => null;

    public string? ValidateMultiItem(string? value)
    {
        var s = value?.Trim() ?? "";
        if (s.Length == 0)
            return "Value cannot be empty.";

        var tokens = s.Split(',', StringSplitOptions.TrimEntries);
        if (tokens.Length < 2 || tokens.Any(t => t.Length == 0))
            return "bridge-interface must be interface,alias[,alias].";

        if (!DnsmasqDhcpTagSyntax.IsInterfaceLike(tokens[0]))
            return "bridge-interface must start with a valid interface name.";

        for (var i = 1; i < tokens.Length; i++)
        {
            if (!DnsmasqDhcpTagSyntax.IsInterfaceLike(tokens[i], allowWildcard: true))
                return $"Invalid bridge-interface alias '{tokens[i]}'.";
        }

        return null;
    }
}
