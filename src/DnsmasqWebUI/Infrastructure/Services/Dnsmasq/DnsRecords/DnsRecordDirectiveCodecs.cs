using System.Globalization;
using System.Linq;
using System.Net;
using System.Text;
using DnsmasqWebUI.Infrastructure.Services.Dnsmasq.DnsRecords.Abstractions;
using DnsmasqWebUI.Infrastructure.Services.EffectiveConfig.Metadata;
using DnsmasqWebUI.Models.Dnsmasq.EffectiveConfig;
using DnsmasqWebUI.Models.DnsRecords;

namespace DnsmasqWebUI.Infrastructure.Services.Dnsmasq.DnsRecords;

internal static class DnsRecordCodecHelpers
{
    public static DnsRecordRow BaseRow(
        string optionName,
        DnsRecordFamily family,
        int index,
        string raw,
        ConfigValueSource? source,
        DnsRecordPayload payload,
        string summary) =>
        new(
            Id: $"{optionName}:{index}",
            OccurrenceId: $"{optionName}:{index}",
            OptionName: optionName,
            Family: family,
            IndexInOption: index,
            RawValue: raw,
            Source: source,
            SourcePath: source?.FilePath,
            SourceLabel: source?.FileName,
            IsDraftOnly: false,
            IsEditable: source?.IsReadOnly != true,
            Payload: payload,
            Issues: [],
            Summary: summary);
}

internal sealed class CnameRecordCodec : IDnsRecordDirectiveCodec
{
    public string OptionName => DnsmasqConfKeys.Cname;
    public DnsRecordFamily Family => DnsRecordFamily.Cname;

    public DnsRecordRow Parse(ValueWithSource sourceValue, int indexInOption)
    {
        var parts = sourceValue.Value.Split(',', StringSplitOptions.TrimEntries);
        var lastIsTtl = parts.Length >= 3 &&
            int.TryParse(parts[^1], NumberStyles.Integer, CultureInfo.InvariantCulture, out var ttl) &&
            ttl > 0;
        var targetIndex = lastIsTtl ? parts.Length - 2 : parts.Length - 1;
        int? ttlOpt = lastIsTtl ? int.Parse(parts[^1], CultureInfo.InvariantCulture) : null;
        var aliases = targetIndex >= 1 ? parts.Take(targetIndex).ToArray() : Array.Empty<string>();
        var target = targetIndex >= 0 && targetIndex < parts.Length ? parts[targetIndex] : "";
        var ttlSuffix = ttlOpt.HasValue ? $" (TTL {ttlOpt.Value})" : "";
        var summary = aliases.Length > 0
            ? $"{string.Join(", ", aliases)} → {target}{ttlSuffix}"
            : sourceValue.Value;
        return DnsRecordCodecHelpers.BaseRow(OptionName, Family, indexInOption, sourceValue.Value, sourceValue.Source,
            new CnamePayload(aliases, target, ttlOpt), summary);
    }

    public string Serialize(DnsRecordRow row)
    {
        var p = (CnamePayload)row.Payload;
        var list = p.Aliases.Append(p.Target).ToList();
        if (p.Ttl is int t)
            list.Add(t.ToString(CultureInfo.InvariantCulture));
        return string.Join(",", list);
    }
}

internal sealed class HostRecordCodec : IDnsRecordDirectiveCodec
{
    public string OptionName => DnsmasqConfKeys.HostRecord;
    public DnsRecordFamily Family => DnsRecordFamily.HostRecord;

