using DnsmasqWebUI.Models.Config;

namespace DnsmasqWebUI.Models.Contracts;

/// <summary>Full managed file content. EffectiveHostsPathInFile is parsed from AddnHosts line if present (for display only).</summary>
public record ManagedConfigContent(IReadOnlyList<DnsmasqConfLine> Lines, string EffectiveHostsPathInFile);
