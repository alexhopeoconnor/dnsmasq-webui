namespace DnsmasqWebUI.Models.Dhcp;

/// <summary>Result of GET api/leases: whether leases are available and the list of entries.</summary>
public record LeasesResult(bool Available, IReadOnlyList<LeaseEntry>? Entries, string? Message);