    public DnsRecordRow Parse(ValueWithSource sourceValue, int indexInOption)
    {
        var parts = sourceValue.Value.Split(',', StringSplitOptions.TrimEntries);
        if (parts.Length == 0)
            return DnsRecordCodecHelpers.BaseRow(OptionName, Family, indexInOption, sourceValue.Value, sourceValue.Source,
                new HostRecordPayload([], null, null, null), sourceValue.Value);

        var lastIsTtl = parts.Length >= 2 &&
            int.TryParse(parts[^1], NumberStyles.Integer, CultureInfo.InvariantCulture, out var ttl) &&
            ttl > 0;
        var end = lastIsTtl ? parts.Length - 2 : parts.Length - 1;
        int? ttlOpt = lastIsTtl ? int.Parse(parts[^1], CultureInfo.InvariantCulture) : null;

        var owners = new List<string> { parts[0] };
        string? v4 = null;
        string? v6 = null;
        for (var i = 1; i <= end; i++)
        {
            var t = parts[i];
            if (IPAddress.TryParse(t, out var ip))
            {
                if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                    v4 = t;
                else
                    v6 = t;
            }
            else
            {
                owners.Add(t);
            }
        }

        var summary = new StringBuilder();
        summary.Append(string.Join(", ", owners));
        if (v4 != null) summary.Append(" → ").Append(v4);
        if (v6 != null) summary.Append(v4 != null ? ", " : " → ").Append(v6);
        if (ttlOpt is { } x) summary.Append($" (TTL {x})");

        return DnsRecordCodecHelpers.BaseRow(OptionName, Family, indexInOption, sourceValue.Value, sourceValue.Source,
            new HostRecordPayload(owners, v4, v6, ttlOpt), summary.Length > 0 ? summary.ToString() : sourceValue.Value);
    }

    public string Serialize(DnsRecordRow row)
    {
        var p = (HostRecordPayload)row.Payload;
        var list = p.Owners.ToList();
        if (!string.IsNullOrEmpty(p.IPv4))
            list.Add(p.IPv4!);
        if (!string.IsNullOrEmpty(p.IPv6))
            list.Add(p.IPv6!);
        if (p.Ttl is int t)
            list.Add(t.ToString(CultureInfo.InvariantCulture));
        return string.Join(",", list);
    }
}

internal sealed class TxtRecordCodec : IDnsRecordDirectiveCodec
{
    public string OptionName => DnsmasqConfKeys.TxtRecord;
    public DnsRecordFamily Family => DnsRecordFamily.Txt;

    public DnsRecordRow Parse(ValueWithSource sourceValue, int indexInOption)
    {
        var s = sourceValue.Value.Trim();
        string name;
        string? text;
        if (!s.Contains(','))
        {
            name = s;
            text = null;
        }
        else
        {
            var idx = s.IndexOf(',');
            name = s[..idx].Trim();
            text = s[(idx + 1)..].Trim();
            if (text.Length == 0)
                text = null;
        }

        var summary = text != null ? $"{name}: {text}" : name;
        return DnsRecordCodecHelpers.BaseRow(OptionName, Family, indexInOption, sourceValue.Value, sourceValue.Source,
            new TxtPayload(name, text), summary);
    }

    public string Serialize(DnsRecordRow row)
    {
        var p = (TxtPayload)row.Payload;
        if (string.IsNullOrEmpty(p.Text))
            return p.Name;
        return $"{p.Name},{p.Text}";
    }
}

internal sealed class PtrRecordCodec : IDnsRecordDirectiveCodec
{
    public string OptionName => DnsmasqConfKeys.PtrRecord;
    public DnsRecordFamily Family => DnsRecordFamily.Ptr;

    public DnsRecordRow Parse(ValueWithSource sourceValue, int indexInOption)
    {
        var parts = sourceValue.Value.Split(',', StringSplitOptions.TrimEntries);
        var name = parts.Length > 0 ? parts[0] : "";
        var target = parts.Length > 1 ? parts[1] : null;
        var summary = target != null ? $"{name} → {target}" : name;
        return DnsRecordCodecHelpers.BaseRow(OptionName, Family, indexInOption, sourceValue.Value, sourceValue.Source,
            new PtrPayload(name, target), summary);
    }

    public string Serialize(DnsRecordRow row)
    {
        var p = (PtrPayload)row.Payload;
        return string.IsNullOrEmpty(p.Target) ? p.Name : $"{p.Name},{p.Target}";
    }
}

internal sealed class MxHostRecordCodec : IDnsRecordDirectiveCodec
{
    public string OptionName => DnsmasqConfKeys.MxHost;
    public DnsRecordFamily Family => DnsRecordFamily.Mx;

