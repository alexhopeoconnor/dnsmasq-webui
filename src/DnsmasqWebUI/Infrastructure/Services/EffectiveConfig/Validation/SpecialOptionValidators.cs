using DnsmasqWebUI.Models.Dnsmasq.EffectiveConfig;

namespace DnsmasqWebUI.Infrastructure.Services.EffectiveConfig.Validation;

/// <summary>
/// Single-value validators for effective-config options with special semantics (key-only or key=value, allowed values).
/// Used by the descriptor factory so field-level validation shows friendly errors in the edit flow.
/// </summary>
public static class SpecialOptionValidators
{
    /// <summary>use-stale-cache: unset, key-only, or non-negative integer.</summary>
    public static string? ValidateUseStaleCache(object? value)
    {
        if (value is null) return null;
        var s = value.ToString()?.Trim() ?? "";
        if (s.Length == 0) return null; // key-only
        return int.TryParse(s, out var n) && n >= 0
            ? null
            : "use-stale-cache must be empty or a non-negative integer.";
    }

    /// <summary>add-mac: unset, key-only, or 'base64' or 'text'.</summary>
    public static string? ValidateAddMac(object? value)
    {
        if (value is null) return null;
        var s = value.ToString()?.Trim() ?? "";
        if (s.Length == 0) return null; // key-only
        return s is "base64" or "text"
            ? null
            : "add-mac must be empty, 'base64', or 'text'.";
    }

    /// <summary>add-subnet: unset, key-only, or value (permissive; format not strictly validated).</summary>
    public static string? ValidateAddSubnet(object? value)
    {
        if (value is null) return null;
        return null; // accept any string for now
    }

    /// <summary>umbrella: unset, key-only, or token list (permissive).</summary>
    public static string? ValidateUmbrella(object? value)
    {
        if (value is null) return null;
        var s = value.ToString()?.Trim() ?? "";
        if (s.Length == 0) return null; // key-only
        return null; // token parsing can be tightened later
    }

    /// <summary>connmark-allowlist-enable: key-only or optional mask (decimal uint or 0x hex).</summary>
    public static string? ValidateConnmarkAllowlistEnable(object? value)
    {
        if (value is null) return null;
        var s = value.ToString()?.Trim() ?? "";
        if (s.Length == 0) return null; // key-only
        if (s.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
            return uint.TryParse(s[2..], System.Globalization.NumberStyles.HexNumber, null, out _)
                ? null
                : "Mask must be valid hex after 0x.";
        return uint.TryParse(s, out _) ? null : "Mask must be empty, decimal uint, or hex (0x...).";
    }

    /// <summary>dnssec-check-unsigned: key-only (enable check) or 'no' (disable).</summary>
    public static string? ValidateDnssecCheckUnsigned(object? value)
    {
        if (value is null) return null;
        var s = value.ToString()?.Trim() ?? "";
        return s is "" or "no" ? null : "Allowed values: empty (enable check) or 'no'.";
    }

    /// <summary>Per-item validation for leasequery: key-only or IP[/prefix].</summary>
    public static string? ValidateLeasequeryValue(string? s)
    {
        if (string.IsNullOrWhiteSpace(s)) return null; // key-only
        var parts = s.Trim().Split('/', 2);
        if (!System.Net.IPAddress.TryParse(parts[0], out _)) return "Leasequery source must be an IP address.";
        if (parts.Length == 2 && !int.TryParse(parts[1], out _)) return "Prefix must be numeric.";
        return null;
    }
}
