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

    /// <summary>True when this entry is from the managed file (editable). False when from main config or another file (read-only). Set by server on GET; new entries created in the UI should set this to true.</summary>
    public bool IsEditable { get; set; }

    /// <summary>Path to the config file this entry comes from. Set by server on GET; null for new entries not yet saved.</summary>
    public string? SourcePath { get; set; }
}
