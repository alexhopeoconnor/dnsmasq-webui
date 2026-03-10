using System.Net;
using DnsmasqWebUI.Infrastructure.Helpers.Config;

namespace DnsmasqWebUI.Infrastructure.Services.EffectiveConfig.Validation;

/// <summary>
/// Specialized semantic behavior for <c>rev-server</c> values.
/// Validates the leading reverse network target and basic presence of an upstream server segment when provided.
/// </summary>
public sealed class RevServerSemanticHandler : IOptionSemanticHandler
{
    public bool CanHandle(string optionName) =>
        optionName == DnsmasqConfKeys.RevServer;

    public string? ValidateSingle(object? value) => null;

    public string? ValidateMultiItem(string? value)
    {
        var s = value?.Trim() ?? "";
        if (s.Length == 0)
            return "Value cannot be empty.";

        var parts = s.Split(',', 2);
        var target = parts[0].Trim();
        var err = ValidateTarget(target);
        if (err is not null)
            return err;

        if (parts.Length == 2 && string.IsNullOrWhiteSpace(parts[1]))
            return "Upstream server cannot be empty when a comma is present.";

        return null;
    }

    private static string? ValidateTarget(string target)
    {
        var slash = target.IndexOf('/');
        var ipText = slash >= 0 ? target[..slash] : target;
        if (!IPAddress.TryParse(ipText, out var ip))
            return "Reverse server prefix must start with a valid IP address.";

        if (slash < 0)
            return null;

        var prefixText = target[(slash + 1)..];
        if (!int.TryParse(prefixText, out var prefix))
            return "Reverse server prefix length must be numeric.";

        var maxPrefix = ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork ? 32 : 128;
        return prefix >= 1 && prefix <= maxPrefix
            ? null
            : $"Reverse server prefix length must be between 1 and {maxPrefix}.";
    }
}
