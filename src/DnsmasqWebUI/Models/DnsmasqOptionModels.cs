namespace DnsmasqWebUI.Models;

/// <summary>Backing model for conf-file=path. Parsed from main config only.</summary>
public record ConfFileOption(string Path, int LineNumber, string SourceFilePath);

/// <summary>Backing model for conf-dir=path[,suffix]. Parsed from main config only.</summary>
public record ConfDirOption(string Path, string? Suffix, int LineNumber, string SourceFilePath);

/// <summary>Backing model for addn-hosts=path. Cumulative across files.</summary>
public record AddnHostsOption(string Path, int LineNumber, string SourceFilePath);

/// <summary>Backing model for dhcp-leasefile= or dhcp-lease-file=. Last wins.</summary>
public record DhcpLeaseFileOption(string Path, int LineNumber, string SourceFilePath);

/// <summary>Backing model for domain=value. Last wins (or first per-subnet if domain=name,subnet).</summary>
public record DomainOption(string Value, int LineNumber, string SourceFilePath);

/// <summary>Generic path-valued option (resolv-file, dhcp-hostsfile, etc.).</summary>
public record PathOption(string Key, string Path, int LineNumber, string SourceFilePath);

/// <summary>Generic key=value option (port, user, cache-size, etc.) or unknown option.</summary>
public record RawOption(string Key, string Value, int LineNumber, string SourceFilePath);

/// <summary>Backing model for dhcp-range=... (start,end,netmask,lease or tag:..., set:..., etc.). Parsed by DhcpRangeOptionParser when needed.</summary>
public record DhcpRangeOption(string RawValue, int LineNumber, string SourceFilePath);

/// <summary>Backing model for dhcp-option=... (option:value or option:value,value). Raw value for now.</summary>
public record DhcpOptionOption(string RawValue, int LineNumber, string SourceFilePath);

/// <summary>Backing model for server=/domain/ip or server=ip. Raw value for now.</summary>
public record ServerOption(string RawValue, int LineNumber, string SourceFilePath);

/// <summary>Backing model for local=/domain/. Raw value for now.</summary>
public record LocalOption(string RawValue, int LineNumber, string SourceFilePath);

/// <summary>Backing model for address=/domain/ip. Raw value for now.</summary>
public record AddressOption(string RawValue, int LineNumber, string SourceFilePath);
