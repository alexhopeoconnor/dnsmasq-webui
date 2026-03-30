using System.Linq;
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

public sealed class BogusPrivBlocksPrivateReverseServerRule : IEffectiveConfigCrossOptionRule
{
    public string Id => "resolver.bogus-priv-private-reverse-server";

    public IReadOnlyList<FieldIssue> Evaluate(EffectiveConfigCrossOptionContext context)
    {
        var bogusPriv = context.GetBool(DnsmasqConfKeys.BogusPriv, cfg => cfg.BogusPriv);
        if (!bogusPriv)
            return [];

        var servers = context.GetMulti(DnsmasqConfKeys.Server, cfg => cfg.ServerValues);
        var hasPrivateReverseServer = servers.Any(LooksLikeRfc1918InAddrArpaServer);

        if (!hasPrivateReverseServer)
            return [];

        return
        [
            new FieldIssue(
                EffectiveConfigCrossOptionContext.FieldKeyForOption(DnsmasqConfKeys.Server),
                "bogus-priv takes priority over private reverse lookup forwarding, so those PTR queries may never reach the configured upstream server.",
                FieldIssueSeverity.Warning)
        ];
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
                $"From dnsmasq 2.86, address= with a domain and IP may forward non-A/AAAA queries upstream unless the domain is also covered by local=. Add matching local= entries for: {preview}{extra}.",
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
