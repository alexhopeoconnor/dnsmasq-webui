using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text.RegularExpressions;
using DnsmasqWebUI.Infrastructure.Services.EffectiveConfig.Metadata;
using DnsmasqWebUI.Infrastructure.Services.EffectiveConfig.Validation.Abstractions;
using DnsmasqWebUI.Models.Dnsmasq.EffectiveConfig;

namespace DnsmasqWebUI.Infrastructure.Services.EffectiveConfig.Validation.Rules;

public sealed class NoResolvWithoutUpstreamsRule : IEffectiveConfigCrossOptionRule
{
    public string Id => "resolver.no-resolv-without-upstreams";

    public IReadOnlyList<FieldIssue> Evaluate(EffectiveConfigCrossOptionContext context)
    {
        var noResolv = context.GetBool(DnsmasqConfKeys.NoResolv, cfg => cfg.NoResolv);
        if (!noResolv)
            return [];

        var servers = context.GetMulti(DnsmasqConfKeys.Server, cfg => cfg.ServerValues);
        var resolvFiles = context.GetMulti(DnsmasqConfKeys.ResolvFile, cfg => cfg.ResolvFiles);
        if (servers.Count > 0 || resolvFiles.Count > 0)
            return [];

        return
        [
            new FieldIssue(
                EffectiveConfigCrossOptionContext.FieldKeyForOption(DnsmasqConfKeys.Server),
                "With no-resolv, dnsmasq does not read /etc/resolv.conf. Add at least one server= or resolv-file= so upstream resolvers are defined; otherwise DNS forwarding may not work.",
                FieldIssueSeverity.Warning)
        ];
    }
}

public sealed class ConntrackWithQueryPortRule : IEffectiveConfigCrossOptionRule
{
    public string Id => "resolver.conntrack-query-port";

    public IReadOnlyList<FieldIssue> Evaluate(EffectiveConfigCrossOptionContext context)
    {
        var conntrack = context.GetBool(DnsmasqConfKeys.Conntrack, cfg => cfg.Conntrack);
        var queryPort = context.GetInt(DnsmasqConfKeys.QueryPort, cfg => cfg.QueryPort);
        if (!conntrack || queryPort is null)
            return [];

        return
        [
            new FieldIssue(
                EffectiveConfigCrossOptionContext.FieldKeyForOption(DnsmasqConfKeys.QueryPort),
                "dnsmasq does not allow query-port together with conntrack: conntrack copies Linux connection marks onto upstream DNS traffic, which needs the usual ephemeral source ports, not a fixed query port. Clear query-port (or set conntrack off) so the daemon can start.",
                FieldIssueSeverity.Error)
        ];
    }
}

public sealed class RebindExceptionsRequireStopDnsRebindRule : IEffectiveConfigCrossOptionRule
{
    public string Id => "resolver.rebind-exceptions-without-stop-dns-rebind";

    public IReadOnlyList<FieldIssue> Evaluate(EffectiveConfigCrossOptionContext context)
    {
        var stopDnsRebind = context.GetBool(DnsmasqConfKeys.StopDnsRebind, cfg => cfg.StopDnsRebind);
        if (stopDnsRebind)
            return [];

        var issues = new List<FieldIssue>();

        var localhostOk = context.GetBool(DnsmasqConfKeys.RebindLocalhostOk, cfg => cfg.RebindLocalhostOk);
        if (localhostOk)
        {
            issues.Add(new FieldIssue(
                EffectiveConfigCrossOptionContext.FieldKeyForOption(DnsmasqConfKeys.RebindLocalhostOk),
                "rebind-localhost-ok only applies when stop-dns-rebind is on. Enable stop-dns-rebind or remove this flag to avoid a misleading configuration.",
                FieldIssueSeverity.Warning));
        }

        var domainOk = context.GetMulti(DnsmasqConfKeys.RebindDomainOk, cfg => cfg.RebindDomainOkValues);
        if (domainOk.Count > 0)
        {
            issues.Add(new FieldIssue(
                EffectiveConfigCrossOptionContext.FieldKeyForOption(DnsmasqConfKeys.RebindDomainOk),
                "rebind-domain-ok only applies when stop-dns-rebind is on. Enable stop-dns-rebind or clear these exceptions so they are not silently ignored.",
                FieldIssueSeverity.Warning));
        }

        return issues;
    }
}

