namespace DnsmasqWebUI.Models.DnsRecords;

/// <summary>Search and filter state for the DNS records page projection.</summary>
public sealed record DnsRecordsQueryState(
    string Search,
    DnsRecordsUiFamily UiFamily,
    string? SourcePathFilter,
    bool ShowReadOnly,
    bool OnlyWithIssues);