    public DnsRecordRow Parse(ValueWithSource sourceValue, int indexInOption)
    {
        var parts = sourceValue.Value.Split(',', StringSplitOptions.TrimEntries);
        if (parts.Length == 0)
            return DnsRecordCodecHelpers.BaseRow(OptionName, Family, indexInOption, sourceValue.Value, sourceValue.Source,
                new MxPayload("", null, null), sourceValue.Value);

        var domain = parts[0];
        string? host = null;
        int? pref = null;
        if (parts.Length == 2)
        {
            if (int.TryParse(parts[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out var p) && p is >= 0 and <= 65535)
                pref = p;
            else
                host = parts[1];
        }
        else if (parts.Length >= 3)
        {
            host = parts[1];
            if (int.TryParse(parts[2], NumberStyles.Integer, CultureInfo.InvariantCulture, out var p) && p is >= 0 and <= 65535)
                pref = p;
        }

        var summary = pref is { } pr
            ? $"{domain} → {host ?? "(default)"} (preference {pr})"
            : host != null
                ? $"{domain} → {host}"
                : domain;

        return DnsRecordCodecHelpers.BaseRow(OptionName, Family, indexInOption, sourceValue.Value, sourceValue.Source,
            new MxPayload(domain, host, pref), summary);
    }

    public string Serialize(DnsRecordRow row)
    {
        var p = (MxPayload)row.Payload;
        if (p.Hostname == null && p.Preference == null)
            return p.Domain;
        if (p.Preference is { } pr && p.Hostname != null)
            return $"{p.Domain},{p.Hostname},{pr}";
        if (p.Preference is { } pr2)
            return $"{p.Domain},{pr2}";
        return $"{p.Domain},{p.Hostname}";
    }
}

internal sealed class SrvRecordCodec : IDnsRecordDirectiveCodec
{
    public string OptionName => DnsmasqConfKeys.Srv;
    public DnsRecordFamily Family => DnsRecordFamily.Srv;

    public DnsRecordRow Parse(ValueWithSource sourceValue, int indexInOption)
    {
        var parts = sourceValue.Value.Split(',', StringSplitOptions.TrimEntries);
        var svc = parts.Length > 0 ? parts[0] : "";
        string? target = parts.Length >= 2 ? parts[1] : null;
        if (string.IsNullOrEmpty(target))
            target = null;
        int? port = parts.Length >= 3 && int.TryParse(parts[2], out var po) ? po : null;
        int? pri = parts.Length >= 4 && int.TryParse(parts[3], out var p) ? p : null;
        int? w = parts.Length >= 5 && int.TryParse(parts[4], out var ww) ? ww : null;

        var sb = new StringBuilder();
        sb.Append("Serve ").Append(svc);
        if (!string.IsNullOrEmpty(target))
            sb.Append(" at ").Append(target);
        if (port is { } pt)
            sb.Append(':').Append(pt);
        if (pri is { } pv || w is { } ww2)
            sb.Append(" (priority ").Append(pri?.ToString() ?? "0")
                .Append(", weight ").Append(w?.ToString() ?? "0").Append(')');

        return DnsRecordCodecHelpers.BaseRow(OptionName, Family, indexInOption, sourceValue.Value, sourceValue.Source,
            new SrvPayload(svc, target, port, pri, w), sb.ToString());
    }

    public string Serialize(DnsRecordRow row)
    {
        var p = (SrvPayload)row.Payload;
        // srv-host: service[,target[,port[,priority[,weight]]]] — do not pad omitted numerics with 0 (breaks round-trip).
        if (string.IsNullOrEmpty(p.Target) && p.Port is null && p.Priority is null && p.Weight is null)
            return p.ServiceName;

        var parts = new List<string> { p.ServiceName, p.Target ?? "" };
        if (p.Port is null && p.Priority is null && p.Weight is null)
            return string.Join(",", parts);

        parts.Add((p.Port ?? 0).ToString(CultureInfo.InvariantCulture));
        if (p.Priority is null && p.Weight is null)
            return string.Join(",", parts);

        parts.Add((p.Priority ?? 0).ToString(CultureInfo.InvariantCulture));
        if (p.Weight is null)
            return string.Join(",", parts);

        parts.Add(p.Weight.Value.ToString(CultureInfo.InvariantCulture));
        return string.Join(",", parts);
    }
}

internal sealed class NaptrRecordCodec : IDnsRecordDirectiveCodec
{
    public string OptionName => DnsmasqConfKeys.NaptrRecord;
    public DnsRecordFamily Family => DnsRecordFamily.Naptr;

