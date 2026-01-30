namespace DnsmasqWebUI.Models;

public class DhcpHostEntry
{
    public int LineNumber { get; set; }

    /// <summary>Stable identifier (content-based: MACs|Address|Name, with ":LineNumber" for uniqueness). Set by server on GET; used to match entries on PUT so reordering is safe.</summary>
    public string Id { get; set; } = "";

    public string RawLine { get; set; } = "";
    public bool IsComment { get; set; }
    public bool IsDeleted { get; set; }
    public bool Ignore { get; set; }
    public List<string> MacAddresses { get; set; } = new();
    public string? Name { get; set; }
    public string? Address { get; set; }
    public string? Lease { get; set; }
    public List<string> Extra { get; set; } = new();
    public string? Comment { get; set; }
}
