using System.Net;
using DnsmasqWebUI.Infrastructure.Services.EffectiveConfig.Metadata;
using DnsmasqWebUI.Infrastructure.Services.EffectiveConfig.Validation.Abstractions;

namespace DnsmasqWebUI.Infrastructure.Services.EffectiveConfig.Validation.Handlers;

/// <summary>
/// Semantic validation for <c>auth-peer</c> values: &lt;ip-address&gt;[,&lt;ip-address&gt;...].
/// One or more IP addresses (secondary servers allowed to initiate AXFR).
/// </summary>
public sealed class AuthPeerSemanticHandler : IOptionSemanticHandler
{
    public bool CanHandle(string optionName) => optionName == DnsmasqConfKeys.AuthPeer;

    public string? ValidateSingle(object? value) => null;

    public string? ValidateMultiItem(string? value)
    {
        var s = (value ?? "").Trim();
        if (s.Length == 0)
            return "auth-peer value cannot be empty.";
        var tokens = s.Split(',', StringSplitOptions.TrimEntries);
        if (tokens.Length == 0 || tokens.Any(t => t.Length == 0))
            return "auth-peer must contain at least one IP address.";
        foreach (var t in tokens)
        {
            if (!IPAddress.TryParse(t, out _))
                return "auth-peer each value must be a valid IP address.";
        }
        return null;
    }
}