/// <summary>
/// Man: <c>bogus-priv</c> avoids forwarding RFC1918 (etc.) reverse lookups; combined with explicit private reverse
/// forwarding via <c>server=</c> or <c>rev-server=</c>, PTR traffic may not reach the configured upstream (subtle).
/// </summary>
public sealed class BogusPrivBlocksPrivateReverseServerRule : IEffectiveConfigCrossOptionRule
{
    public string Id => "resolver.bogus-priv-private-reverse-server";

    private const string PrivateReverseMessage =
        "bogus-priv takes priority over private reverse lookup forwarding, so those PTR queries may never reach the configured upstream server.";

    public IReadOnlyList<FieldIssue> Evaluate(EffectiveConfigCrossOptionContext context)
    {
        var bogusPriv = context.GetBool(DnsmasqConfKeys.BogusPriv, cfg => cfg.BogusPriv);
        if (!bogusPriv)
            return [];

        var servers = context.GetMulti(DnsmasqConfKeys.Server, cfg => cfg.ServerValues);
        var revServers = context.GetMulti(DnsmasqConfKeys.RevServer, cfg => cfg.RevServerValues);
        var serverMatch = servers.Any(LooksLikeRfc1918InAddrArpaServer);
        var revMatch = revServers.Any(IsRfc1918RevServerIpv4Target);

        if (!serverMatch && !revMatch)
            return [];

        var issues = new List<FieldIssue>();
        if (serverMatch)
        {
            issues.Add(new FieldIssue(
                EffectiveConfigCrossOptionContext.FieldKeyForOption(DnsmasqConfKeys.Server),
                PrivateReverseMessage,
                FieldIssueSeverity.Warning));
        }

        if (revMatch)
        {
            issues.Add(new FieldIssue(
                EffectiveConfigCrossOptionContext.FieldKeyForOption(DnsmasqConfKeys.RevServer),
                PrivateReverseMessage,
                FieldIssueSeverity.Warning));
        }

        return issues;
    }

    // RFC1918 reverse zone labels only; avoids substring false positives (e.g. 210, 192.0.0.x, 172.15).
    private static bool LooksLikeRfc1918InAddrArpaServer(string v)
    {
        if (string.IsNullOrEmpty(v) || !v.Contains("in-addr.arpa", StringComparison.OrdinalIgnoreCase))
            return false;

        const RegexOptions re = RegexOptions.IgnoreCase | RegexOptions.CultureInvariant;

        // 192.168.0.0/16 → *.168.192.in-addr.arpa
        if (Regex.IsMatch(v, @"(^|[/.])168\.192\.in-addr\.arpa", re))
            return true;

        // 10.0.0.0/8 → /10.in-addr.arpa/ or PTR ... .10.in-addr.arpa
        if (Regex.IsMatch(v, @"(^|[/.])10\.in-addr\.arpa", re))
            return true;

        // 172.16.0.0–172.31.255.255 → [16–31].172.in-addr.arpa
        if (Regex.IsMatch(v, @"(^|[/.])(1[6-9]|2[0-9]|3[01])\.172\.in-addr\.arpa", re))
            return true;

        return false;
    }