    public DnsRecordRow Parse(ValueWithSource sourceValue, int indexInOption)
    {
        var parts = sourceValue.Value.Split(',', StringSplitOptions.TrimEntries);
        var name = parts.Length > 0 ? parts[0] : "";
        var repl = parts.Length == 7 ? parts[6] : null;
        var summary = parts.Length >= 6
            ? $"For {name}, order {parts[1]} pref {parts[2]}, service {parts[4]}"
            : sourceValue.Value;
        return DnsRecordCodecHelpers.BaseRow(OptionName, Family, indexInOption, sourceValue.Value, sourceValue.Source,
            new NaptrPayload(
                name,
                parts.Length > 1 ? parts[1] : "",
                parts.Length > 2 ? parts[2] : "",
                parts.Length > 3 ? parts[3] : "",
                parts.Length > 4 ? parts[4] : "",
                parts.Length > 5 ? parts[5] : "",
                string.IsNullOrEmpty(repl) ? null : repl),
            summary);
    }

    public string Serialize(DnsRecordRow row)
    {
        var p = (NaptrPayload)row.Payload;
        var parts = new List<string>
        {
            p.Name, p.Order, p.Preference, p.Flags, p.Service, p.Regexp
        };
        if (!string.IsNullOrEmpty(p.Replacement))
            parts.Add(p.Replacement!);
        return string.Join(",", parts);
    }
}

internal sealed class CaaRecordCodec : IDnsRecordDirectiveCodec
{
    public string OptionName => DnsmasqConfKeys.CaaRecord;
    public DnsRecordFamily Family => DnsRecordFamily.Caa;

    public DnsRecordRow Parse(ValueWithSource sourceValue, int indexInOption)
    {
        var parts = sourceValue.Value.Split(',', StringSplitOptions.TrimEntries);
        var name = parts.Length > 0 ? parts[0] : "";
        var flags = parts.Length > 1 ? parts[1] : "";
        var tag = parts.Length > 2 ? parts[2] : "";
        var val = parts.Length > 3 ? parts[3] : "";
        var summary = $"CAA for {name}: {tag} = {val} (flags {flags})";
        return DnsRecordCodecHelpers.BaseRow(OptionName, Family, indexInOption, sourceValue.Value, sourceValue.Source,
            new CaaPayload(name, flags, tag, val), summary);
    }

    public string Serialize(DnsRecordRow row)
    {
        var p = (CaaPayload)row.Payload;
        return $"{p.Name},{p.Flags},{p.Tag},{p.Value}";
    }
}

internal sealed class DnsRrRecordCodec : IDnsRecordDirectiveCodec
{
    public string OptionName => DnsmasqConfKeys.DnsRr;
    public DnsRecordFamily Family => DnsRecordFamily.DnsRr;

    public DnsRecordRow Parse(ValueWithSource sourceValue, int indexInOption)
    {
        var parts = sourceValue.Value.Split(',', 3, StringSplitOptions.TrimEntries);
        var name = parts.Length > 0 ? parts[0] : "";
        var num = parts.Length > 1 ? parts[1] : "";
        var hex = parts.Length > 2 ? parts[2] : null;
        var summary = $"RR {name} type {num}" + (string.IsNullOrEmpty(hex) ? "" : " + data");
        return DnsRecordCodecHelpers.BaseRow(OptionName, Family, indexInOption, sourceValue.Value, sourceValue.Source,
            new DnsRrPayload(name, num, string.IsNullOrEmpty(hex) ? null : hex), summary);
    }

    public string Serialize(DnsRecordRow row)
    {
        var p = (DnsRrPayload)row.Payload;
        return string.IsNullOrEmpty(p.HexData) ? $"{p.Name},{p.RrNumber}" : $"{p.Name},{p.RrNumber},{p.HexData}";
    }
}

internal sealed class DynamicHostRecordCodec : IDnsRecordDirectiveCodec
{
    public string OptionName => DnsmasqConfKeys.DynamicHost;
    public DnsRecordFamily Family => DnsRecordFamily.DynamicHost;

