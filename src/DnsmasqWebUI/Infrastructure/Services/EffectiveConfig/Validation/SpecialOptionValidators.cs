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
}
