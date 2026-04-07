namespace DnsmasqWebUI.Models.Hosts;

/// <summary>One hosts file row in the filter menu (path, short name, non-comment line count).</summary>
public sealed record HostsFileFilterMenuItem(string Path, string FileName, int RecordCount);

/// <summary>A source-kind section in the hosts file filter menu.</summary>
public sealed record HostsFileFilterMenuGroup(
    HostsRowSourceKind SourceKind,
    string Title,
    IReadOnlyList<HostsFileFilterMenuItem> Files);
