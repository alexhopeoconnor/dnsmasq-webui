using System.Net;
using System.Net.Sockets;

namespace DnsmasqWebUI.Infrastructure.Services.EffectiveConfig.Validation;

/// <summary>
/// Validates listen-address option values: IPv4 or IPv6 address only.
/// </summary>
public sealed class ListenAddressMultiValidator : IMultiValueOptionValidator
{
    public string? ValidateItem(string normalized, IReadOnlyList<string> current, int? editIndex = null)
    {
        var v = (normalized ?? "").Trim();
        if (string.IsNullOrEmpty(v))
            return null;
        if (!IPAddress.TryParse(v, out var ip))
            return $"Invalid value for listen-address: '{normalized}'.";
        if (ip.AddressFamily != AddressFamily.InterNetwork &&
            ip.AddressFamily != AddressFamily.InterNetworkV6)
            return $"Invalid value for listen-address: '{normalized}'.";
        return null;
    }
}
