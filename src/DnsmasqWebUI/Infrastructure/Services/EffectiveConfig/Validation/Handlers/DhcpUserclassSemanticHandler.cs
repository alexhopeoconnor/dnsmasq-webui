using DnsmasqWebUI.Infrastructure.Services.EffectiveConfig.Metadata;
using DnsmasqWebUI.Infrastructure.Services.EffectiveConfig.Validation.Abstractions;
using DnsmasqWebUI.Infrastructure.Serialization.Parsers.EffectiveConfig;

namespace DnsmasqWebUI.Infrastructure.Services.EffectiveConfig.Validation.Handlers;

/// <summary>
/// Semantic validation for <c>dhcp-userclass</c> values.
/// </summary>
public sealed class DhcpUserclassSemanticHandler : IOptionSemanticHandler
{
    public bool CanHandle(string optionName) => optionName == DnsmasqConfKeys.DhcpUserclass;

    public string? ValidateSingle(object? value) => null;

    public string? ValidateMultiItem(string? value)
    {
        var s = value?.Trim() ?? "";
        if (s.Length == 0)
            return "Value cannot be empty.";

        var tokens = s.Split(',', StringSplitOptions.TrimEntries);
        if (tokens.Length < 2 || tokens.Any(t => t.Length == 0))
            return "dhcp-userclass must be set:<tag>,<user-class>.";

        if (!DnsmasqDhcpTagSyntax.IsSetToken(tokens[0], prefixOptional: true))
            return "dhcp-userclass must start with a tag (optionally prefixed by set:).";

        return null;
    }
}