    public DnsRecordRow Parse(ValueWithSource sourceValue, int indexInOption)
    {
        var parts = sourceValue.Value.Split(',', StringSplitOptions.TrimEntries);
        string name = "", iface = "";
        string? v4 = null, v6 = null;
        if (parts.Length == 3)
        {
            name = parts[0];
            v4 = string.IsNullOrEmpty(parts[1]) ? null : parts[1];
            iface = parts[2];
        }
        else if (parts.Length >= 4)
        {
            name = parts[0];
            v4 = string.IsNullOrEmpty(parts[1]) ? null : parts[1];
            v6 = string.IsNullOrEmpty(parts[2]) ? null : parts[2];
            iface = parts[3];
        }

        var summary = $"Dynamic {name} on {iface}" + (v4 != null ? $" IPv4 {v4}" : "") + (v6 != null ? $" IPv6 {v6}" : "");
        return DnsRecordCodecHelpers.BaseRow(OptionName, Family, indexInOption, sourceValue.Value, sourceValue.Source,
            new DynamicHostPayload(name, v4, v6, iface), summary);
    }

    public string Serialize(DnsRecordRow row)
    {
        var p = (DynamicHostPayload)row.Payload;
        if (p.IPv6 != null)
            return $"{p.Name},{p.IPv4 ?? ""},{p.IPv6},{p.Interface}";
        return $"{p.Name},{p.IPv4 ?? ""},{p.Interface}";
    }
}

internal sealed class InterfaceNameRecordCodec : IDnsRecordDirectiveCodec
{
    public string OptionName => DnsmasqConfKeys.InterfaceName;
    public DnsRecordFamily Family => DnsRecordFamily.InterfaceName;

    public DnsRecordRow Parse(ValueWithSource sourceValue, int indexInOption)
    {
        var parts = sourceValue.Value.Split(',', StringSplitOptions.TrimEntries);
        var dns = parts.Length > 0 ? parts[0] : "";
        var iface = parts.Length > 1 ? parts[1] : "";
        var summary = $"{dns} bound to interface {iface}";
        return DnsRecordCodecHelpers.BaseRow(OptionName, Family, indexInOption, sourceValue.Value, sourceValue.Source,
            new InterfaceNamePayload(dns, iface), summary);
    }

    public string Serialize(DnsRecordRow row)
    {
        var p = (InterfaceNamePayload)row.Payload;
        return $"{p.DnsName},{p.InterfaceSpec}";
    }
}

internal sealed class SynthDomainRecordCodec : IDnsRecordDirectiveCodec
{
    public string OptionName => DnsmasqConfKeys.SynthDomain;
    public DnsRecordFamily Family => DnsRecordFamily.SynthDomain;

    public DnsRecordRow Parse(ValueWithSource sourceValue, int indexInOption)
    {
        var parts = sourceValue.Value.Split(',', StringSplitOptions.TrimEntries);
        var domain = parts.Length > 0 ? parts[0] : "";
        var range = parts.Length > 1 ? parts[1] : "";
        var prefix = parts.Length > 2 ? parts[2] : null;
        var summary = $"Synthesize names under {domain} for {range}" +
                      (!string.IsNullOrEmpty(prefix) ? $" with prefix {prefix}" : "");
        return DnsRecordCodecHelpers.BaseRow(OptionName, Family, indexInOption, sourceValue.Value, sourceValue.Source,
            new SynthDomainPayload(domain, range, string.IsNullOrEmpty(prefix) ? null : prefix), summary);
    }

    public string Serialize(DnsRecordRow row)
    {
        var p = (SynthDomainPayload)row.Payload;
        return string.IsNullOrEmpty(p.Prefix) ? $"{p.Domain},{p.AddressRange}" : $"{p.Domain},{p.AddressRange},{p.Prefix}";
    }
}

internal sealed class AuthZoneRecordCodec : IDnsRecordDirectiveCodec
{
    public string OptionName => DnsmasqConfKeys.AuthZone;
    public DnsRecordFamily Family => DnsRecordFamily.AuthZone;

    public DnsRecordRow Parse(ValueWithSource sourceValue, int indexInOption)
    {
        var parts = sourceValue.Value.Split(',', StringSplitOptions.TrimEntries);
        var domain = parts.Length > 0 ? parts[0] : "";
        var rest = parts.Length > 1 ? parts.Skip(1).ToArray() : Array.Empty<string>();
        var summary = rest.Length > 0
            ? $"Authoritative zone {domain}: {string.Join(", ", rest)}"
            : $"Authoritative zone {domain}";
        return DnsRecordCodecHelpers.BaseRow(OptionName, Family, indexInOption, sourceValue.Value, sourceValue.Source,
            new AuthZonePayload(domain, rest), summary);
    }

