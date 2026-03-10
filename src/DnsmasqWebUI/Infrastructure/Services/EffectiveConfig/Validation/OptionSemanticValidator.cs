using System.Net;
using System.Net.Sockets;
using DnsmasqWebUI.Infrastructure.Helpers.Config;
using DnsmasqWebUI.Models.Dnsmasq.EffectiveConfig;

namespace DnsmasqWebUI.Infrastructure.Services.EffectiveConfig.Validation;

/// <summary>
/// Central validator for effective-config values. Specialized handlers are consulted first;
/// otherwise validation falls back to generic <see cref="OptionValidationKind"/>-based rules.
/// </summary>
public sealed class OptionSemanticValidator : IOptionSemanticValidator
{
    private readonly IReadOnlyList<IOptionSemanticHandler> _handlers;

    public OptionSemanticValidator(IEnumerable<IOptionSemanticHandler>? handlers = null)
    {
        _handlers = (handlers ?? Array.Empty<IOptionSemanticHandler>()).ToList();
    }

    /// <inheritdoc />
    public string? ValidateSingle(string optionName, object? value, OptionValidationSemantics semantics)
    {
        var handler = _handlers.FirstOrDefault(h => h.CanHandle(optionName));
        if (handler is not null)
            return handler.ValidateSingle(value);

        return ValidateSingleByKind(optionName, value, semantics);
    }

    /// <inheritdoc />
    public string? ValidateMultiItem(string optionName, string? value, OptionValidationSemantics semantics)
    {
        var handler = _handlers.FirstOrDefault(h => h.CanHandle(optionName));
        if (handler is not null)
            return handler.ValidateMultiItem(value);

        return ValidateMultiByKind(optionName, value, semantics);
    }

    private static string? ValidateSingleByKind(string optionName, object? value, OptionValidationSemantics semantics)
    {
        return semantics.Kind switch
        {
            OptionValidationKind.Flag or OptionValidationKind.InversePair => null,
            OptionValidationKind.Int => ValidateInt(value),
            OptionValidationKind.String => ValidateString(value?.ToString(), semantics),
            OptionValidationKind.PathFile or OptionValidationKind.PathDirectory or OptionValidationKind.PathFileOrDirectory => ValidatePath(value?.ToString(), semantics),
            OptionValidationKind.IpAddress => ValidateIpAddress(value?.ToString(), semantics),
            OptionValidationKind.HostOrIp => ValidateHostOrIp(value?.ToString(), semantics),
            OptionValidationKind.KeyOnlyOrValue => ValidateKeyOnlyOrValue(optionName, value?.ToString()),
            OptionValidationKind.Complex => null,
            _ => null
        };
    }

    private static string? ValidateMultiByKind(string optionName, string? value, OptionValidationSemantics semantics) =>
        semantics.Kind switch
        {
            OptionValidationKind.String => ValidateString(value, semantics),
            OptionValidationKind.PathFile or OptionValidationKind.PathDirectory or OptionValidationKind.PathFileOrDirectory => ValidatePath(value, semantics),
            OptionValidationKind.IpAddress => ValidateIpAddress(value, semantics),
            OptionValidationKind.HostOrIp => ValidateHostOrIp(value, semantics),
            OptionValidationKind.KeyOnlyOrValue => ValidateKeyOnlyOrValue(optionName, value),
            OptionValidationKind.Complex => null,
            _ => null
        };

    private static string? ValidateInt(object? value)
    {
        if (value is null) return null;
        if (value is int) return null;
        var s = value.ToString()?.Trim();
        if (string.IsNullOrEmpty(s)) return null;
        return int.TryParse(s, out _) ? null : "Must be a valid integer.";
    }

    private static string? ValidateString(string? value, OptionValidationSemantics semantics)
    {
        if (!semantics.AllowEmpty && string.IsNullOrWhiteSpace(value))
            return "Value cannot be empty.";
        return null;
    }

