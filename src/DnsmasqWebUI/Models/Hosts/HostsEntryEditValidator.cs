using System.Net;

namespace DnsmasqWebUI.Models.Hosts;

/// <summary>Client-side checks for hosts table inline editing (IP + at least one name).</summary>
public static class HostsEntryEditValidator
{
    public static IReadOnlyList<string> GetAddressFieldErrors(string addressTrimmed)
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(addressTrimmed))
            errors.Add("Address is required.");
        else if (!IPAddress.TryParse(addressTrimmed, out _))
            errors.Add("Address must be a valid IPv4 or IPv6 address.");
        return errors;
    }

    public static IReadOnlyList<string> GetHostnameFieldErrors(IReadOnlyList<string> names)
    {
        if (names == null || names.Count == 0)
            return new[] { "At least one hostname is required." };
        return Array.Empty<string>();
    }

    public static IReadOnlyList<string> GetErrors(string addressTrimmed, IReadOnlyList<string> names)
    {
        var list = new List<string>();
        list.AddRange(GetAddressFieldErrors(addressTrimmed));
        list.AddRange(GetHostnameFieldErrors(names));
        return list;
    }

    public static bool IsValid(string addressTrimmed, IReadOnlyList<string> names) =>
        GetErrors(addressTrimmed, names).Count == 0;
}
