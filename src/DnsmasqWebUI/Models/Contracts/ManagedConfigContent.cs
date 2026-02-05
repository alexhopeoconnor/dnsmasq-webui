using DnsmasqWebUI.Models.Config;

namespace DnsmasqWebUI.Models.Contracts;

/// <summary>Full managed file content. EffectiveHostsPathInFile is parsed from AddnHosts line if present (for display only).</summary>
/// <param name="Lines">Parsed config lines (blank, comment, addn-hosts, dhcp-host, other).</param>
/// <param name="EffectiveHostsPathInFile">Path from addn-hosts= in the managed file, for display; empty when not set.</param>
public record ManagedConfigContent(IReadOnlyList<DnsmasqConfLine> Lines, string EffectiveHostsPathInFile);
