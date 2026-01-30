namespace DnsmasqWebUI.Models;

/// <summary>Kind of line in a dnsmasq config file. Config format: one option per line, key=value (no leading --), # for comments.</summary>
public enum ConfigLineKind
{
    Blank,
    Comment,
    DhcpHost,
    Other
}

/// <summary>One line of a dnsmasq config file. For Blank/Comment/Other we preserve RawLine; for DhcpHost we have a parsed DhcpHostEntry.</summary>
public sealed class ConfigLine
{
    public ConfigLineKind Kind { get; init; }
    public int LineNumber { get; init; }
    public string RawLine { get; init; } = "";
    public DhcpHostEntry? DhcpHost { get; init; }
}
