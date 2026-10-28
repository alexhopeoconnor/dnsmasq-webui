using System.Net;

namespace DnsmasqWebUI.Models.Hosts;

/// <summary>
/// Validation shared by the Hosts editor and the server-side managed-hosts write boundary.
/// It intentionally does not impose a narrow DNS-label policy: dnsmasq accepts a wider set of local names.
/// It does reject characters which would change the tokenisation or comment semantics of a hosts line.
/// </summary>
public static class HostsEntryEditValidator
{
    public static IReadOnlyList<string> GetAddressFieldErrors(string addressTrimmed)
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(addressTrimmed))
            errors.Add("Address is required.");
        else if (addressTrimmed.Any(char.IsWhiteSpace) || addressTrimmed.Any(char.IsControl))
            errors.Add("Address must not contain whitespace or control characters.");
        else if (!IPAddress.TryParse(addressTrimmed, out _))
            errors.Add("Address must be a valid IPv4 or IPv6 address.");
        return errors;
    }

    public static IReadOnlyList<string> GetHostnameFieldErrors(IReadOnlyList<string> names)
    {
        if (names == null || names.Count == 0)
            return new[] { "At least one hostname is required." };

        var errors = new List<string>();
        for (var i = 0; i < names.Count; i++)
        {
            var name = names[i];
            var label = $"Hostname {i + 1}";
            if (string.IsNullOrWhiteSpace(name))
            {
                errors.Add($"{label} is required.");
                continue;
            }
            if (name.Any(char.IsWhiteSpace))
                errors.Add($"{label} must not contain whitespace.");
            if (name.Contains('#'))
                errors.Add($"{label} must not contain # because it starts an inline comment.");
            if (name.Any(char.IsControl))
                errors.Add($"{label} must not contain control characters.");
        }

        return errors;
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

    /// <summary>
    /// Validates a complete managed-hosts file before it reaches the serializer. Existing passthrough
    /// lines are intentionally preserved, but a caller cannot use a line break in RawLine to inject
    /// additional records.
    /// </summary>
    public static IReadOnlyList<string> GetEntryErrors(IReadOnlyList<HostEntry> entries)
    {
        if (entries == null)
            return new[] { "Hosts entries are required." };

        var errors = new List<string>();
        for (var i = 0; i < entries.Count; i++)
        {
            var entry = entries[i];
            var prefix = $"Hosts entry {i + 1}";
            if (entry == null)
            {
                errors.Add($"{prefix} is missing.");
                continue;
            }

            if (entry.IsPassthrough)
            {
                if ((entry.RawLine ?? string.Empty).Contains('\r') || (entry.RawLine ?? string.Empty).Contains('\n'))
                    errors.Add($"{prefix} raw line must not contain a line break.");
                continue;
            }

            if (entry.IsComment)
            {
                errors.Add($"{prefix} is a comment; preserve comments as passthrough lines instead.");
                continue;
            }

            foreach (var error in GetErrors(entry.Address ?? string.Empty, entry.Names ?? new List<string>()))
                errors.Add($"{prefix}: {error}");
        }

        return errors;
    }
}
