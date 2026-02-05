namespace DnsmasqWebUI.Models.Hosts;

/// <summary>One line or entry in an /etc/hosts-style file (IP, names, comment, or passthrough).</summary>
public class HostEntry
{
    /// <summary>1-based line number in the file. Used for display and as fallback when Id is not set.</summary>
    public int LineNumber { get; set; }

    /// <summary>Stable identifier: host entries use "Address|name1,name2" (content-based, stable across reorder); passthrough use "line:LineNumber". Set by server on GET; optional on PUT (order is preserved).</summary>
    public string Id { get; set; } = "";

    /// <summary>IP address (e.g. 192.168.1.1). Empty for comment or passthrough lines.</summary>
    public string Address { get; set; } = "";

    /// <summary>Canonical hostname and aliases. Order preserved.</summary>
    public List<string> Names { get; set; } = new();

    /// <summary>Original line text as read from the file (for passthrough and round-trip).</summary>
    public string RawLine { get; set; } = "";

    /// <summary>True when the line is a comment (starts with #).</summary>
    public bool IsComment { get; set; }

    /// <summary>True when the line could not be parsed as address/names (e.g. malformed or comment); preserved as RawLine on write.</summary>
    public bool IsPassthrough { get; set; }
}
