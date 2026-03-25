namespace DnsmasqWebUI.Models.Hosts;

/// <summary>Preview of names after <c>expand-hosts</c> (does not mutate stored names).</summary>
public static class HostsEffectiveNames
{
    public static IReadOnlyList<string> Expand(
        IReadOnlyList<string> names,
        bool expandHosts,
        string? domain)
    {
        if (!expandHosts || string.IsNullOrWhiteSpace(domain))
            return names;

        return names
            .SelectMany(name =>
                name.Contains('.')
                    ? new[] { name }
                    : new[] { name, $"{name}.{domain}" })
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }
}