    /// <summary>True when the rev-server reverse target (IPv4 CIDR or host) overlaps RFC1918 space.</summary>
    private static bool IsRfc1918RevServerIpv4Target(string line)
    {
        if (string.IsNullOrWhiteSpace(line))
            return false;

        var target = line.Split(',', 2)[0].Trim();
        var slash = target.IndexOf('/');
        var ipText = slash >= 0 ? target[..slash] : target;
        if (!IPAddress.TryParse(ipText, out var ip) || ip.AddressFamily != AddressFamily.InterNetwork)
            return false;

        var prefix = slash < 0 ? 32 : int.TryParse(target[(slash + 1)..], out var p) ? p : -1;
        if (prefix is < 0 or > 32)
            return false;

        var ipUint = ToIpv4UInt32(ip);
        var mask = prefix == 0 ? 0u : uint.MaxValue << (32 - prefix);
        var netStart = ipUint & mask;
        var netEnd = netStart | ~mask;

        return OverlapsRfc1918Ipv4(netStart, netEnd);
    }

    private static uint ToIpv4UInt32(IPAddress ip)
    {
        var b = ip.GetAddressBytes();
        return ((uint)b[0] << 24) | ((uint)b[1] << 16) | ((uint)b[2] << 8) | b[3];
    }

    private static bool OverlapsRfc1918Ipv4(uint netStart, uint netEnd)
    {
        // 10.0.0.0/8, 172.16.0.0/12, 192.168.0.0/16
        (uint s, uint e)[] blocks =
        [
            (0x0A00_0000u, 0x0AFF_FFFFu),
            (0xAC10_0000u, 0xAC1F_FFFFu),
            (0xC0A8_0000u, 0xC0A8_FFFFu)
        ];

        foreach (var (bs, be) in blocks)
        {
            if (netStart <= be && bs <= netEnd)
                return true;
        }

        return false;
    }
}

/// <summary>
/// Man (2.80): <c>no-ping</c> with <c>dhcp-sequential-ip</c> breaks DHCP — documented regression/failure mode.
/// </summary>
public sealed class NoPingWithDhcpSequentialIpRule : IEffectiveConfigCrossOptionRule
{
    public string Id => "dhcp.no-ping-with-dhcp-sequential-ip";

    public IReadOnlyList<FieldIssue> Evaluate(EffectiveConfigCrossOptionContext context)
    {
        var noPing = context.GetBool(DnsmasqConfKeys.NoPing, cfg => cfg.NoPing);
        var sequential = context.GetBool(DnsmasqConfKeys.DhcpSequentialIp, cfg => cfg.DhcpSequentialIp);
        if (!noPing || !sequential)
            return [];

        return
        [
            new FieldIssue(
                EffectiveConfigCrossOptionContext.FieldKeyForOption(DnsmasqConfKeys.DhcpSequentialIp),
                "no-ping together with dhcp-sequential-ip is a known-broken combination in dnsmasq; disable one or the other.",
                FieldIssueSeverity.Warning)
        ];
    }
}

/// <summary>
/// Man: <c>port=0</c> disables the DNS listener; TFTP/PXE still expect a working setup — 2.87 fixed crashes for port 0 with netboot.
/// </summary>
public sealed class DnsPortZeroWithTftpOrPxeRule : IEffectiveConfigCrossOptionRule
{
    public string Id => "dns.port-zero-with-tftp-or-pxe";

    public IReadOnlyList<FieldIssue> Evaluate(EffectiveConfigCrossOptionContext context)
    {
        if (context.GetInt(DnsmasqConfKeys.Port, cfg => cfg.Port) != 0)
            return [];

        var tftp = context.GetBool(DnsmasqConfKeys.EnableTftp, cfg => cfg.EnableTftp);
        var pxe = context.GetMulti(DnsmasqConfKeys.PxeService, cfg => cfg.PxeServiceValues);
        if (!tftp && pxe.Count == 0)
            return [];

        return
        [
            new FieldIssue(
                EffectiveConfigCrossOptionContext.FieldKeyForOption(DnsmasqConfKeys.Port),
                "port=0 disables the DNS listener. With enable-tftp or pxe-service, netboot can fail or hit dnsmasq bugs fixed only in recent releases—use a non-zero port or turn off TFTP/PXE unless DNS is intentionally off.",
                FieldIssueSeverity.Warning)
        ];
    }
}

