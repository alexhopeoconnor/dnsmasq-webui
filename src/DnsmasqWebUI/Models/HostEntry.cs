namespace DnsmasqWebUI.Models;

public class HostEntry
{
    /// <summary>1-based line number in the file. Used for display and as fallback when Id is not set.</summary>
    public int LineNumber { get; set; }

    /// <summary>Stable identifier: host entries use "Address|name1,name2" (content-based, stable across reorder); passthrough use "line:LineNumber". Set by server on GET; optional on PUT (order is preserved).</summary>
    public string Id { get; set; } = "";

    public string Address { get; set; } = "";
    public List<string> Names { get; set; } = new();
    public string RawLine { get; set; } = "";
    public bool IsComment { get; set; }
    public bool IsPassthrough { get; set; }
}
