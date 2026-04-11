using DnsmasqWebUI.Infrastructure.Serialization.Parsers.EffectiveConfig;

namespace DnsmasqWebUI.Models.Dhcp;

/// <summary>
/// Editable view model for one <c>dhcp-range=</c> value. Uses <see cref="DnsmasqDhcpRangeValueParser"/> for structure;
/// unknown shapes stay in <see cref="RawFallback"/> for lossless round-trip.
/// </summary>
public sealed class DhcpRangeDraft
{
    public IReadOnlyList<string> Tags { get; init; } = Array.Empty<string>();

    /// <summary>IPv4/IPv6 start, or <c>constructor:iface</c>.</summary>
    public string StartToken { get; init; } = "";

    /// <summary>End address, mode keyword, or second constructor segment per dnsmasq grammar.</summary>
    public string SecondToken { get; init; } = "";

    public IReadOnlyList<string> RemainingTokens { get; init; } = Array.Empty<string>();

    /// <summary>When set, the value could not be structured; use verbatim in UI.</summary>
    public string? RawFallback { get; init; }

    public bool IsStructured => RawFallback == null;

    public static DhcpRangeDraft FromRaw(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return new DhcpRangeDraft { RawFallback = raw };

        if (!DnsmasqDhcpRangeValueParser.TryParse(raw, out var p, out _) || p == null)
            return new DhcpRangeDraft { RawFallback = raw };

        return new DhcpRangeDraft
        {
            Tags = p.Tags,
            StartToken = p.StartToken,
            SecondToken = p.SecondToken,
            RemainingTokens = p.RemainingTokens
        };
    }

    public string ToRaw()
    {
        if (RawFallback != null)
            return RawFallback;

        var parts = new List<string>();
        parts.AddRange(Tags);
        parts.Add(StartToken);
        parts.Add(SecondToken);
        parts.AddRange(RemainingTokens);
        return string.Join(',', parts);
    }
}
