using DnsmasqWebUI.Infrastructure.Helpers.Config;

namespace DnsmasqWebUI.Infrastructure.Services.EffectiveConfig.Validation;

/// <summary>
/// Conservative semantic validation for <c>shared-network</c> values.
/// </summary>
public sealed class SharedNetworkSemanticHandler : IOptionSemanticHandler
{
    public bool CanHandle(string optionName) => optionName == DnsmasqConfKeys.SharedNetwork;

    public string? ValidateSingle(object? value) => null;

    public string? ValidateMultiItem(string? value)
    {
        var s = value?.Trim() ?? "";
        if (s.Length == 0)
            return "Value cannot be empty.";

        var tokens = s.Split(',', StringSplitOptions.TrimEntries);
        if (tokens.Length < 2 || tokens.Any(t => t.Length == 0))
            return "shared-network must include an interface/name and at least one additional value.";

        return DnsmasqDhcpTagSyntax.IsInterfaceLike(tokens[0])
            ? null
            : "shared-network must start with an interface or shared-network name.";
    }
}
