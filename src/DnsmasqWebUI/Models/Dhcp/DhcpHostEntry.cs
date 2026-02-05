namespace DnsmasqWebUI.Models.Dhcp;

/// <summary>One dhcp-host= line (MAC(s), optional address, name, lease, options). Used for GET/PUT api/dhcp/hosts.</summary>
public class DhcpHostEntry
{
    /// <summary>1-based line number in the config file. Used for display and matching.</summary>
    public int LineNumber { get; set; }

    /// <summary>Stable identifier (content-based: MACs|Address|Name, with ":LineNumber" for uniqueness). Set by server on GET; used to match entries on PUT so reordering is safe.</summary>
    public string Id { get; set; } = "";

    /// <summary>Original line text as read from the config (for passthrough and round-trip).</summary>
    public string RawLine { get; set; } = "";

    /// <summary>True when the line is a comment (starts with #).</summary>
    public bool IsComment { get; set; }

    /// <summary>True when the entry was marked for deletion in the UI (removed on save).</summary>
    public bool IsDeleted { get; set; }

    /// <summary>True when the line could not be parsed (preserved as RawLine on write).</summary>
    public bool Ignore { get; set; }

    /// <summary>MAC address(es) from the dhcp-host line. One or more; order preserved.</summary>
    public List<string> MacAddresses { get; set; } = new();

    /// <summary>Hostname or DHCP hostname from the line; null when not set.</summary>
    public string? Name { get; set; }

    /// <summary>Reserved IP address from the line; null when not set.</summary>
    public string? Address { get; set; }

    /// <summary>Lease identifier (e.g. "01:02:03:04:05:06"); null when not set.</summary>
    public string? Lease { get; set; }

    /// <summary>Extra tokens after the main dhcp-host fields (e.g. set:name); preserved on write.</summary>
    public List<string> Extra { get; set; } = new();

    /// <summary>Inline comment text from the line; null when none.</summary>
    public string? Comment { get; set; }

    /// <summary>True when this entry is from the managed file (editable). False when from main config or another file (read-only). Set by server on GET; new entries created in the UI should set this to true.</summary>
    public bool IsEditable { get; set; }

    /// <summary>Path to the config file this entry comes from. Set by server on GET; null for new entries not yet saved.</summary>
    public string? SourcePath { get; set; }
}
