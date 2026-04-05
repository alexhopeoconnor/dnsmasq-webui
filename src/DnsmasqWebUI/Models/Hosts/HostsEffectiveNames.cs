namespace DnsmasqWebUI.Models.Hosts;

/// <summary>Preview of names after <c>expand-hosts</c> (does not mutate stored names).</summary>
public static class HostsEffectiveNames
{
    private sealed record ScopedDomainRule(string Raw, uint Start, uint End, string Domain);

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

    public static IReadOnlyList<string> Expand(
        IReadOnlyList<string> names,
        bool expandHosts,
        IReadOnlyList<string>? domainValues,
        string? address)
    {
        var domain = ResolveDomainForAddress(domainValues, address);
        return Expand(names, expandHosts, domain);
    }

    /// <summary>
    /// Picks the suffix domain for expand-hosts, mirroring dnsmasq <c>read_hostsfile</c> → <c>get_domain</c> (<c>domain.c</c>).
    /// Conditional <c>domain=</c> lines are stored in a singly-linked list by prepending each new rule
    /// (<c>option.c</c>: <c>new-&gt;next = daemon-&gt;cond_domain</c>), and <c>search_domain</c> returns the first match
    /// walking from the head — so <strong>later</strong> occurrences in the config (later files / later lines) win when
    /// ranges overlap. <see cref="EffectiveDnsmasqConfig.DomainValues"/> keeps multi-value options in that read order.
    /// </summary>
    public static string? ResolveDomainForAddress(
        IReadOnlyList<string>? domainValues,
        string? address)
    {
        if (domainValues == null || domainValues.Count == 0)
            return null;

        string? defaultDomain = null;
        var scopedRules = new List<ScopedDomainRule>();

        foreach (var raw in domainValues)
            ParseDomainValue(raw, scopedRules, ref defaultDomain);

        if (!TryParseIpv4ToUInt(address, out var ip))
            return defaultDomain;

        // Walk scoped rules last-to-first in config order ≡ dnsmasq's head-first list walk (later line wins on overlap).
        for (var i = scopedRules.Count - 1; i >= 0; i--)
        {
            var rule = scopedRules[i];
            if (ip >= rule.Start && ip <= rule.End)
                return rule.Domain;
        }

        return defaultDomain;
    }

    /// <summary>Short explanation for UI tooltips: which domain= rule supplies the suffix for this row address.</summary>
    public static string ExplainSuffixSource(
        bool expandHosts,
        IReadOnlyList<string>? domainValues,
        string? address,
        IReadOnlyList<string>? names)
    {
        if (!expandHosts)
            return "expand-hosts is off; dnsmasq does not add a domain suffix to short names.";

        if (domainValues == null || domainValues.Count == 0)
            return "No domain= lines are configured; there is no suffix to append.";

        var nameList = names ?? Array.Empty<string>();
        if (nameList.Count == 0 || nameList.All(n => string.IsNullOrWhiteSpace(n)))
            return "No hostnames on this row.";

        var nonEmpty = nameList.Where(n => !string.IsNullOrWhiteSpace(n)).ToList();
        if (nonEmpty.Count > 0 && nonEmpty.All(n => n.Contains('.')))
            return "Every name already contains a dot; dnsmasq does not append a suffix (expand-hosts only affects simple names).";

        ParseAllDomainValues(domainValues, out var defaultDomain, out var defaultRaw, out var scopedRules);

        if (TryParseIpv4ToUInt(address, out var ip))
        {
            for (var i = scopedRules.Count - 1; i >= 0; i--)
            {
                var rule = scopedRules[i];
                if (ip >= rule.Start && ip <= rule.End)
                    return $"Suffix from scoped rule: {rule.Raw}";
            }

            if (!string.IsNullOrWhiteSpace(defaultDomain))
                return string.IsNullOrWhiteSpace(defaultRaw)
                    ? $"Suffix from default domain: {defaultDomain}"
                    : $"Suffix from default rule: {defaultRaw}";

            return "No default domain= is set; this IPv4 address matched no scoped rule, so no suffix is applied in this preview.";
        }

        // IPv6 or non-IP: dnsmasq host expansion uses default suffix only (see read_hostsfile in dnsmasq).
        if (!string.IsNullOrWhiteSpace(defaultDomain))
            return string.IsNullOrWhiteSpace(defaultRaw)
                ? $"Non-IPv4 address: preview uses default domain only: {defaultDomain}"
                : $"Non-IPv4 address: preview uses default rule: {defaultRaw}";

        return "Non-IPv4 address and no default domain=; no suffix in this preview.";
    }

