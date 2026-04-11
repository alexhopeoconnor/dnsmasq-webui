using DnsmasqWebUI.Models.Dnsmasq.EffectiveConfig;

namespace DnsmasqWebUI.Models.DnsRecords;

/// <summary>One effective-config value row for the DNS records page.</summary>
public sealed record DnsRecordRow(
    string Id,
    string OccurrenceId,
    string OptionName,
    DnsRecordFamily Family,
    int IndexInOption,
    string RawValue,
    ConfigValueSource? Source,
    string? SourcePath,
    string? SourceLabel,
    bool IsDraftOnly,
    bool IsEditable,
    DnsRecordPayload Payload,
    IReadOnlyList<DnsRecordIssue> Issues,
    string Summary);
