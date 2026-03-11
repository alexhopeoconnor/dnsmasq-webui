using DnsmasqWebUI.Infrastructure.Services.EffectiveConfig.Metadata;
using DnsmasqWebUI.Infrastructure.Serialization.Parsers.DnsmasqConfig;
using DnsmasqWebUI.Tests.Helpers;

namespace DnsmasqWebUI.Tests.Serialization.Parsers.DnsmasqConfig;

/// <summary>
/// Tests that the parser correctly handles the official dnsmasq example (testdata/dnsmasq.official.conf.example).
/// All options in that file are commented out, so parsing it should yield no active values (single path, empty/false/null).
/// </summary>
public class DnsmasqConfIncludeParserOfficialExampleTests
{
    private static string GetOfficialExamplePath()
    {
        var path = TestDataHelper.GetPath("dnsmasq.official.conf.example");
        Assert.True(File.Exists(path), "Testdata dnsmasq.official.conf.example required (download from https://github.com/imp/dnsmasq)");
        return path;
    }

    [Fact]
    public void GetIncludedPaths_OfficialExample_ReturnsSinglePath()
    {
        var mainPath = GetOfficialExamplePath();
        var paths = DnsmasqConfIncludeParser.GetIncludedPaths(mainPath);
        Assert.Single(paths);
        Assert.Equal(Path.GetFullPath(mainPath), paths[0]);
    }

    [Fact]
    public void GetLastValueFromConfigFiles_OfficialExample_AllCommented_ReturnsNull()
    {
        var mainPath = GetOfficialExamplePath();
        var paths = new[] { mainPath };
        var (portVal, _) = DnsmasqConfIncludeParser.GetLastValueFromConfigFiles(paths, DnsmasqConfKeys.Port);
        var (cacheVal, _) = DnsmasqConfIncludeParser.GetLastValueFromConfigFiles(paths, DnsmasqConfKeys.CacheSize);
        var (mxTargetVal, _) = DnsmasqConfIncludeParser.GetLastValueFromConfigFiles(paths, DnsmasqConfKeys.MxTarget);
        var (dhcpScriptVal, _) = DnsmasqConfIncludeParser.GetLastValueFromConfigFiles(paths, DnsmasqConfKeys.DhcpScript);
        Assert.Null(portVal);
        Assert.Null(cacheVal);
        Assert.Null(mxTargetVal);
        Assert.Null(dhcpScriptVal);
    }

    [Fact]
    public void GetMultiValueFromConfigFiles_OfficialExample_AllCommented_ReturnsEmpty()
    {
        var mainPath = GetOfficialExamplePath();
        var paths = new[] { mainPath };
        var servers = DnsmasqConfIncludeParser.GetMultiValueFromConfigFiles(paths, DnsmasqConfKeys.Server);
        var locals = DnsmasqConfIncludeParser.GetMultiValueFromConfigFiles(paths, DnsmasqConfKeys.Local);
        var dhcpOptionForce = DnsmasqConfIncludeParser.GetMultiValueFromConfigFiles(paths, DnsmasqConfKeys.DhcpOptionForce);
        var ipset = DnsmasqConfIncludeParser.GetMultiValueFromConfigFiles(paths, DnsmasqConfKeys.Ipset);
        var nftset = DnsmasqConfIncludeParser.GetMultiValueFromConfigFiles(paths, DnsmasqConfKeys.Nftset);
        var dhcpMac = DnsmasqConfIncludeParser.GetMultiValueFromConfigFiles(paths, DnsmasqConfKeys.DhcpMac);
        var dhcpNameMatch = DnsmasqConfIncludeParser.GetMultiValueFromConfigFiles(paths, DnsmasqConfKeys.DhcpNameMatch);
        var dhcpIgnoreNames = DnsmasqConfIncludeParser.GetMultiValueFromConfigFiles(paths, DnsmasqConfKeys.DhcpIgnoreNames);
        Assert.Empty(servers);
        Assert.Empty(locals);
        Assert.Empty(dhcpOptionForce);
        Assert.Empty(ipset);
        Assert.Empty(nftset);
        Assert.Empty(dhcpMac);
        Assert.Empty(dhcpNameMatch);
        Assert.Empty(dhcpIgnoreNames);
    }

