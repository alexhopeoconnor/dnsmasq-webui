using System.Net;
using System.Net.Sockets;
using System.Text.RegularExpressions;

namespace DnsmasqWebUI.Infrastructure.Services.EffectiveConfig.Validation;

/// <summary>
/// Validates server option values: IP address or hostname. Duplicate check is done by the editor when AllowDuplicates is false.
/// </summary>
public sealed class ServerMultiValidator : IMultiValueOptionValidator
{
    private static readonly Regex HostnameRegex = new(
        @"^[a-zA-Z0-9]([a-zA-Z0-9.-]*[a-zA-Z0-9])?$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    public string? ValidateItem(string normalized, IReadOnlyList<string> current, int? editIndex = null)
    {
        if (string.IsNullOrWhiteSpace(normalized))
            return "Server value cannot be empty.";

        if (IPAddress.TryParse(normalized, out var ip))
        {
            if (ip.AddressFamily != AddressFamily.InterNetwork && ip.AddressFamily != AddressFamily.InterNetworkV6)
                return "Invalid IP address.";
            return null;
        }

        if (HostnameRegex.IsMatch(normalized) && normalized.Length <= 253)
            return null;

        return "Enter a valid IP address or hostname.";
    }
}
