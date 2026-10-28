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
    public async Task LogQueries_KeyOnly_PreservesEnabledStateAsEmptyValue()
    {
        var (dir, _, managedPath, cache) = CreateCacheWithManaged("log-queries\n");
        try
        {
            var snapshot = await cache.GetSnapshotAsync();

            Assert.Equal("", snapshot.Config.LogQueries);
            Assert.NotNull(snapshot.Sources.LogQueries);
            Assert.Equal(Path.GetFullPath(managedPath), snapshot.Sources.LogQueries!.FilePath, StringComparer.OrdinalIgnoreCase);
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

    [Fact]
    public async Task GetSnapshot_WithManagedFilesDirectory_UsesOverridePaths()
    {
        var dir = Path.Combine(Path.GetTempPath(), "dnsmasq-cache-" + Guid.NewGuid().ToString("N"));
        var managedDir = Path.Combine(dir, "repo");
        Directory.CreateDirectory(dir);
        Directory.CreateDirectory(managedDir);
        var mainPath = Path.Combine(dir, "dnsmasq.conf");
        var managedFileName = "zz-managed.conf";
        var managedHostsFileName = "zz-managed.hosts";
        var managedPath = Path.Combine(managedDir, managedFileName);
        File.WriteAllText(mainPath, $"conf-file={managedPath}\n");
        File.WriteAllText(managedPath, $"addn-hosts={Path.Combine(managedDir, managedHostsFileName)}\n");

        using var cache = new ConfigSetCache(
            Options.Create(new DnsmasqOptions
            {
                MainConfigPath = mainPath,
                ManagedFilesDirectory = managedDir,
                ManagedFileName = managedFileName,
                ManagedHostsFileName = managedHostsFileName
            }),
            NullLogger<ConfigSetCache>.Instance);

        var snapshot = await cache.GetSnapshotAsync();

        Assert.Equal(Path.GetFullPath(managedPath), snapshot.Set.ManagedFilePath);
        Assert.Equal(Path.GetFullPath(Path.Combine(managedDir, managedHostsFileName)), snapshot.Set.ManagedHostsFilePath);
        Assert.Contains(snapshot.Set.Files, f => f.IsManaged && string.Equals(f.Path, Path.GetFullPath(managedPath), StringComparison.Ordinal));
    }

    [Fact]
    public async Task GetSnapshot_ConfFileInMiddle_PreservesInPlaceOrderingForValuesAndSources()
    {
        var dir = Path.Combine(Path.GetTempPath(), "dnsmasq-cache-order-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        var mainPath = Path.Combine(dir, "dnsmasq.conf");
        var includePath = Path.Combine(dir, "extra.conf");
        try
        {
            File.WriteAllText(mainPath, "port=53\nserver=main-before\nconf-file=extra.conf\nport=5353\nserver=main-after\n");
            File.WriteAllText(includePath, "port=54\nserver=included\n");
            using var cache = new ConfigSetCache(
                Options.Create(new DnsmasqOptions { MainConfigPath = mainPath, ManagedFileName = "zz-managed.conf" }),
                NullLogger<ConfigSetCache>.Instance);

            var snapshot = await cache.GetSnapshotAsync();

            Assert.Equal(5353, snapshot.Config.Port);
            Assert.NotNull(snapshot.Sources.Port);
            Assert.Equal(Path.GetFullPath(mainPath), snapshot.Sources.Port!.FilePath);
            Assert.Equal(4, snapshot.Sources.Port.LineNumber);
            Assert.Equal(new[] { "main-before", "included", "main-after" }, snapshot.Config.ServerValues);
            Assert.Equal(new[]
            {
                Path.GetFullPath(mainPath),
                Path.GetFullPath(includePath),
                Path.GetFullPath(mainPath)
            }, snapshot.Sources.ServerValues.Select(value => value.Source?.FilePath));
        }
        finally
        {
            if (Directory.Exists(dir)) Directory.Delete(dir, recursive: true);
        }
    }

    [Fact]
    public async Task GetSnapshot_ExternalIncludedFileChanges_RefreshesBeforeStaleTimeout()
    {
        var dir = Path.Combine(Path.GetTempPath(), "dnsmasq-cache-include-change-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        var mainPath = Path.Combine(dir, "dnsmasq.conf");
        var includePath = Path.Combine(dir, "extra.conf");
        try
        {
            File.WriteAllText(mainPath, "conf-file=extra.conf\n");
            File.WriteAllText(includePath, "port=54\n");
            using var cache = new ConfigSetCache(
                Options.Create(new DnsmasqOptions { MainConfigPath = mainPath, ManagedFileName = "zz-managed.conf" }),
                NullLogger<ConfigSetCache>.Instance);

            var first = await cache.GetSnapshotAsync();
            File.WriteAllText(includePath, "port=5353\n");
            var second = await cache.GetSnapshotAsync();

            Assert.Equal(54, first.Config.Port);
            Assert.Equal(5353, second.Config.Port);
            Assert.NotSame(first, second);
        }
        finally
        {
            if (Directory.Exists(dir)) Directory.Delete(dir, recursive: true);
        }
    }

    [Fact]
    public async Task GetSnapshot_MissingConfFileCreated_RefreshesBeforeStaleTimeout()
    {
        var dir = Path.Combine(Path.GetTempPath(), "dnsmasq-cache-missing-conf-file-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        var mainPath = Path.Combine(dir, "dnsmasq.conf");
        var missingIncludePath = Path.Combine(dir, "later.conf");
        try
        {
            File.WriteAllText(mainPath, "conf-file=later.conf\n");
            using var cache = new ConfigSetCache(
                Options.Create(new DnsmasqOptions { MainConfigPath = mainPath, ManagedFileName = "zz-managed.conf" }),
                NullLogger<ConfigSetCache>.Instance);

            var first = await cache.GetSnapshotAsync();
            File.WriteAllText(missingIncludePath, "server=created-later\n");
            var second = await cache.GetSnapshotAsync();

            Assert.Empty(first.Config.ServerValues);
            Assert.Equal(new[] { "created-later" }, second.Config.ServerValues);
            Assert.NotSame(first, second);
            Assert.Contains(second.Set.Files, file => string.Equals(file.Path, Path.GetFullPath(missingIncludePath), StringComparison.Ordinal));
        }
        finally
        {
            if (Directory.Exists(dir)) Directory.Delete(dir, recursive: true);
        }
    }

    [Fact]
    public async Task GetSnapshot_EmptyConfDirNewMatchingFile_RefreshesWithoutUnrelatedFileChurn()
    {
        var dir = Path.Combine(Path.GetTempPath(), "dnsmasq-cache-empty-conf-dir-" + Guid.NewGuid().ToString("N"));
        var confDir = Path.Combine(dir, "dnsmasq.d");
        Directory.CreateDirectory(confDir);
        var mainPath = Path.Combine(dir, "dnsmasq.conf");
        try
        {
            File.WriteAllText(mainPath, "conf-dir=dnsmasq.d,*.conf\n");
            using var cache = new ConfigSetCache(
                Options.Create(new DnsmasqOptions { MainConfigPath = mainPath, ManagedFileName = "zz-managed.conf" }),
                NullLogger<ConfigSetCache>.Instance);

            var first = await cache.GetSnapshotAsync();
            File.WriteAllText(Path.Combine(confDir, "notes.txt"), "not dnsmasq configuration\n");
            var afterUnrelatedFile = await cache.GetSnapshotAsync();
            File.WriteAllText(Path.Combine(confDir, "10-server.conf"), "server=from-conf-dir\n");
            var afterMatchingFile = await cache.GetSnapshotAsync();

            Assert.Empty(first.Config.ServerValues);
            Assert.Same(first, afterUnrelatedFile);
            Assert.Equal(new[] { "from-conf-dir" }, afterMatchingFile.Config.ServerValues);
            Assert.NotSame(first, afterMatchingFile);
        }
        finally
        {
            if (Directory.Exists(dir)) Directory.Delete(dir, recursive: true);
        }
    }

    [Fact]
    public async Task GetSnapshot_MissingConfDirCreatedThenPopulated_RefreshesBeforeStaleTimeout()
    {
        var dir = Path.Combine(Path.GetTempPath(), "dnsmasq-cache-missing-conf-dir-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        var mainPath = Path.Combine(dir, "dnsmasq.conf");
        var confDir = Path.Combine(dir, "dnsmasq.d");
        try
        {
            File.WriteAllText(mainPath, "conf-dir=dnsmasq.d,*.conf\n");
            using var cache = new ConfigSetCache(
                Options.Create(new DnsmasqOptions { MainConfigPath = mainPath, ManagedFileName = "zz-managed.conf" }),
                NullLogger<ConfigSetCache>.Instance);

            var first = await cache.GetSnapshotAsync();
            Directory.CreateDirectory(confDir);
            var afterDirectoryCreation = await cache.GetSnapshotAsync();
            File.WriteAllText(Path.Combine(confDir, "10-server.conf"), "server=from-new-directory\n");
            var afterMatchingFile = await cache.GetSnapshotAsync();

            Assert.Empty(afterDirectoryCreation.Config.ServerValues);
            Assert.NotSame(first, afterDirectoryCreation);
            Assert.Equal(new[] { "from-new-directory" }, afterMatchingFile.Config.ServerValues);
            Assert.NotSame(afterDirectoryCreation, afterMatchingFile);
        }
        finally
        {
            if (Directory.Exists(dir)) Directory.Delete(dir, recursive: true);
        }
    }

    [Fact]
    public async Task GetSnapshot_MissingMainConfigCreated_RefreshesBeforeStaleTimeout()
    {
        var root = Path.Combine(Path.GetTempPath(), "dnsmasq-cache-missing-main-" + Guid.NewGuid().ToString("N"));
        var configDirectory = Path.Combine(root, "not-created-yet");
        var mainPath = Path.Combine(configDirectory, "dnsmasq.conf");
        try
        {
            using var cache = new ConfigSetCache(
                Options.Create(new DnsmasqOptions { MainConfigPath = mainPath, ManagedFileName = "zz-managed.conf" }),
                NullLogger<ConfigSetCache>.Instance);

            var first = await cache.GetSnapshotAsync();
            Directory.CreateDirectory(configDirectory);
            File.WriteAllText(mainPath, "server=from-recreated-main\n");
            var second = await cache.GetSnapshotAsync();

            Assert.Empty(first.Config.ServerValues);
            Assert.Equal(new[] { "from-recreated-main" }, second.Config.ServerValues);
            Assert.NotSame(first, second);
        }
        finally
        {
            if (Directory.Exists(root)) Directory.Delete(root, recursive: true);
        }
    }
}
