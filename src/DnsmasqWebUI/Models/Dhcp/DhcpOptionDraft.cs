using DnsmasqWebUI.Infrastructure.Serialization.Parsers.EffectiveConfig;

namespace DnsmasqWebUI.Models.Dhcp;

/// <summary>Workflow model for <c>dhcp-option</c> / <c>dhcp-option-force</c> values.</summary>
public sealed class DhcpOptionDraft
{
    /// <summary>Leading tokens: tag:, encap:, vendor:, etc.</summary>
    public IReadOnlyList<string> PrefixTokens { get; init; } = Array.Empty<string>();

    /// <summary>e.g. <c>option:router</c>, <c>3</c>, <c>option6:dns-server</c>.</summary>
    public string Selector { get; init; } = "";

    public IReadOnlyList<string> ValueTokens { get; init; } = Array.Empty<string>();

    /// <summary>Unstructured fallback when token split is ambiguous.</summary>
    public string? RawFallback { get; init; }

    public bool IsStructured => RawFallback == null;

    public static DhcpOptionDraft FromRaw(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return new DhcpOptionDraft { RawFallback = raw };

        var tokens = DnsmasqDhcpOptionSyntax.SplitTokens(raw);
        if (DnsmasqDhcpOptionSyntax.HasEmptyToken(tokens))
            return new DhcpOptionDraft { RawFallback = raw };

        var prefixes = new List<string>();
        var i = 0;
        while (i < tokens.Length)
        {
            var t = tokens[i];
            if (t.StartsWith("tag:", StringComparison.OrdinalIgnoreCase) ||
                t.StartsWith("set:", StringComparison.OrdinalIgnoreCase))
            {
                prefixes.Add(t);
                i++;
                continue;
            }

            if (!DnsmasqDhcpOptionSyntax.IsPrefixToken(t, out var pErr) || pErr != null)
                break;
            prefixes.Add(t);
            i++;
        }

        if (i >= tokens.Length)
            return new DhcpOptionDraft { RawFallback = raw };

        if (!DnsmasqDhcpOptionSyntax.IsOptionSelector(tokens[i]))
            return new DhcpOptionDraft { RawFallback = raw };

        var selector = tokens[i];
        i++;
        var values = tokens.Skip(i).ToList();
        return new DhcpOptionDraft
        {
            PrefixTokens = prefixes,
            Selector = selector,
            ValueTokens = values
        };
    }

    public string ToRaw()
    {
        if (RawFallback != null)
            return RawFallback;

        var parts = new List<string>();
        parts.AddRange(PrefixTokens);
        parts.Add(Selector);
        parts.AddRange(ValueTokens);
        return string.Join(',', parts);
    }
}

public sealed record DhcpOptionPreset(string Key, string Label, string Selector, IReadOnlyList<string> DefaultValues);

public static class DhcpOptionPresets
{
    public static readonly IReadOnlyList<DhcpOptionPreset> All =
    [
        new("router", "Default router (IPv4)", "option:router", ["192.168.1.1"]),
        new("dns", "DNS servers", "option:dns-server", ["192.168.1.1"]),
        new("dns6", "DNS servers (IPv6)", "option6:dns-server", ["2001:db8::1"]),
        new("domain", "Domain name", "option:domain-name", ["home.arpa"]),
        new("search", "DNS search list", "option:domain-search", ["home.arpa"]),
        new("ntp", "NTP servers", "option:ntp-server", ["192.168.1.1"]),
        new("pxe_magic", "PXE magic (175)", "option:pxe-cs", ["0"]),
    ];
}
