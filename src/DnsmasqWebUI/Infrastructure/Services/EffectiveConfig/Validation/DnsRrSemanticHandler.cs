using System.Globalization;
using DnsmasqWebUI.Infrastructure.Helpers.Config;

namespace DnsmasqWebUI.Infrastructure.Services.EffectiveConfig.Validation;

/// <summary>
/// Semantic validation for <c>dns-rr</c> values: &lt;name&gt;,&lt;RR-number&gt;,[&lt;hex data&gt;].
/// Name and RR type number required; optional hex data (digits, colons, spaces).
/// </summary>
public sealed class DnsRrSemanticHandler : IOptionSemanticHandler
{
    public bool CanHandle(string optionName) => optionName == DnsmasqConfKeys.DnsRr;

    public string? ValidateSingle(object? value) => null;

    public string? ValidateMultiItem(string? value)
    {
        var s = (value ?? "").Trim();
        if (s.Length == 0)
            return "dns-rr value cannot be empty.";
        var tokens = s.Split(',', 3, StringSplitOptions.TrimEntries);
        if (tokens.Length < 2)
            return "dns-rr must be <name>,<RR-number>[,<hex data>].";
        if (tokens[0].Length == 0)
            return "dns-rr name cannot be empty.";
        if (!DnsmasqScopedDomainSyntax.IsValidDnsName(tokens[0]))
            return "dns-rr name must be a valid DNS name.";
        if (!ushort.TryParse(tokens[1], NumberStyles.None, CultureInfo.InvariantCulture, out _))
            return "dns-rr RR-number must be 0-65535.";
        if (tokens.Length == 3 && tokens[2].Length > 0 && !IsValidHexData(tokens[2]))
            return "dns-rr hex data must be hex digits, colons or spaces (e.g. 01:23:45 or 012345).";
        return null;
    }

    private static bool IsValidHexData(string value) =>
        value.All(c => char.IsAsciiHexDigit(c) || c is ':' or ' ' or '\t');
}
