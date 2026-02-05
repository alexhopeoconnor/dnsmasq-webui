namespace DnsmasqWebUI.Models.Dhcp;

/// <summary>Result of GET api/leases: whether leases are available and the list of entries.</summary>
/// <param name="Available">True when the leases file is configured and readable; false when not configured or path invalid.</param>
/// <param name="Entries">Lease entries; null when not available or file unreadable (Message explains).</param>
/// <param name="Message">Error or status message (e.g. "Leases not configured."); null when successful.</param>
public record LeasesResult(bool Available, IReadOnlyList<LeaseEntry>? Entries, string? Message);
