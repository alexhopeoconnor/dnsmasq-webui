using DnsmasqWebUI.Models.Dnsmasq.EffectiveConfig;

namespace DnsmasqWebUI.Models.DnsRecords;

/// <summary>Validation or conflict issue attached to a DNS record row.</summary>
public sealed record DnsRecordIssue(string Message, FieldIssueSeverity Severity);