public sealed class DnssecPrerequisitesRule : IEffectiveConfigCrossOptionRule
{
    public string Id => "resolver.dnssec-prerequisites";

    public IReadOnlyList<FieldIssue> Evaluate(EffectiveConfigCrossOptionContext context)
    {
        var dnssec = context.GetBool(DnsmasqConfKeys.Dnssec, cfg => cfg.Dnssec);
        if (!dnssec)
            return [];

        var issues = new List<FieldIssue>();

        if (context.Status is { DnsmasqSupportsDnssec: false })
        {
            issues.Add(new FieldIssue(
                EffectiveConfigCrossOptionContext.FieldKeyForOption(DnsmasqConfKeys.Dnssec),
                "This dnsmasq binary does not report DNSSEC in its compile capabilities. Enabling dnssec will probably fail at startup; install a build with DNSSEC or turn dnssec off.",
                FieldIssueSeverity.Error));
        }

        var trustAnchors = context.GetMulti(DnsmasqConfKeys.TrustAnchor, cfg => cfg.TrustAnchorValues);
        if (trustAnchors.Count == 0)
        {
            issues.Add(new FieldIssue(
                EffectiveConfigCrossOptionContext.FieldKeyForOption(DnsmasqConfKeys.TrustAnchor),
                "dnssec is on but no trust-anchor entries are set, so validation cannot anchor the chain of trust. Add trust-anchor lines (or disable dnssec) for DNSSEC to be meaningful.",
                FieldIssueSeverity.Warning));
        }

        return issues;
    }
}

public sealed class ProxyDnssecCacheWarningRule : IEffectiveConfigCrossOptionRule
{
    public string Id => "resolver.proxy-dnssec-cache-warning";

    public IReadOnlyList<FieldIssue> Evaluate(EffectiveConfigCrossOptionContext context)
    {
        var proxyDnssec = context.GetBool(DnsmasqConfKeys.ProxyDnssec, cfg => cfg.ProxyDnssec);
        if (!proxyDnssec)
            return [];

        var cacheSize = context.GetInt(DnsmasqConfKeys.CacheSize, cfg => cfg.CacheSize);
        if (cacheSize == 0)
            return [];

        return
        [
            new FieldIssue(
                EffectiveConfigCrossOptionContext.FieldKeyForOption(DnsmasqConfKeys.CacheSize),
                "With proxy-dnssec, the dnsmasq docs recommend cache-size=0 if clients rely on the AD bit.",
                FieldIssueSeverity.Warning)
        ];
    }
}

public sealed class ConnmarkAllowlistEnableRequiresAllowlistRule : IEffectiveConfigCrossOptionRule
{
    public string Id => "resolver.connmark-allowlist-enable-requires-allowlist";

    public IReadOnlyList<FieldIssue> Evaluate(EffectiveConfigCrossOptionContext context)
    {
        if (!context.IsConnmarkAllowlistFilteringEnabled())
            return [];

        var allowlists = context.GetMulti(
            DnsmasqConfKeys.ConnmarkAllowlist,
            cfg => cfg.ConnmarkAllowlistValues);
        if (allowlists.Count > 0)
            return [];

        return
        [
            new FieldIssue(
                EffectiveConfigCrossOptionContext.FieldKeyForOption(DnsmasqConfKeys.ConnmarkAllowlist),
                "connmark-allowlist-enable is on but there are no connmark-allowlist= rules, so dnsmasq may refuse DNS for marked connections. Add at least one allowlist rule or disable connmark-allowlist-enable.",
                FieldIssueSeverity.Error)
        ];
    }
}

public sealed class QueryPortIgnoredForSourceBoundServerRule : IEffectiveConfigCrossOptionRule
{
    public string Id => "resolver.query-port-ignored-source-bound-server";