    private static string? ValidatePath(string? value, OptionValidationSemantics semantics)
    {
        if (string.IsNullOrWhiteSpace(value))
            return semantics.AllowEmpty ? null : "Path cannot be empty.";

        var path = value.Trim();
        return semantics.PathPolicy switch
        {
            PathExistencePolicy.MustExist when !PathExistsForKind(path, semantics.Kind)
                => GetMissingPathMessage(semantics.Kind),
            PathExistencePolicy.ParentMustExist when !Directory.Exists(Path.GetDirectoryName(path) ?? "")
                => "Parent directory does not exist.",
            _ => null
        };
    }

    private static bool PathExistsForKind(string path, OptionValidationKind kind) =>
        kind switch
        {
            OptionValidationKind.PathFile => File.Exists(path),
            OptionValidationKind.PathDirectory => Directory.Exists(path),
            OptionValidationKind.PathFileOrDirectory => File.Exists(path) || Directory.Exists(path),
            _ => File.Exists(path) || Directory.Exists(path),
        };

    private static string GetMissingPathMessage(OptionValidationKind kind) =>
        kind switch
        {
            OptionValidationKind.PathFile => "File does not exist.",
            OptionValidationKind.PathDirectory => "Directory does not exist.",
            _ => "Path does not exist.",
        };

    private static string? ValidateIpAddress(string? value, OptionValidationSemantics semantics)
    {
        if (string.IsNullOrWhiteSpace(value))
            return semantics.AllowEmpty ? null : "Value cannot be empty.";
        if (!IPAddress.TryParse(value, out var ip)) return "Enter a valid IP address.";
        if (ip.AddressFamily != AddressFamily.InterNetwork && ip.AddressFamily != AddressFamily.InterNetworkV6)
            return "Invalid IP address.";
        return null;
    }

    private static string? ValidateHostOrIp(string? value, OptionValidationSemantics semantics)
    {
        if (string.IsNullOrWhiteSpace(value))
            return semantics.AllowEmpty ? null : "Value cannot be empty.";
        if (IPAddress.TryParse(value, out var ip))
        {
            if (ip.AddressFamily != AddressFamily.InterNetwork && ip.AddressFamily != AddressFamily.InterNetworkV6)
                return "Invalid IP address.";
            return null;
        }
        // Simple hostname check
        if (value.Length <= 253 && !value.Contains("://", StringComparison.Ordinal))
            return null;
        return "Enter a valid IP address or hostname.";
    }

    private static string? ValidateKeyOnlyOrValue(string optionName, string? value)
    {
        var s = value?.Trim() ?? "";
        if (s.Length == 0)
            return null;

        return optionName switch
        {
            DnsmasqConfKeys.UseStaleCache => int.TryParse(s, out var n) && n >= 0
                ? null
                : "use-stale-cache must be empty or a non-negative integer.",
            DnsmasqConfKeys.AddMac => s is "base64" or "text"
                ? null
                : "add-mac must be empty, 'base64', or 'text'.",
            // Intentionally permissive for now: dnsmasq accepts richer option-specific syntax here,
            // and we do not yet model that syntax centrally.
            DnsmasqConfKeys.AddSubnet => null,
            DnsmasqConfKeys.Umbrella => null,
            DnsmasqConfKeys.ConnmarkAllowlistEnable => ValidateConnmarkAllowlistEnableMask(s),
            DnsmasqConfKeys.DnssecCheckUnsigned => s is "no"
                ? null
                : "Allowed values: empty (enable check) or 'no'.",
            _ => null
        };
    }

    private static string? ValidateConnmarkAllowlistEnableMask(string value)
    {
        if (value.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
        {
            return uint.TryParse(value[2..], System.Globalization.NumberStyles.HexNumber, null, out _)
                ? null
                : "Mask must be valid hex after 0x.";
        }

        return uint.TryParse(value, out _)
            ? null
            : "Mask must be empty, decimal uint, or hex (0x...).";
    }
}
