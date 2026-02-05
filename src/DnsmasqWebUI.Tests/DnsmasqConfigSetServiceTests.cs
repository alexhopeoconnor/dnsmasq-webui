using DnsmasqWebUI.Models.Config;
using DnsmasqWebUI.Models.Dnsmasq;
using DnsmasqWebUI.Models.Dnsmasq.EffectiveConfig;
using DnsmasqWebUI.Infrastructure.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace DnsmasqWebUI.Tests;

/// <summary>
/// Tests for DnsmasqConfigSetService, including GetEffectiveConfigWithSources.
/// </summary>
public class DnsmasqConfigSetServiceTests
{
    [Fact]
    public void GetEffectiveConfigWithSources_NoMainPath_ReturnsDefaultConfigAndDefaultSources()
    {
        var options = Options.Create(new DnsmasqOptions { MainConfigPath = "" });
        var cache = new ConfigSetCache(options, NullLogger<ConfigSetCache>.Instance);
        var service = new DnsmasqConfigSetService(cache);
        var (config, sources) = service.GetEffectiveConfigWithSources();

        Assert.NotNull(config);
        Assert.False(config.NoHosts);
        Assert.Empty(config.AddnHostsPaths);
        Assert.NotNull(sources);
        Assert.Empty(sources.AddnHostsPaths);
        Assert.Empty(sources.ServerLocalValues);
        Assert.Empty(sources.DhcpRanges);
        Assert.Null(sources.NoHosts);
        Assert.Null(sources.Port);
        Assert.Null(sources.DhcpLeaseFilePath);
    }

    [Fact]
    public void GetEffectiveConfigWithSources_WithTempConfig_ReturnsConfigAndSourcesFromFiles()
    {
        var dir = Path.Combine(Path.GetTempPath(), "dnsmasq-svc-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        var mainPath = Path.Combine(dir, "dnsmasq.conf");
        ConfigSetCache? cache = null;
        try
        {
            File.WriteAllText(mainPath, "port=53\ncache-size=500\naddn-hosts=/etc/hosts.extra\n");
            var options = Options.Create(new DnsmasqOptions
            {
                MainConfigPath = mainPath,
                ManagedFileName = "zz-dnsmasq-webui.conf"
            });
            cache = new ConfigSetCache(options, NullLogger<ConfigSetCache>.Instance);
            var service = new DnsmasqConfigSetService(cache);
            var (config, sources) = service.GetEffectiveConfigWithSources();

            Assert.NotNull(config);
            Assert.Equal(53, config.Port);
            Assert.Equal(500, config.CacheSize);
            Assert.Single(config.AddnHostsPaths);
            Assert.Equal(Path.GetFullPath("/etc/hosts.extra"), config.AddnHostsPaths[0]);
            Assert.Empty(config.ServerLocalValues);
            Assert.Empty(config.DhcpRanges);

            Assert.NotNull(sources);
            Assert.NotNull(sources.Port);
            Assert.Equal(Path.GetFileName(mainPath), sources.Port!.FileName);
            Assert.NotNull(sources.CacheSize);
            Assert.Single(sources.AddnHostsPaths);
            Assert.Equal(Path.GetFullPath("/etc/hosts.extra"), sources.AddnHostsPaths[0].Path);
            Assert.NotNull(sources.AddnHostsPaths[0].Source);
            Assert.Equal(Path.GetFileName(mainPath), sources.AddnHostsPaths[0].Source!.FileName);
        }
        finally
        {
            cache?.Dispose();
            Directory.Delete(dir, recursive: true);
        }
    }
}
