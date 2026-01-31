using DnsmasqWebUI.Models;

namespace DnsmasqWebUI.Services.Abstractions;

public interface IDnsmasqConfigService : IApplicationScopedService
{
    Task<IReadOnlyList<DhcpHostEntry>> ReadDhcpHostsAsync(CancellationToken ct = default);
    Task WriteDhcpHostsAsync(IReadOnlyList<DhcpHostEntry> entries, CancellationToken ct = default);
    Task<ManagedConfigContent> ReadManagedConfigAsync(CancellationToken ct = default);
    Task WriteManagedConfigAsync(IReadOnlyList<DnsmasqConfLine> lines, CancellationToken ct = default);
}

/// <summary>Full managed file content. EffectiveHostsPathInFile is parsed from AddnHosts line if present (for display only).</summary>
public record ManagedConfigContent(IReadOnlyList<DnsmasqConfLine> Lines, string EffectiveHostsPathInFile);
