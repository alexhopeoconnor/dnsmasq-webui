using DnsmasqWebUI.Infrastructure.Services.EffectiveConfig.Metadata;
using DnsmasqWebUI.Infrastructure.Serialization.Parsers.DnsmasqConfig;
using DnsmasqWebUI.Tests.Helpers;

namespace DnsmasqWebUI.Tests.Serialization.Parsers.DnsmasqConfig;

/// <summary>
/// Corpus-driven parser tests using testdata/real-world. Asserts counts, booleans, and effective state only.
/// </summary>
public class DnsmasqConfIncludeParserRealWorldCorpusTests
{
    public static IEnumerable<object[]> GetCases()
    {
        foreach (var c in RealWorldCasesHelper.LoadCases())
            yield return new object[] { c };
    }

    [Theory]
    [MemberData(nameof(GetCases))]
    public void GetIncludedPaths_RealWorldCorpus_ReturnsAtLeastMain(CorpusCase c)
    {
        var mainPath = RealWorldCasesHelper.Resolve(c);
        Assert.True(File.Exists(mainPath), $"Corpus file missing: {c.File}");

        var paths = DnsmasqConfIncludeParser.GetIncludedPaths(mainPath);
        Assert.NotNull(paths);
        Assert.NotEmpty(paths);
        Assert.Contains(Path.GetFullPath(mainPath), paths);
    }

    [Theory]
    [MemberData(nameof(GetCases))]
    public void ParseEffectiveSignals_FromRealWorldCorpus(CorpusCase c)
    {
        var e = c.Expected;
        var hasAny = e.ServerCountMin is not null || e.NoHosts is not null || e.Do0x20State is not null ||
                     e.Port is not null || e.ExpandHosts is not null || e.LocalCountMin is not null ||
                     e.AddressCountMin is not null || e.CacheSize is not null || e.AddnHostsCountMin is not null ||
                     e.DomainCountMin is not null || e.NoResolv is not null || e.BogusPriv is not null ||
                     e.CnameCountMin is not null || e.TxtRecordCountMin is not null || e.SrvCountMin is not null ||
                     e.PtrRecordCountMin is not null || e.MxHostCountMin is not null || e.AuthZoneCountMin is not null ||
                     e.SynthDomainCountMin is not null || e.AuthServerCountMin is not null;
        if (!hasAny)
            return;

        var mainPath = RealWorldCasesHelper.Resolve(c);
        var paths = DnsmasqConfIncludeParser.GetIncludedPaths(mainPath);

        if (e.ServerCountMin is int minServers)
        {
            var servers = DnsmasqConfIncludeParser.GetMultiValueFromConfigFiles(paths, DnsmasqConfKeys.Server);
            Assert.True(servers.Count >= minServers, $"Expected at least {minServers} server(s) for {c.File}");
        }
        if (e.LocalCountMin is int minLocal)
        {
            var locals = DnsmasqConfIncludeParser.GetMultiValueFromConfigFiles(paths, DnsmasqConfKeys.Local);
            Assert.True(locals.Count >= minLocal, $"Expected at least {minLocal} local(s) for {c.File}");
        }
        if (e.AddressCountMin is int minAddr)
        {
            var addrs = DnsmasqConfIncludeParser.GetMultiValueFromConfigFiles(paths, DnsmasqConfKeys.Address);
            Assert.True(addrs.Count >= minAddr, $"Expected at least {minAddr} address(es) for {c.File}");
        }
        if (e.CacheSize is int cacheSize)
        {
            var (val, _) = DnsmasqConfIncludeParser.GetLastValueFromConfigFiles(paths, DnsmasqConfKeys.CacheSize);
            Assert.NotNull(val);
            Assert.True(int.TryParse(val, out var n) && n == cacheSize);
        }
        if (e.AddnHostsCountMin is int minAddn)
        {
            var addn = DnsmasqConfIncludeParser.GetAddnHostsPathsFromConfigFiles(paths);
            Assert.True(addn.Count >= minAddn);
        }
        if (e.DomainCountMin is int minDomain)
        {
            var domains = DnsmasqConfIncludeParser.GetMultiValueFromConfigFiles(paths, DnsmasqConfKeys.Domain);
            Assert.True(domains.Count >= minDomain);
        }
        if (e.NoHosts is bool noHostsExpected)
        {
            var noHosts = DnsmasqConfIncludeParser.GetNoHostsFromConfigFiles(paths);
            Assert.Equal(noHostsExpected, noHosts);
        }
        if (e.NoResolv is true)
        {
            Assert.True(DnsmasqConfIncludeParser.GetFlagFromConfigFiles(paths, DnsmasqConfKeys.NoResolv));
        }
        if (e.BogusPriv is true)
        {
            Assert.True(DnsmasqConfIncludeParser.GetFlagFromConfigFiles(paths, DnsmasqConfKeys.BogusPriv));
        }
        if (e.Do0x20State == "Disabled")
        {
            Assert.True(DnsmasqConfIncludeParser.GetFlagFromConfigFiles(paths, DnsmasqConfKeys.No0x20Encode));
        }
        if (e.Port is int portExpected)
        {
            var (portVal, _) = DnsmasqConfIncludeParser.GetLastValueFromConfigFiles(paths, DnsmasqConfKeys.Port);
            Assert.NotNull(portVal);
            Assert.True(int.TryParse(portVal, out var port) && port == portExpected);
        }
        if (e.ExpandHosts is true)
        {
            Assert.True(DnsmasqConfIncludeParser.GetFlagFromConfigFiles(paths, DnsmasqConfKeys.ExpandHosts));
        }
        if (e.CnameCountMin is int minCname)
        {
            var list = DnsmasqConfIncludeParser.GetMultiValueFromConfigFiles(paths, DnsmasqConfKeys.Cname);
            Assert.True(list.Count >= minCname);
        }
        if (e.TxtRecordCountMin is int minTxt)
        {
            var list = DnsmasqConfIncludeParser.GetMultiValueFromConfigFiles(paths, DnsmasqConfKeys.TxtRecord);
            Assert.True(list.Count >= minTxt);
        }
        if (e.SrvCountMin is int minSrv)
        {
            var list = DnsmasqConfIncludeParser.GetMultiValueFromConfigFiles(paths, DnsmasqConfKeys.Srv);
            Assert.True(list.Count >= minSrv);
        }
        if (e.PtrRecordCountMin is int minPtr)
        {
            var list = DnsmasqConfIncludeParser.GetMultiValueFromConfigFiles(paths, DnsmasqConfKeys.PtrRecord);
            Assert.True(list.Count >= minPtr);
        }
        if (e.MxHostCountMin is int minMx)
        {
            var list = DnsmasqConfIncludeParser.GetMultiValueFromConfigFiles(paths, DnsmasqConfKeys.MxHost);
            Assert.True(list.Count >= minMx);
        }
        if (e.AuthZoneCountMin is int minAz)
        {
            var list = DnsmasqConfIncludeParser.GetMultiValueFromConfigFiles(paths, DnsmasqConfKeys.AuthZone);
            Assert.True(list.Count >= minAz);
        }
        if (e.SynthDomainCountMin is int minSd)
        {
            var list = DnsmasqConfIncludeParser.GetMultiValueFromConfigFiles(paths, DnsmasqConfKeys.SynthDomain);
            Assert.True(list.Count >= minSd);
        }
        if (e.AuthServerCountMin is int minAs)
        {
            var list = DnsmasqConfIncludeParser.GetMultiValueFromConfigFiles(paths, DnsmasqConfKeys.AuthServer);
            Assert.True(list.Count >= minAs);
        }
    }
}
