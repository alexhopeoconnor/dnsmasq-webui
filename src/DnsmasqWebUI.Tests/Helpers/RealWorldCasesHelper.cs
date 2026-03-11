using System.Text.Json;

namespace DnsmasqWebUI.Tests.Helpers;

/// <summary>Expected parser/effective state for a real-world corpus case. Counts and booleans only.</summary>
public sealed class CorpusExpected
{
    public int? ServerCountMin { get; set; }
    public int? LocalCountMin { get; set; }
    public int? AddressCountMin { get; set; }
    public int? CacheSize { get; set; }
    public int? AddnHostsCountMin { get; set; }
    public int? DomainCountMin { get; set; }
    public bool? NoHosts { get; set; }
    public bool? NoResolv { get; set; }
    public bool? BogusPriv { get; set; }
    public string? Do0x20State { get; set; }
    public int? Port { get; set; }
    public bool? ExpandHosts { get; set; }
    public int? CnameCountMin { get; set; }
    public int? TxtRecordCountMin { get; set; }
    public int? SrvCountMin { get; set; }
    public int? PtrRecordCountMin { get; set; }
    public int? MxHostCountMin { get; set; }
    public int? AuthZoneCountMin { get; set; }
    public int? SynthDomainCountMin { get; set; }
    public int? AuthServerCountMin { get; set; }
}

/// <summary>Single real-world corpus case from dnsmasq-real-cases.json.</summary>
public sealed class CorpusCase
{
    public string File { get; set; } = "";
    public CorpusExpected Expected { get; set; } = new();
    public List<string> SemanticInvalidOptions { get; set; } = [];
}

/// <summary>Loads real-world corpus index and resolves paths via TestDataHelper.</summary>
public static class RealWorldCasesHelper
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public static IReadOnlyList<CorpusCase> LoadCases()
    {
        var path = TestDataHelper.GetPath("real-world/dnsmasq-real-cases.json");
        var json = File.ReadAllText(path);
        return JsonSerializer.Deserialize<List<CorpusCase>>(json, JsonOptions) ?? [];
    }

    public static string Resolve(CorpusCase c) => TestDataHelper.GetPath(c.File);
}
