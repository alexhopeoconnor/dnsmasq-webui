using System.Text.Json.Serialization;
using DnsmasqWebUI.Models.Dhcp;

namespace DnsmasqWebUI.Models.Config;

/// <summary>Kind of line in a dnsmasq .conf file. One option per line, key=value (no leading --), # for comments.</summary>
public enum DnsmasqConfLineKind
{
    Blank,
    Comment,
    AddnHosts,
    DhcpHost,
    Other
}

/// <summary>One line of a dnsmasq .conf file, used for the managed file only. Use the concrete type (BlankLine, CommentLine, AddnHostsLine, DhcpHostLine, OtherLine). Round-trip via DnsmasqConfFileLineParser.ParseFile / ToLine. The same directive types (addn-hosts=, etc.) can appear in any conf file; we use DnsmasqConfLine only for the single managed file because that is the only file we parse into structured lines and write back. Effective config (and where each value came from) is built from all files via DnsmasqConfIncludeParser and GetEffectiveConfigWithSources.</summary>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "kind")]
[JsonDerivedType(typeof(BlankLine), "blank")]
[JsonDerivedType(typeof(CommentLine), "comment")]
[JsonDerivedType(typeof(AddnHostsLine), "addnHosts")]
[JsonDerivedType(typeof(DhcpHostLine), "dhcpHost")]
[JsonDerivedType(typeof(OtherLine), "other")]
public abstract class DnsmasqConfLine
{
    public int LineNumber { get; init; }
    public abstract DnsmasqConfLineKind Kind { get; }
}

public sealed class BlankLine : DnsmasqConfLine
{
    public override DnsmasqConfLineKind Kind => DnsmasqConfLineKind.Blank;
    public string RawLine { get; init; } = "";
}

public sealed class CommentLine : DnsmasqConfLine
{
    public override DnsmasqConfLineKind Kind => DnsmasqConfLineKind.Comment;
    public string RawLine { get; init; } = "";
}

public sealed class AddnHostsLine : DnsmasqConfLine
{
    public override DnsmasqConfLineKind Kind => DnsmasqConfLineKind.AddnHosts;
    public string AddnHostsPath { get; init; } = "";
}

public sealed class DhcpHostLine : DnsmasqConfLine
{
    public override DnsmasqConfLineKind Kind => DnsmasqConfLineKind.DhcpHost;
    public DhcpHostEntry DhcpHost { get; init; } = null!;
}

public sealed class OtherLine : DnsmasqConfLine
{
    public override DnsmasqConfLineKind Kind => DnsmasqConfLineKind.Other;
    public string RawLine { get; init; } = "";
}
