using DnsmasqWebUI.Infrastructure.Services.EffectiveConfig.Metadata;
using DnsmasqWebUI.Infrastructure.Services.EffectiveConfig.Validation.Abstractions;
using DnsmasqWebUI.Infrastructure.Serialization.Parsers.EffectiveConfig;

namespace DnsmasqWebUI.Infrastructure.Services.EffectiveConfig.Validation.Handlers;

/// <summary>
/// Conservative semantic validation for <c>dhcp-boot</c> values.
/// Validates optional leading tag and the required filename field.
/// </summary>
public sealed class DhcpBootSemanticHandler : IOptionSemanticHandler
{
    public bool CanHandle(string optionName) => optionName == DnsmasqConfKeys.DhcpBoot;

    public string? ValidateSingle(object? value) => null;

    public string? ValidateMultiItem(string? value)
    {
        var s = value?.Trim() ?? "";
        if (s.Length == 0)
            return "Value cannot be empty.";

        var tokens = s.Split(',').Select(t => t.Trim()).ToArray();

        var index = 0;
        if (tokens[0].StartsWith("tag:", StringComparison.OrdinalIgnoreCase))
        {
            if (!DnsmasqDhcpTagSyntax.IsTagToken(tokens[0]))
                return "dhcp-boot tag: value cannot be empty.";
            index++;
        }

        if (index >= tokens.Length)
            return "dhcp-boot must include a boot filename.";

        if (tokens[index].Length == 0)
            return "dhcp-boot filename cannot be empty.";

        if (tokens.Length > index + 3)
            return "dhcp-boot supports filename[,servername[,server address]].";

        if (tokens.Length == index + 3 && tokens[index + 2].Length == 0)
            return "dhcp-boot server address cannot be empty when the third field is present.";

        return null;
    }
}
