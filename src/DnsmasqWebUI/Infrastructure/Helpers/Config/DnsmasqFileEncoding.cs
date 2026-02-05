using System.Text;

namespace DnsmasqWebUI.Infrastructure.Helpers.Config;

/// <summary>Encoding and reading helpers for dnsmasq config and hosts files. UTF-8 without BOM so dnsmasq does not see a BOM as "bad option" on line 1.</summary>
public static class DnsmasqFileEncoding
{
    private const char Utf8BomChar = '\uFEFF';

    public static readonly Encoding Utf8NoBom = new UTF8Encoding(false);

    /// <summary>Read config file as UTF-8 and strip UTF-8 BOM from first line if present, so conf-dir= etc. are recognized.</summary>
    public static string[] ReadConfigLines(string path)
    {
        var lines = File.ReadAllLines(path, Encoding.UTF8);
        StripBomFromFirstLine(lines);
        return lines;
    }

    /// <summary>Strip UTF-8 BOM from first line if present (mutates list). Use so we do not perpetuate BOM when writing.</summary>
    public static void StripBomFromFirstLine(IList<string> lines)
    {
        if (lines.Count == 0) return;
        var first = lines[0];
        if (first.Length > 0 && first[0] == Utf8BomChar)
            lines[0] = first[1..];
    }
}