    public IReadOnlyList<FieldIssue> Evaluate(EffectiveConfigCrossOptionContext context)
    {
        if (context.GetInt(DnsmasqConfKeys.QueryPort, cfg => cfg.QueryPort) is null)
            return [];

        var servers = context.GetMulti(DnsmasqConfKeys.Server, cfg => cfg.ServerValues);
        if (!servers.Any(s => s.Contains('@', StringComparison.Ordinal)))
            return [];

        return
        [
            new FieldIssue(
                EffectiveConfigCrossOptionContext.FieldKeyForOption(DnsmasqConfKeys.QueryPort),
                "query-port does not apply to server= lines that bind a source address or interface (the part after @). Those queries still use ephemeral ports; remove query-port or adjust server bindings if you expected a fixed source port everywhere.",
                FieldIssueSeverity.Warning)
        ];
    }
}

public sealed class AddSubnetCacheBehaviorRule : IEffectiveConfigCrossOptionRule
{
    public string Id => "resolver.add-subnet-cache-behavior";

    public IReadOnlyList<FieldIssue> Evaluate(EffectiveConfigCrossOptionContext context)
    {
        var addSubnet = context.GetString(DnsmasqConfKeys.AddSubnet, cfg => cfg.AddSubnet);
        if (string.IsNullOrWhiteSpace(addSubnet))
            return [];

        return
        [
            new FieldIssue(
                EffectiveConfigCrossOptionContext.FieldKeyForOption(DnsmasqConfKeys.AddSubnet),
                "add-subnet can disable caching for replies that vary by client subnet unless the forwarded subnet is constant.",
                FieldIssueSeverity.Warning)
        ];
    }
}

public sealed class Filterwin2kSrvWarningRule : IEffectiveConfigCrossOptionRule
{
    public string Id => "resolver.filterwin2k-srv-warning";

    public IReadOnlyList<FieldIssue> Evaluate(EffectiveConfigCrossOptionContext context)
    {
        var enabled = context.GetBool(DnsmasqConfKeys.Filterwin2k, cfg => cfg.Filterwin2k);
        if (!enabled)
            return [];

        return
        [
            new FieldIssue(
                EffectiveConfigCrossOptionContext.FieldKeyForOption(DnsmasqConfKeys.Filterwin2k),
                "filterwin2k blocks SRV queries and can break Kerberos, SIP, XMPP, or similar service discovery.",
                FieldIssueSeverity.Warning)
        ];
    }
}

/// <summary>
/// Man: from 2.86, <c>address=/domain/IP</c> applies only to A/AAAA; other types forward unless <c>local=/domain/</c>
/// restores earlier “whole domain” behavior. Precedence among <c>address</c>, <c>server</c>, and defaults was refined through 2.92.
/// </summary>
public sealed class AddressLocalDnsmasq286CompatibilityRule : IEffectiveConfigCrossOptionRule
{
    public string Id => "resolver.address-local-dnsmasq-286-compat";

    public IReadOnlyList<FieldIssue> Evaluate(EffectiveConfigCrossOptionContext context)
    {
        var addresses = context.GetMulti(DnsmasqConfKeys.Address, cfg => cfg.AddressValues);
        if (addresses.Count == 0)
            return [];

        var addressDomains = addresses
            .SelectMany(GetDomainsFromAddressDirective)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        if (addressDomains.Count == 0)
            return [];

        var localDomains = context.GetMulti(DnsmasqConfKeys.Local, cfg => cfg.LocalValues)
            .SelectMany(GetDomainsFromLocalDirective)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var uncovered = addressDomains.Where(d => !localDomains.Contains(d)).ToList();
        if (uncovered.Count == 0)
            return [];

        var preview = string.Join(", ", uncovered.Take(3));
        var extra = uncovered.Count > 3 ? ", ..." : "";
        return
        [
            new FieldIssue(
                EffectiveConfigCrossOptionContext.FieldKeyForOption(DnsmasqConfKeys.Address),
                $"Since dnsmasq 2.86, address= with a domain and IP answers only A/AAAA locally; other RR types are forwarded unless the domain is also covered by local= (see the dnsmasq man page). Add matching local= for: {preview}{extra}.",
                FieldIssueSeverity.Warning)
        ];
    }

