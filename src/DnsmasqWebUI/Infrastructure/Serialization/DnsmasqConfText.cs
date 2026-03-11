namespace DnsmasqWebUI.Infrastructure.Serialization;

/// <summary>
/// Shared dnsmasq directive text mechanics: key=, key=value, strip prefix, and match checks for option, #option=, ##option=.
/// Use with <see cref="DnsmasqWebUI.Infrastructure.Services.EffectiveConfig.Metadata.DnsmasqConfKeys"/> for option names.
/// </summary>
public static class DnsmasqConfText
{
    /// <summary>Returns the directive prefix for an option (e.g. "dhcp-host=").</summary>
    public static string DirectivePrefix(string optionName) => $"{optionName}=";

    /// <summary>Returns a full directive line: option=value, or just option when value is null/empty.</summary>
    public static string DirectiveLine(string optionName, string? value) =>
        string.IsNullOrEmpty(value) ? optionName : $"{optionName}={value}";

    /// <summary>Strips the option= prefix from a line (option=, #option=, ##option=); preserves leading #/## so value round-trips. Returns the full line if it does not match.</summary>
    public static string StripDirectivePrefix(string optionName, string line)
    {
        var prefix = DirectivePrefix(optionName);
        if (line.StartsWith("##" + prefix, StringComparison.Ordinal))
            return "##" + line[(2 + prefix.Length)..];
        if (line.StartsWith("#" + prefix, StringComparison.Ordinal))
            return "#" + line[(1 + prefix.Length)..];
        if (line.StartsWith(prefix, StringComparison.Ordinal))
            return line[prefix.Length..];
        return line;
    }

    /// <summary>True when the line is an option directive, optionally with # or ## prefix.</summary>
    public static bool HasDirectivePrefix(string optionName, string line)
    {
        var prefix = DirectivePrefix(optionName);
        return line.StartsWith(prefix, StringComparison.Ordinal) ||
               line.StartsWith("#" + prefix, StringComparison.Ordinal) ||
               line.StartsWith("##" + prefix, StringComparison.Ordinal);
    }
}