    [Fact]
    public void GetFlagFromConfigFiles_OfficialExample_AllCommented_ReturnsFalse()
    {
        var mainPath = GetOfficialExamplePath();
        var paths = new[] { mainPath };
        Assert.False(DnsmasqConfIncludeParser.GetFlagFromConfigFiles(paths, DnsmasqConfKeys.ExpandHosts));
        Assert.False(DnsmasqConfIncludeParser.GetFlagFromConfigFiles(paths, DnsmasqConfKeys.ReadEthers));
        Assert.False(DnsmasqConfIncludeParser.GetFlagFromConfigFiles(paths, DnsmasqConfKeys.DhcpRapidCommit));
        Assert.False(DnsmasqConfIncludeParser.GetFlagFromConfigFiles(paths, DnsmasqConfKeys.TftpNoFail));
        Assert.False(DnsmasqConfIncludeParser.GetFlagFromConfigFiles(paths, DnsmasqConfKeys.TftpNoBlocksize));
        Assert.False(DnsmasqConfIncludeParser.GetFlagFromConfigFiles(paths, DnsmasqConfKeys.Localmx));
        Assert.False(DnsmasqConfIncludeParser.GetFlagFromConfigFiles(paths, DnsmasqConfKeys.Selfmx));
        Assert.False(DnsmasqConfIncludeParser.GetFlagFromConfigFiles(paths, DnsmasqConfKeys.EnableRa));
        Assert.False(DnsmasqConfIncludeParser.GetFlagFromConfigFiles(paths, DnsmasqConfKeys.LogDhcp));
    }

    [Fact]
    public void GetAddnHostsPathsFromConfigFiles_OfficialExample_AllCommented_ReturnsEmpty()
    {
        var mainPath = GetOfficialExamplePath();
        var paths = new[] { mainPath };
        var addnHosts = DnsmasqConfIncludeParser.GetAddnHostsPathsFromConfigFiles(paths);
        Assert.NotNull(addnHosts);
        Assert.Empty(addnHosts);
    }

    [Fact]
    public void GetNoHostsFromConfigFiles_OfficialExample_AllCommented_ReturnsFalse()
    {
        var mainPath = GetOfficialExamplePath();
        var paths = new[] { mainPath };
        var noHosts = DnsmasqConfIncludeParser.GetNoHostsFromConfigFiles(paths);
        Assert.False(noHosts);
    }

    /// <summary>Official-like sample with commented and active lines: commented ignored, active parsed, state reflects active only.</summary>
    [Fact]
    public void OfficialLikeActive_CommentedLinesIgnored_ActiveLinesParsed()
    {
        var mainPath = TestDataHelper.GetPath("real-world/edge/dnsmasq-real-official-like-active.conf");
        Assert.True(File.Exists(mainPath));
        var paths = DnsmasqConfIncludeParser.GetIncludedPaths(mainPath);

        var (portVal, _) = DnsmasqConfIncludeParser.GetLastValueFromConfigFiles(paths, DnsmasqConfKeys.Port);
        Assert.NotNull(portVal);
        Assert.True(int.TryParse(portVal, out var port) && port == 5353);

        var servers = DnsmasqConfIncludeParser.GetMultiValueFromConfigFiles(paths, DnsmasqConfKeys.Server);
        Assert.Single(servers);
        Assert.Equal("1.1.1.1", servers[0]);

        Assert.True(DnsmasqConfIncludeParser.GetFlagFromConfigFiles(paths, DnsmasqConfKeys.ExpandHosts));
    }
}