    private static IReadOnlyList<string> GetDomainsFromAddressDirective(string value)
    {
        var t = value.Trim();
        if (!t.StartsWith('/'))
            return [];
        var end = t.LastIndexOf('/');
        if (end <= 1)
            return [];

        var domainsPart = t[1..end];
        return domainsPart
            .Split('/', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
            .Where(d => !string.Equals(d, "#", StringComparison.Ordinal))
            .ToList();
    }

    private static IReadOnlyList<string> GetDomainsFromLocalDirective(string value)
    {
        var t = value.Trim();
        if (!t.StartsWith('/') || !t.EndsWith('/') || t.Length < 3)
            return [];

        var domainsPart = t[1..^1];
        return domainsPart
            .Split('/', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
            .Where(d => !string.Equals(d, "#", StringComparison.Ordinal))
            .ToList();
    }
}

public sealed class DnssecCheckUnsignedRequiresDnssecRule : IEffectiveConfigCrossOptionRule
{
    public string Id => "resolver.dnssec-check-unsigned-requires-dnssec";

    public IReadOnlyList<FieldIssue> Evaluate(EffectiveConfigCrossOptionContext context)
    {
        var dnssecCheckUnsigned = context.GetString(DnsmasqConfKeys.DnssecCheckUnsigned, cfg => cfg.DnssecCheckUnsigned);
        if (dnssecCheckUnsigned is null)
            return [];

        if (context.GetBool(DnsmasqConfKeys.Dnssec, cfg => cfg.Dnssec))
            return [];

        return
        [
            new FieldIssue(
                EffectiveConfigCrossOptionContext.FieldKeyForOption(DnsmasqConfKeys.DnssecCheckUnsigned),
                "dnssec-check-unsigned only has effect when dnssec is enabled. Enable dnssec or remove dnssec-check-unsigned to avoid a no-op setting.",
                FieldIssueSeverity.Warning)
        ];
    }
}

public sealed class LocalServiceIgnoredByBindSettingsRule : IEffectiveConfigCrossOptionRule
{
    public string Id => "resolver.local-service-ignored-by-bind-settings";

    public IReadOnlyList<FieldIssue> Evaluate(EffectiveConfigCrossOptionContext context)
    {
        if (context.GetString(DnsmasqConfKeys.LocalService, cfg => cfg.LocalService) is null)
            return [];

        var hasInterface = context.GetMulti(DnsmasqConfKeys.Interface, cfg => cfg.Interfaces).Count > 0;
        var hasExceptInterface = context.GetMulti(DnsmasqConfKeys.ExceptInterface, cfg => cfg.ExceptInterfaces).Count > 0;
        var hasListenAddress = context.GetMulti(DnsmasqConfKeys.ListenAddress, cfg => cfg.ListenAddresses).Count > 0;
        var hasAuthServer = context.GetMulti(DnsmasqConfKeys.AuthServer, cfg => cfg.AuthServerValues).Count > 0;

        if (!hasInterface && !hasExceptInterface && !hasListenAddress && !hasAuthServer)
            return [];

        return
        [
            new FieldIssue(
                EffectiveConfigCrossOptionContext.FieldKeyForOption(DnsmasqConfKeys.LocalService),
                "local-service is ignored when interface, except-interface, listen-address, or auth-server is configured. Remove local-service or rely on explicit bind/listen settings.",
                FieldIssueSeverity.Warning)
        ];
    }
}

/*
 * Deferred cross-option: dhcp-authoritative vs DHCPv6 REBIND-without-lease (dnsmasq 2.92 changelog).
 * Implement only when either:
 *   (1) the man page states a configuration-level requirement to set dhcp-authoritative for the affected DHCPv6 mode, or
 *   (2) this codebase can reliably detect stateful DHCPv6 (vs SLAAC-only) from DhcpRanges / RA flags.
 * Otherwise prefer tooltips or external docs, not a static warning.
 */
