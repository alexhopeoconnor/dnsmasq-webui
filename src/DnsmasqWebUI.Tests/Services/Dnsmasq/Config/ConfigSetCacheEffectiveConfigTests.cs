using DnsmasqWebUI.Infrastructure.Services.EffectiveConfig.Metadata;
using DnsmasqWebUI.Infrastructure.Services.Dnsmasq.Config;
using DnsmasqWebUI.Models.Config;
using DnsmasqWebUI.Models.Dnsmasq.EffectiveConfig;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using DnsmasqWebUI.Tests.Helpers;

namespace DnsmasqWebUI.Tests.Services.Dnsmasq.Config;

/// <summary>
/// Tests effective-config resolution in ConfigSetCache (BuildEffectiveConfig / BuildEffectiveConfigSources).
/// Verifies no-0x20-encode precedence and source attribution per dnsmasq docs.
/// </summary>
public class ConfigSetCacheEffectiveConfigTests
{
    private static (string Dir, string MainPath, string ManagedPath, ConfigSetCache Cache) CreateCacheWithManaged(string managedContent)
    {
        var dir = Path.Combine(Path.GetTempPath(), "dnsmasq-cache-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        var mainPath = Path.Combine(dir, "dnsmasq.conf");
        var managedName = "zz-managed.conf";
        var managedPath = Path.Combine(dir, managedName);
        File.WriteAllText(mainPath, "port=53\n");
        File.WriteAllText(managedPath, managedContent);
        var options = Options.Create(new DnsmasqOptions { MainConfigPath = mainPath, ManagedFileName = managedName });
        var cache = new ConfigSetCache(options, NullLogger<ConfigSetCache>.Instance);
        return (dir, mainPath, managedPath, cache);
    }

    [Fact]
    public async Task Do0x20_BothPresent_No0x20Wins()
    {
        var (dir, _, managedPath, cache) = CreateCacheWithManaged("do-0x20-encode\nno-0x20-encode\n");
        try
        {
            var snapshot = await cache.GetSnapshotAsync();
            Assert.Equal(ExplicitToggleState.Disabled, snapshot.Config.Do0x20EncodeState);
            Assert.NotNull(snapshot.Sources.Do0x20Encode);
            Assert.Equal(Path.GetFullPath(managedPath), snapshot.Sources.Do0x20Encode.FilePath, StringComparer.OrdinalIgnoreCase);
        }
        finally
        {
            cache.Dispose();
            if (Directory.Exists(dir)) Directory.Delete(dir, recursive: true);
        }
    }

    [Fact]
    public async Task Do0x20_OnlyDo_Enabled()
    {
        var (dir, _, _, cache) = CreateCacheWithManaged("do-0x20-encode\n");
        try
        {
            var snapshot = await cache.GetSnapshotAsync();
            Assert.Equal(ExplicitToggleState.Enabled, snapshot.Config.Do0x20EncodeState);
        }
        finally
        {
            cache.Dispose();
            if (Directory.Exists(dir)) Directory.Delete(dir, recursive: true);
        }
    }

    [Fact]
    public async Task Do0x20_OnlyNo_Disabled()
    {
        var (dir, _, managedPath, cache) = CreateCacheWithManaged("no-0x20-encode\n");
        try
        {
            var snapshot = await cache.GetSnapshotAsync();
            Assert.Equal(ExplicitToggleState.Disabled, snapshot.Config.Do0x20EncodeState);
            Assert.NotNull(snapshot.Sources.Do0x20Encode);
            Assert.Equal(Path.GetFullPath(managedPath), snapshot.Sources.Do0x20Encode.FilePath, StringComparer.OrdinalIgnoreCase);
        }
        finally
        {
            cache.Dispose();
            if (Directory.Exists(dir)) Directory.Delete(dir, recursive: true);
        }
    }

    [Fact]
    public async Task Do0x20_Neither_Default()
    {
        var (dir, _, _, cache) = CreateCacheWithManaged("port=53\n");
        try
        {
            var snapshot = await cache.GetSnapshotAsync();
            Assert.Equal(ExplicitToggleState.Default, snapshot.Config.Do0x20EncodeState);
            Assert.Null(snapshot.Sources.Do0x20Encode);
        }
        finally
        {
            cache.Dispose();
            if (Directory.Exists(dir)) Directory.Delete(dir, recursive: true);
        }
    }

    [Fact]
    public async Task Do0x20_SourceAttribution_WhenBothPresent_IsNo0x20Line()
    {
        var (dir, _, managedPath, cache) = CreateCacheWithManaged("no-0x20-encode\ndo-0x20-encode\n");
        try
        {
            var snapshot = await cache.GetSnapshotAsync();
            Assert.Equal(ExplicitToggleState.Disabled, snapshot.Config.Do0x20EncodeState);
            Assert.NotNull(snapshot.Sources.Do0x20Encode);
            Assert.Equal(Path.GetFullPath(managedPath), snapshot.Sources.Do0x20Encode.FilePath, StringComparer.OrdinalIgnoreCase);
        }
        finally
        {
            cache.Dispose();
            if (Directory.Exists(dir)) Directory.Delete(dir, recursive: true);
        }
    }

    [Fact]
    public async Task StripFlags_ParseState_And_Source()
    {
        var (dir, _, managedPath, cache) = CreateCacheWithManaged("strip-mac\nstrip-subnet\n");
        try
        {
            var snapshot = await cache.GetSnapshotAsync();
            Assert.True(snapshot.Config.StripMac);
            Assert.True(snapshot.Config.StripSubnet);
            Assert.NotNull(snapshot.Sources.StripMac);
            Assert.NotNull(snapshot.Sources.StripSubnet);
            Assert.Equal(Path.GetFullPath(managedPath), snapshot.Sources.StripMac!.FilePath, StringComparer.OrdinalIgnoreCase);
            Assert.Equal(Path.GetFullPath(managedPath), snapshot.Sources.StripSubnet!.FilePath, StringComparer.OrdinalIgnoreCase);
        }
        finally
        {
            cache.Dispose();
            if (Directory.Exists(dir)) Directory.Delete(dir, recursive: true);
        }
    }

    [Fact]
    public async Task RealWorldCorpus_EdgeCommentedActiveMix_Do0x20StateAndSource()
    {
        var mainPath = TestDataHelper.GetPath("real-world/edge/dnsmasq-real-edge-commented-active-mix.conf");
        Assert.True(File.Exists(mainPath));
        var dir = Path.GetDirectoryName(mainPath)!;
        var managedName = "zz-managed.conf";
        var options = Options.Create(new DnsmasqOptions { MainConfigPath = mainPath, ManagedFileName = managedName });
        using var cache = new ConfigSetCache(options, NullLogger<ConfigSetCache>.Instance);
        cache.Invalidate();
        var snapshot = await cache.GetSnapshotAsync();
        Assert.Equal(ExplicitToggleState.Disabled, snapshot.Config.Do0x20EncodeState);
        Assert.NotNull(snapshot.Sources.Do0x20Encode);
        Assert.Single(snapshot.Config.ServerValues);
        Assert.Equal("1.1.1.1", snapshot.Config.ServerValues[0]);
    }

    [Fact]
    public async Task RealWorldCorpus_GoodHomeBasic_ServerCountAndSource()
    {
        var mainPath = TestDataHelper.GetPath("real-world/good/dnsmasq-real-home-basic.conf");
        Assert.True(File.Exists(mainPath));
        var options = Options.Create(new DnsmasqOptions { MainConfigPath = mainPath, ManagedFileName = "zz-managed.conf" });
        using var cache = new ConfigSetCache(options, NullLogger<ConfigSetCache>.Instance);
        cache.Invalidate();
        var snapshot = await cache.GetSnapshotAsync();
        Assert.True(snapshot.Config.ServerValues.Count >= 2);
        Assert.Equal(2, snapshot.Sources.ServerValues.Count);
    }

    [Fact]
    public async Task RealWorldCorpus_GoodResolverRich_LocalAddressCacheSizeFlags()
    {
        var mainPath = TestDataHelper.GetPath("real-world/good/dnsmasq-real-resolver-rich.conf");
        Assert.True(File.Exists(mainPath));
        var options = Options.Create(new DnsmasqOptions { MainConfigPath = mainPath, ManagedFileName = "zz-managed.conf" });
        using var cache = new ConfigSetCache(options, NullLogger<ConfigSetCache>.Instance);
        cache.Invalidate();
        var snapshot = await cache.GetSnapshotAsync();
        Assert.True(snapshot.Config.ServerValues.Count >= 2);
        Assert.True(snapshot.Config.LocalValues.Count >= 2);
        Assert.True(snapshot.Config.AddressValues.Count >= 2);
        Assert.Equal(1000, snapshot.Config.CacheSize);
        Assert.True(snapshot.Config.NoResolv);
        Assert.True(snapshot.Config.BogusPriv);
        Assert.True(snapshot.Config.ExpandHosts);
    }
}