    private static void ParseAllDomainValues(
        IReadOnlyList<string> domainValues,
        out string? defaultDomain,
        out string? defaultRaw,
        out List<ScopedDomainRule> scopedRules)
    {
        defaultDomain = null;
        defaultRaw = null;
        scopedRules = new List<ScopedDomainRule>();

        foreach (var raw in domainValues)
        {
            if (string.IsNullOrWhiteSpace(raw))
                continue;
            var parts = raw.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 0)
                continue;
            var domain = NormalizeDomain(parts[0]);
            if (string.IsNullOrWhiteSpace(domain))
                continue;

            if (parts.Length == 1)
            {
                defaultDomain = domain;
                defaultRaw = raw.Trim();
                continue;
            }

            if (TryParseScopedRange(parts, out var start, out var end))
                scopedRules.Add(new ScopedDomainRule(raw.Trim(), start, end, domain));
        }
    }

    private static void ParseDomainValue(
        string? raw,
        ICollection<ScopedDomainRule> scopedRules,
        ref string? defaultDomain)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return;

        var parts = raw.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0)
            return;

        var domain = NormalizeDomain(parts[0]);
        if (string.IsNullOrWhiteSpace(domain))
            return;

        if (parts.Length == 1)
        {
            defaultDomain = domain;
            return;
        }

        if (TryParseScopedRange(parts, out var start, out var end))
            scopedRules.Add(new ScopedDomainRule(raw.Trim(), start, end, domain));
    }

    private static string? NormalizeDomain(string value)
    {
        var domain = value.Trim();
        if (domain.Length == 0 || domain == "#")
            return null;
        return domain;
    }

    private static bool TryParseScopedRange(string[] parts, out uint start, out uint end)
    {
        start = 0;
        end = 0;
        if (parts.Length < 2)
            return false;

        var second = parts[1];
        if (TryParseIpv4Cidr(second, out start, out end))
            return true;

        if (TryParseIpv4ToUInt(second, out start))
        {
            // domain=a.b.c.d,ip-start,ip-end
            if (parts.Length >= 3 && TryParseIpv4ToUInt(parts[2], out end))
            {
                if (end < start)
                    (start, end) = (end, start);
                return true;
            }

            end = start;
            return true;
        }

        return false;
    }

    private static bool TryParseIpv4Cidr(string value, out uint start, out uint end)
    {
        start = 0;
        end = 0;
        var slashIndex = value.IndexOf('/');
        if (slashIndex <= 0 || slashIndex >= value.Length - 1)
            return false;

        var ipPart = value[..slashIndex];
        var prefixPart = value[(slashIndex + 1)..];
        if (!TryParseIpv4ToUInt(ipPart, out var ip) || !int.TryParse(prefixPart, out var prefix) || prefix < 0 || prefix > 32)
            return false;

        var mask = prefix == 0 ? 0u : uint.MaxValue << (32 - prefix);
        start = ip & mask;
        end = start | ~mask;
        return true;
    }

    private static bool TryParseIpv4ToUInt(string? value, out uint result)
    {
        result = 0;
        if (string.IsNullOrWhiteSpace(value) || !System.Net.IPAddress.TryParse(value, out var ip))
            return false;

        var bytes = ip.GetAddressBytes();
        if (bytes.Length != 4)
            return false;

        result = ((uint)bytes[0] << 24)
               | ((uint)bytes[1] << 16)
               | ((uint)bytes[2] << 8)
               | bytes[3];
        return true;
    }
}
