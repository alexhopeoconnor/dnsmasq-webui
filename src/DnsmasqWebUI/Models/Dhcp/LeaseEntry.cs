namespace DnsmasqWebUI.Models.Dhcp;

/// <summary>One DHCP lease entry from the dnsmasq leases file (timestamp, MAC, IP, hostname, client-id).</summary>
public class LeaseEntry
{
    /// <summary>Lease expiry time as Unix timestamp (seconds since epoch).</summary>
    public long Epoch { get; set; }

    /// <summary>Client MAC address.</summary>
    public string Mac { get; set; } = "";

    /// <summary>Assigned IP address.</summary>
    public string Address { get; set; } = "";

    /// <summary>Hostname from DHCP or DNS; empty when unknown.</summary>
    public string Name { get; set; } = "";

    /// <summary>DHCP client identifier; empty when not set.</summary>
    public string ClientId { get; set; } = "";

    /// <summary>Lease expiry as DateTime (derived from Epoch).</summary>
    public DateTime Timestamp => DateTimeOffset.FromUnixTimeSeconds(Epoch).DateTime;
}
