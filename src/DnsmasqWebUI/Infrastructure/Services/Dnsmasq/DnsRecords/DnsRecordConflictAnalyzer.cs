using System.Linq;
using DnsmasqWebUI.Models.Dnsmasq.EffectiveConfig;
using DnsmasqWebUI.Models.DnsRecords;

namespace DnsmasqWebUI.Infrastructure.Services.Dnsmasq.DnsRecords;

/// <summary>Cross-row DNS record warnings (page-local; not global effective-config rules).</summary>
public sealed class DnsRecordConflictAnalyzer
{
    public IReadOnlyDictionary<string, IReadOnlyList<DnsRecordIssue>> Analyze(IReadOnlyList<DnsRecordRow> rows)
    {
        var byId = new Dictionary<string, List<DnsRecordIssue>>(StringComparer.Ordinal);

        void Add(string id, DnsRecordIssue issue)
        {
            if (!byId.TryGetValue(id, out var list))
            {
                list = [];
                byId[id] = list;
            }

            list.Add(issue);
        }

        // Duplicate exact lines per option
        foreach (var g in rows.GroupBy((DnsRecordRow r) => $"{r.OptionName}\u0001{r.RawValue}", StringComparer.Ordinal))
        {
            if (g.Count() <= 1 || string.IsNullOrWhiteSpace(g.First().RawValue))
                continue;
            foreach (var row in g)
            {
                Add(row.Id, new DnsRecordIssue(
                    $"Duplicate identical {row.OptionName} entry ({g.Count()}×).",
                    FieldIssueSeverity.Warning));
            }
        }

        // PTR duplicate reverse names
        foreach (var g in rows.Where(r => r.Payload is PtrPayload).GroupBy(r => ((PtrPayload)r.Payload).Name, StringComparer.OrdinalIgnoreCase))
        {
            if (g.Count() <= 1 || string.IsNullOrWhiteSpace(g.Key))
                continue;
            foreach (var row in g)
            {
                Add(row.Id, new DnsRecordIssue(
                    $"Multiple ptr-record lines for name '{g.Key}'.",
                    FieldIssueSeverity.Warning));
            }
        }

        // SRV duplicate (service, target, port)
        foreach (var g in rows.Where(r => r.Payload is SrvPayload).GroupBy(r =>
        {
            var p = (SrvPayload)r.Payload;
            return (p.ServiceName, p.Target ?? "", p.Port ?? 0);
        }))
        {
            if (g.Count() <= 1 || string.IsNullOrWhiteSpace(((SrvPayload)g.First().Payload).ServiceName))
                continue;
            foreach (var row in g)
            {
                Add(row.Id, new DnsRecordIssue(
                    "Duplicate srv-host with the same service, target, and port.",
                    FieldIssueSeverity.Warning));
            }
        }

        // host-record duplicate owner names (same option)
        foreach (var g in rows.Where(r => r.Payload is HostRecordPayload).SelectMany(r =>
                 ((HostRecordPayload)r.Payload).Owners.Select(o => (Owner: o, Row: r)))
             .GroupBy(x => x.Owner, StringComparer.OrdinalIgnoreCase))
        {
            if (g.Count() <= 1 || string.IsNullOrWhiteSpace(g.Key))
                continue;
            foreach (var x in g)
            {
                Add(x.Row.Id, new DnsRecordIssue(
                    $"Owner name '{g.Key}' appears in multiple host-record lines.",
                    FieldIssueSeverity.Warning));
            }
        }

        var cnameAliases = rows
            .Where(r => r.Payload is CnamePayload)
            .SelectMany(r => ((CnamePayload)r.Payload).Aliases.Select(a => (Alias: a, Row: r)))
            .Where(x => !string.IsNullOrWhiteSpace(x.Alias))
            .ToList();

        var hostOwnerSet = rows
            .Where(r => r.Payload is HostRecordPayload)
            .SelectMany(r => ((HostRecordPayload)r.Payload).Owners)
            .Where(o => !string.IsNullOrWhiteSpace(o))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var mxNames = rows
            .Where(r => r.Payload is MxPayload)
            .Select(r => (((MxPayload)r.Payload).Domain, r))
            .Where(x => !string.IsNullOrWhiteSpace(x.Domain))
            .ToList();

        var txtNameSet = rows
            .Where(r => r.Payload is TxtPayload)
            .Select(r => ((TxtPayload)r.Payload).Name)
            .Where(n => !string.IsNullOrWhiteSpace(n))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var srvServiceSet = rows
            .Where(r => r.Payload is SrvPayload)
            .Select(r => ((SrvPayload)r.Payload).ServiceName)
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var c in cnameAliases)
        {
            if (hostOwnerSet.Contains(c.Alias))
            {
                Add(c.Row.Id, new DnsRecordIssue(
                    $"'{c.Alias}' is both a CNAME alias and a host-record owner (often invalid together).",
                    FieldIssueSeverity.Warning));
            }

            if (mxNames.Any(m => string.Equals(m.Domain, c.Alias, StringComparison.OrdinalIgnoreCase)))
            {
                Add(c.Row.Id, new DnsRecordIssue(
                    $"'{c.Alias}' is both a CNAME alias and an mx-host domain.",
                    FieldIssueSeverity.Warning));
            }

            if (txtNameSet.Contains(c.Alias))
            {
                Add(c.Row.Id, new DnsRecordIssue(
                    $"'{c.Alias}' is both a CNAME alias and a txt-record name.",
                    FieldIssueSeverity.Warning));
            }

            if (srvServiceSet.Contains(c.Alias))
            {
                Add(c.Row.Id, new DnsRecordIssue(
                    $"'{c.Alias}' matches an srv-host service name (unusual coexistence).",
                    FieldIssueSeverity.Warning));
            }
        }

        // Incomplete rows
        foreach (var row in rows)
        {
            switch (row.Payload)
            {
                case PtrPayload p when string.IsNullOrWhiteSpace(p.Target):
                    Add(row.Id, new DnsRecordIssue("ptr-record has no target (may be intentional).", FieldIssueSeverity.Warning));
                    break;
                case SrvPayload p when string.IsNullOrWhiteSpace(p.Target):
                    Add(row.Id, new DnsRecordIssue("srv-host has no target (local-only SRV).", FieldIssueSeverity.Warning));
                    break;
                case MxPayload p when string.IsNullOrWhiteSpace(p.Hostname) && p.Preference is null:
                    Add(row.Id, new DnsRecordIssue("mx-host has only a domain; mail host and preference are unset.", FieldIssueSeverity.Warning));
                    break;
            }
        }

        return byId.ToDictionary(kv => kv.Key, kv => (IReadOnlyList<DnsRecordIssue>)kv.Value, StringComparer.Ordinal);
    }
}