    public string Serialize(DnsRecordRow row)
    {
        var p = (AuthZonePayload)row.Payload;
        return p.SubnetsAndExcludes.Count == 0
            ? p.Domain
            : $"{p.Domain},{string.Join(",", p.SubnetsAndExcludes)}";
    }
}

internal sealed class AuthSoaRecordCodec : IDnsRecordDirectiveCodec
{
    public string OptionName => DnsmasqConfKeys.AuthSoa;
    public DnsRecordFamily Family => DnsRecordFamily.AuthSoa;

    public DnsRecordRow Parse(ValueWithSource sourceValue, int indexInOption)
    {
        var parts = sourceValue.Value.Split(',', StringSplitOptions.TrimEntries);
        var serial = parts.Length > 0 ? parts[0] : "";
        string? hm = parts.Length > 1 ? parts[1] : null;
        string? r = parts.Length > 2 ? parts[2] : null;
        string? ry = parts.Length > 3 ? parts[3] : null;
        string? ex = parts.Length > 4 ? parts[4] : null;
        var summary = $"SOA serial {serial}" + (hm != null ? $", hostmaster {hm}" : "");
        return DnsRecordCodecHelpers.BaseRow(OptionName, Family, indexInOption, sourceValue.Value, sourceValue.Source,
            new AuthSoaPayload(serial, hm, r, ry, ex), summary);
    }

    public string Serialize(DnsRecordRow row)
    {
        var p = (AuthSoaPayload)row.Payload;
        // auth-soa is positional: serial,hostmaster,refresh,retry,expiry — omitting empty slots
        // collapses later fields (e.g. refresh into hostmaster). Keep commas through the last set field.
        var tail = new[]
        {
            p.Hostmaster ?? "",
            p.Refresh ?? "",
            p.Retry ?? "",
            p.Expiry ?? ""
        };
        var lastNonEmpty = -1;
        for (var i = tail.Length - 1; i >= 0; i--)
        {
            if (!string.IsNullOrEmpty(tail[i]))
            {
                lastNonEmpty = i;
                break;
            }
        }

        if (lastNonEmpty < 0)
            return p.Serial;

        var parts = new List<string> { p.Serial };
        for (var i = 0; i <= lastNonEmpty; i++)
            parts.Add(tail[i]);
        return string.Join(",", parts);
    }
}

internal sealed class AuthSecServersRecordCodec : IDnsRecordDirectiveCodec
{
    public string OptionName => DnsmasqConfKeys.AuthSecServers;
    public DnsRecordFamily Family => DnsRecordFamily.AuthSecServers;

    public DnsRecordRow Parse(ValueWithSource sourceValue, int indexInOption)
    {
        var parts = sourceValue.Value.Split(',', StringSplitOptions.TrimEntries);
        var summary = $"Secondary NS: {string.Join(", ", parts)}";
        return DnsRecordCodecHelpers.BaseRow(OptionName, Family, indexInOption, sourceValue.Value, sourceValue.Source,
            new AuthSecServersPayload(parts), summary);
    }

    public string Serialize(DnsRecordRow row)
    {
        var p = (AuthSecServersPayload)row.Payload;
        return string.Join(",", p.Domains);
    }
}

internal sealed class AuthPeerRecordCodec : IDnsRecordDirectiveCodec
{
    public string OptionName => DnsmasqConfKeys.AuthPeer;
    public DnsRecordFamily Family => DnsRecordFamily.AuthPeer;

    public DnsRecordRow Parse(ValueWithSource sourceValue, int indexInOption)
    {
        var parts = sourceValue.Value.Split(',', StringSplitOptions.TrimEntries);
        var summary = $"AXFR peers: {string.Join(", ", parts)}";
        return DnsRecordCodecHelpers.BaseRow(OptionName, Family, indexInOption, sourceValue.Value, sourceValue.Source,
            new AuthPeerPayload(parts), summary);
    }

    public string Serialize(DnsRecordRow row)
    {
        var p = (AuthPeerPayload)row.Payload;
        return string.Join(",", p.Ips);
    }
}
