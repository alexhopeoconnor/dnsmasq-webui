using DnsmasqWebUI.Infrastructure.Helpers.Config;
using DnsmasqWebUI.Infrastructure.Parsers;

namespace DnsmasqWebUI.Tests;

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
        var servers = DnsmasqConfIncludeParser.GetMultiValueFromConfigFiles(paths, DnsmasqConfKeys.ServerLocalKeys);
        var dhcpOptionForce = DnsmasqConfIncludeParser.GetMultiValueFromConfigFiles(paths, DnsmasqConfKeys.DhcpOptionForce);
        var ipset = DnsmasqConfIncludeParser.GetMultiValueFromConfigFiles(paths, DnsmasqConfKeys.Ipset);
        var nftset = DnsmasqConfIncludeParser.GetMultiValueFromConfigFiles(paths, DnsmasqConfKeys.Nftset);
        var dhcpMac = DnsmasqConfIncludeParser.GetMultiValueFromConfigFiles(paths, DnsmasqConfKeys.DhcpMac);
        var dhcpNameMatch = DnsmasqConfIncludeParser.GetMultiValueFromConfigFiles(paths, DnsmasqConfKeys.DhcpNameMatch);
        var dhcpIgnoreNames = DnsmasqConfIncludeParser.GetMultiValueFromConfigFiles(paths, DnsmasqConfKeys.DhcpIgnoreNames);
        Assert.Empty(servers);
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
    public void OfficialExample_FileContainsExpectedOptionNames()
    {
        var path = GetOfficialExamplePath();
        var content = File.ReadAllText(path);
        // Sanity: official example documents the options we support (as commented lines).
        Assert.Contains("read-ethers", content);
        Assert.Contains("dhcp-option-force", content);
        Assert.Contains("dhcp-rapid-commit", content);
        Assert.Contains("dhcp-script", content);
        Assert.Contains("tftp-no-fail", content);
        Assert.Contains("tftp-no-blocksize", content);
        Assert.Contains("mx-target", content);
        Assert.Contains("localmx", content);
        Assert.Contains("selfmx", content);
        Assert.Contains("enable-ra", content);
        Assert.Contains("log-dhcp", content);
        Assert.Contains("ipset=", content);
        Assert.Contains("nftset=", content);
        Assert.Contains("dhcp-mac", content);
        Assert.Contains("dhcp-name-match", content);
        Assert.Contains("dhcp-ignore-names", content);
        // SRV records use option name srv-host (not "srv")
        Assert.Contains("srv-host", content);
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
}
