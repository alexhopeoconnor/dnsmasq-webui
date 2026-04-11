namespace DnsmasqWebUI.Models.DnsRecords;

public abstract record DnsRecordPayload;

public sealed record CnamePayload(
    IReadOnlyList<string> Aliases,
    string Target,
    int? Ttl) : DnsRecordPayload;

public sealed record HostRecordPayload(
    IReadOnlyList<string> Owners,
    string? IPv4,
    string? IPv6,
    int? Ttl) : DnsRecordPayload;

public sealed record TxtPayload(string Name, string? Text) : DnsRecordPayload;

public sealed record PtrPayload(string Name, string? Target) : DnsRecordPayload;

public sealed record MxPayload(string Domain, string? Hostname, int? Preference) : DnsRecordPayload;

public sealed record SrvPayload(
    string ServiceName,
    string? Target,
    int? Port,
    int? Priority,
    int? Weight) : DnsRecordPayload;

public sealed record NaptrPayload(
    string Name,
    string Order,
    string Preference,
    string Flags,
    string Service,
    string Regexp,
    string? Replacement) : DnsRecordPayload;

public sealed record CaaPayload(string Name, string Flags, string Tag, string Value) : DnsRecordPayload;

public sealed record DnsRrPayload(string Name, string RrNumber, string? HexData) : DnsRecordPayload;

public sealed record DynamicHostPayload(
    string Name,
    string? IPv4,
    string? IPv6,
    string Interface) : DnsRecordPayload;

public sealed record InterfaceNamePayload(string DnsName, string InterfaceSpec) : DnsRecordPayload;

public sealed record SynthDomainPayload(string Domain, string AddressRange, string? Prefix) : DnsRecordPayload;

public sealed record AuthZonePayload(string Domain, IReadOnlyList<string> SubnetsAndExcludes) : DnsRecordPayload;

public sealed record AuthSoaPayload(
    string Serial,
    string? Hostmaster,
    string? Refresh,
    string? Retry,
    string? Expiry) : DnsRecordPayload;

public sealed record AuthSecServersPayload(IReadOnlyList<string> Domains) : DnsRecordPayload;

public sealed record AuthPeerPayload(IReadOnlyList<string> Ips) : DnsRecordPayload;

/// <summary>Fallback when no structured codec applies.</summary>
public sealed record RawPayload(string Value) : DnsRecordPayload;
