using DnsmasqWebUI.Infrastructure.Services.EffectiveConfig.Metadata;
using DnsmasqWebUI.Infrastructure.Services.Dnsmasq.Config;
using DnsmasqWebUI.Models.Config;
using DnsmasqWebUI.Models.Dnsmasq.EffectiveConfig;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace DnsmasqWebUI.Tests.Services.Dnsmasq.Config;

/// <summary>
/// Save-path tests for DnsmasqConfigService.ApplyEffectiveConfigChangesAsync.
/// Verifies that pending changes produce the correct managed config lines.
/// </summary>
public class DnsmasqConfigServiceApplyChangesTests
{
    [Fact]
    public async Task ApplyEffectiveConfigChangesAsync_ConntrackTrue_WritesBareFlagLine()
    {
        var dir = Path.Combine(Path.GetTempPath(), "dnsmasq-apply-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        var mainPath = Path.Combine(dir, "dnsmasq.conf");
        var managedName = "zz-managed.conf";
        var managedPath = Path.Combine(dir, managedName);
        ConfigSetCache? cache = null;
        try
        {
            File.WriteAllText(mainPath, "port=53\n");
            var options = Options.Create(new DnsmasqOptions { MainConfigPath = mainPath, ManagedFileName = managedName });
            cache = new ConfigSetCache(options, NullLogger<ConfigSetCache>.Instance);
            var setService = new DnsmasqConfigSetService(cache);
            var configService = new DnsmasqConfigService(setService, cache, NullLogger<DnsmasqConfigService>.Instance);

            var changes = new List<PendingEffectiveConfigChange>
            {
                new(EffectiveConfigSections.SectionResolver, DnsmasqConfKeys.Conntrack, false, true, null)
            };
            await configService.ApplyEffectiveConfigChangesAsync(changes);

            Assert.True(File.Exists(managedPath));
            var content = await File.ReadAllTextAsync(managedPath);
            var lines = content.TrimEnd().Split('\n').Select(l => l.Trim()).Where(l => l.Length > 0).ToList();
            Assert.Contains(lines, l => l == "conntrack");
        }
        finally
        {
            cache?.Dispose();
            if (Directory.Exists(dir))
                Directory.Delete(dir, recursive: true);
        }
    }

    [Fact]
    public async Task ApplyEffectiveConfigChangesAsync_ConntrackFalse_RemovesFlagLine()
    {
        var dir = Path.Combine(Path.GetTempPath(), "dnsmasq-apply-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        var mainPath = Path.Combine(dir, "dnsmasq.conf");
        var managedName = "zz-managed.conf";
        var managedPath = Path.Combine(dir, managedName);
        ConfigSetCache? cache = null;
        try
        {
            File.WriteAllText(mainPath, "port=53\n");
            File.WriteAllText(managedPath, "conntrack\n");
            var options = Options.Create(new DnsmasqOptions { MainConfigPath = mainPath, ManagedFileName = managedName });
            cache = new ConfigSetCache(options, NullLogger<ConfigSetCache>.Instance);
            cache.Invalidate();
            var setService = new DnsmasqConfigSetService(cache);
            var configService = new DnsmasqConfigService(setService, cache, NullLogger<DnsmasqConfigService>.Instance);

            var changes = new List<PendingEffectiveConfigChange>
            {
                new(EffectiveConfigSections.SectionResolver, DnsmasqConfKeys.Conntrack, true, false, null)
            };
            await configService.ApplyEffectiveConfigChangesAsync(changes);

            var content = await File.ReadAllTextAsync(managedPath);
            var lines = content.TrimEnd().Split('\n').Select(l => l.Trim()).Where(l => l.Length > 0).ToList();
            Assert.DoesNotContain(lines, l => l == "conntrack" || l.StartsWith("conntrack=", StringComparison.Ordinal));
        }
        finally
        {
            cache?.Dispose();
            if (Directory.Exists(dir))
                Directory.Delete(dir, recursive: true);
        }
    }

    [Fact]
    public async Task ApplyEffectiveConfigChangesAsync_StripMacTrue_WritesBareFlagLine()
    {
        var dir = Path.Combine(Path.GetTempPath(), "dnsmasq-apply-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        var mainPath = Path.Combine(dir, "dnsmasq.conf");
        var managedName = "zz-managed.conf";
        var managedPath = Path.Combine(dir, managedName);
        ConfigSetCache? cache = null;
        try
        {
            File.WriteAllText(mainPath, "port=53\n");
            var options = Options.Create(new DnsmasqOptions { MainConfigPath = mainPath, ManagedFileName = managedName });
            cache = new ConfigSetCache(options, NullLogger<ConfigSetCache>.Instance);
            var setService = new DnsmasqConfigSetService(cache);
            var configService = new DnsmasqConfigService(setService, cache, NullLogger<DnsmasqConfigService>.Instance);

            var changes = new List<PendingEffectiveConfigChange>
            {
                new(EffectiveConfigSections.SectionCache, DnsmasqConfKeys.StripMac, false, true, null)
            };
            await configService.ApplyEffectiveConfigChangesAsync(changes);

            Assert.True(File.Exists(managedPath));
            var content = await File.ReadAllTextAsync(managedPath);
            var lines = content.TrimEnd().Split('\n').Select(l => l.Trim()).Where(l => l.Length > 0).ToList();
            Assert.Contains(lines, l => l == DnsmasqConfKeys.StripMac);
        }
        finally
        {
            cache?.Dispose();
            if (Directory.Exists(dir))
                Directory.Delete(dir, recursive: true);
        }
    }

    [Fact]
    public async Task ApplyEffectiveConfigChangesAsync_StripSubnetFalse_RemovesFlagLine()
    {
        var dir = Path.Combine(Path.GetTempPath(), "dnsmasq-apply-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        var mainPath = Path.Combine(dir, "dnsmasq.conf");
        var managedName = "zz-managed.conf";
        var managedPath = Path.Combine(dir, managedName);
        ConfigSetCache? cache = null;
        try
        {
            File.WriteAllText(mainPath, "port=53\n");
            File.WriteAllText(managedPath, "strip-subnet\n");
            var options = Options.Create(new DnsmasqOptions { MainConfigPath = mainPath, ManagedFileName = managedName });
            cache = new ConfigSetCache(options, NullLogger<ConfigSetCache>.Instance);
            cache.Invalidate();
            var setService = new DnsmasqConfigSetService(cache);
            var configService = new DnsmasqConfigService(setService, cache, NullLogger<DnsmasqConfigService>.Instance);

            var changes = new List<PendingEffectiveConfigChange>
            {
                new(EffectiveConfigSections.SectionCache, DnsmasqConfKeys.StripSubnet, true, false, null)
            };
            await configService.ApplyEffectiveConfigChangesAsync(changes);

            var content = await File.ReadAllTextAsync(managedPath);
            var lines = content.TrimEnd().Split('\n').Select(l => l.Trim()).Where(l => l.Length > 0).ToList();
            Assert.DoesNotContain(lines, l => l == DnsmasqConfKeys.StripSubnet || l.StartsWith($"{DnsmasqConfKeys.StripSubnet}=", StringComparison.Ordinal));
        }
        finally
        {
            cache?.Dispose();
            if (Directory.Exists(dir))
                Directory.Delete(dir, recursive: true);
        }
    }

    [Fact]
    public async Task ApplyEffectiveConfigChangesAsync_DnssecCheckUnsignedNo_WritesEqualsNo()
    {
        var dir = Path.Combine(Path.GetTempPath(), "dnsmasq-apply-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        var mainPath = Path.Combine(dir, "dnsmasq.conf");
        var managedName = "zz-managed.conf";
        var managedPath = Path.Combine(dir, managedName);
        ConfigSetCache? cache = null;
        try
        {
            File.WriteAllText(mainPath, "port=53\n");
            var options = Options.Create(new DnsmasqOptions { MainConfigPath = mainPath, ManagedFileName = managedName });
            cache = new ConfigSetCache(options, NullLogger<ConfigSetCache>.Instance);
            var setService = new DnsmasqConfigSetService(cache);
            var configService = new DnsmasqConfigService(setService, cache, NullLogger<DnsmasqConfigService>.Instance);

            var changes = new List<PendingEffectiveConfigChange>
            {
                new(EffectiveConfigSections.SectionDnssec, DnsmasqConfKeys.DnssecCheckUnsigned, null, "no", null)
            };
            await configService.ApplyEffectiveConfigChangesAsync(changes);

            Assert.True(File.Exists(managedPath));
            var text = await File.ReadAllTextAsync(managedPath);
            Assert.Contains("dnssec-check-unsigned=no", text);
        }
        finally
        {
            cache?.Dispose();
            if (Directory.Exists(dir))
                Directory.Delete(dir, recursive: true);
        }
    }

    [Fact]
    public async Task ApplyEffectiveConfigChangesAsync_SingleValueNull_RemovesExistingLine()
    {
        var dir = Path.Combine(Path.GetTempPath(), "dnsmasq-apply-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        var mainPath = Path.Combine(dir, "dnsmasq.conf");
        var managedName = "zz-managed.conf";
        var managedPath = Path.Combine(dir, managedName);
        ConfigSetCache? cache = null;
        try
        {
            File.WriteAllText(mainPath, "port=53\n");
            File.WriteAllText(managedPath, "port=54\n");
            var options = Options.Create(new DnsmasqOptions { MainConfigPath = mainPath, ManagedFileName = managedName });
            cache = new ConfigSetCache(options, NullLogger<ConfigSetCache>.Instance);
            cache.Invalidate();
            var setService = new DnsmasqConfigSetService(cache);
            var configService = new DnsmasqConfigService(setService, cache, NullLogger<DnsmasqConfigService>.Instance);

            var changes = new List<PendingEffectiveConfigChange>
            {
                new(EffectiveConfigSections.SectionResolver, DnsmasqConfKeys.Port, "54", null, null)
            };
            await configService.ApplyEffectiveConfigChangesAsync(changes);

            var content = await File.ReadAllTextAsync(managedPath);
            var lines = content.TrimEnd().Split('\n').Select(l => l.Trim()).Where(l => l.Length > 0).ToList();
            Assert.DoesNotContain(lines, l => l == DnsmasqConfKeys.Port || l.StartsWith($"{DnsmasqConfKeys.Port}=", StringComparison.Ordinal));
        }
        finally
        {
            cache?.Dispose();
            if (Directory.Exists(dir))
                Directory.Delete(dir, recursive: true);
        }
    }

    [Fact]
    public async Task ApplyEffectiveConfigChangesAsync_KeyOnlyOrValue_KeyOnly_WritesBareKey()
    {
        var dir = Path.Combine(Path.GetTempPath(), "dnsmasq-apply-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        var mainPath = Path.Combine(dir, "dnsmasq.conf");
        var managedName = "zz-managed.conf";
        var managedPath = Path.Combine(dir, managedName);
        ConfigSetCache? cache = null;
        try
        {
            File.WriteAllText(mainPath, "port=53\n");
            var options = Options.Create(new DnsmasqOptions { MainConfigPath = mainPath, ManagedFileName = managedName });
            cache = new ConfigSetCache(options, NullLogger<ConfigSetCache>.Instance);
            var setService = new DnsmasqConfigSetService(cache);
            var configService = new DnsmasqConfigService(setService, cache, NullLogger<DnsmasqConfigService>.Instance);

            var changes = new List<PendingEffectiveConfigChange>
            {
                new(EffectiveConfigSections.SectionCache, DnsmasqConfKeys.UseStaleCache, null, "", null)
            };
            await configService.ApplyEffectiveConfigChangesAsync(changes);

            Assert.True(File.Exists(managedPath));
            var content = await File.ReadAllTextAsync(managedPath);
            Assert.Contains("use-stale-cache", content);
            Assert.DoesNotContain("use-stale-cache=", content);
        }
        finally
        {
            cache?.Dispose();
            if (Directory.Exists(dir))
                Directory.Delete(dir, recursive: true);
        }
    }

    [Fact]
    public async Task ApplyEffectiveConfigChangesAsync_InversePair_Enabled_WritesDo0x20Encode()
    {
        var dir = Path.Combine(Path.GetTempPath(), "dnsmasq-apply-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        var mainPath = Path.Combine(dir, "dnsmasq.conf");
        var managedName = "zz-managed.conf";
        var managedPath = Path.Combine(dir, managedName);
        ConfigSetCache? cache = null;
        try
        {
            File.WriteAllText(mainPath, "port=53\n");
            var options = Options.Create(new DnsmasqOptions { MainConfigPath = mainPath, ManagedFileName = managedName });
            cache = new ConfigSetCache(options, NullLogger<ConfigSetCache>.Instance);
            var setService = new DnsmasqConfigSetService(cache);
            var configService = new DnsmasqConfigService(setService, cache, NullLogger<DnsmasqConfigService>.Instance);

            var changes = new List<PendingEffectiveConfigChange>
            {
                new(EffectiveConfigSections.SectionResolver, DnsmasqConfKeys.Do0x20Encode, ExplicitToggleState.Default, ExplicitToggleState.Enabled, null)
            };
            await configService.ApplyEffectiveConfigChangesAsync(changes);

            Assert.True(File.Exists(managedPath));
            var content = await File.ReadAllTextAsync(managedPath);
            Assert.Contains("do-0x20-encode", content);
            Assert.DoesNotContain("no-0x20-encode", content);
        }
        finally
        {
            cache?.Dispose();
            if (Directory.Exists(dir))
                Directory.Delete(dir, recursive: true);
        }
    }

    [Fact]
    public async Task ApplyChanges_Do0x20_Default_RemovesBothLines()
    {
        var dir = Path.Combine(Path.GetTempPath(), "dnsmasq-apply-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        var mainPath = Path.Combine(dir, "dnsmasq.conf");
        var managedName = "zz-managed.conf";
        var managedPath = Path.Combine(dir, managedName);
        ConfigSetCache? cache = null;
        try
        {
            File.WriteAllText(mainPath, "port=53\n");
            File.WriteAllText(managedPath, "do-0x20-encode\nno-0x20-encode\n");
            var options = Options.Create(new DnsmasqOptions { MainConfigPath = mainPath, ManagedFileName = managedName });
            cache = new ConfigSetCache(options, NullLogger<ConfigSetCache>.Instance);
            cache.Invalidate();
            var setService = new DnsmasqConfigSetService(cache);
            var configService = new DnsmasqConfigService(setService, cache, NullLogger<DnsmasqConfigService>.Instance);

            var changes = new List<PendingEffectiveConfigChange>
            {
                new(EffectiveConfigSections.SectionResolver, DnsmasqConfKeys.Do0x20Encode, ExplicitToggleState.Disabled, ExplicitToggleState.Default, null)
            };
            await configService.ApplyEffectiveConfigChangesAsync(changes);

            var content = await File.ReadAllTextAsync(managedPath);
            var lines = content.TrimEnd().Split('\n').Select(l => l.Trim()).Where(l => l.Length > 0).ToList();
            Assert.DoesNotContain(lines, l => l == "do-0x20-encode" || l == "no-0x20-encode");
        }
        finally
        {
            cache?.Dispose();
            if (Directory.Exists(dir))
                Directory.Delete(dir, recursive: true);
        }
    }

    [Fact]
    public async Task ApplyChanges_Do0x20Enabled_RoundTripsToEffectiveState()
    {
        var dir = Path.Combine(Path.GetTempPath(), "dnsmasq-roundtrip-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        var mainPath = Path.Combine(dir, "dnsmasq.conf");
        var managedName = "zz-managed.conf";
        ConfigSetCache? cache = null;
        try
        {
            File.WriteAllText(mainPath, "port=53\n");
            var options = Options.Create(new DnsmasqOptions { MainConfigPath = mainPath, ManagedFileName = managedName });
            cache = new ConfigSetCache(options, NullLogger<ConfigSetCache>.Instance);
            var setService = new DnsmasqConfigSetService(cache);
            var configService = new DnsmasqConfigService(setService, cache, NullLogger<DnsmasqConfigService>.Instance);
            var changes = new List<PendingEffectiveConfigChange>
            {
                new(EffectiveConfigSections.SectionResolver, DnsmasqConfKeys.Do0x20Encode, ExplicitToggleState.Default, ExplicitToggleState.Enabled, null)
            };
            await configService.ApplyEffectiveConfigChangesAsync(changes);
            cache.Invalidate();
            var snapshot = await cache.GetSnapshotAsync();
            Assert.Equal(ExplicitToggleState.Enabled, snapshot.Config.Do0x20EncodeState);
            Assert.NotNull(snapshot.Sources.Do0x20Encode);
        }
        finally
        {
            cache?.Dispose();
            if (Directory.Exists(dir))
                Directory.Delete(dir, recursive: true);
        }
    }

    [Fact]
    public async Task ApplyChanges_UseStaleCache_RoundTripsToEffectiveState()
    {
        var dir = Path.Combine(Path.GetTempPath(), "dnsmasq-roundtrip-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        var mainPath = Path.Combine(dir, "dnsmasq.conf");
        var managedName = "zz-managed.conf";
        ConfigSetCache? cache = null;
        try
        {
            File.WriteAllText(mainPath, "port=53\n");
            var options = Options.Create(new DnsmasqOptions { MainConfigPath = mainPath, ManagedFileName = managedName });
            cache = new ConfigSetCache(options, NullLogger<ConfigSetCache>.Instance);
            var setService = new DnsmasqConfigSetService(cache);
            var configService = new DnsmasqConfigService(setService, cache, NullLogger<DnsmasqConfigService>.Instance);
            var changes = new List<PendingEffectiveConfigChange>
            {
                new(EffectiveConfigSections.SectionCache, DnsmasqConfKeys.UseStaleCache, null, "60", null)
            };
            await configService.ApplyEffectiveConfigChangesAsync(changes);
            cache.Invalidate();
            var snapshot = await cache.GetSnapshotAsync();
            Assert.Equal("60", snapshot.Config.UseStaleCache);
            Assert.NotNull(snapshot.Sources.UseStaleCache);
        }
        finally
        {
            cache?.Dispose();
            if (Directory.Exists(dir))
                Directory.Delete(dir, recursive: true);
        }
    }

    [Fact]
    public async Task ApplyChanges_Server_RoundTripsToEffectiveState()
    {
        var dir = Path.Combine(Path.GetTempPath(), "dnsmasq-roundtrip-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        var mainPath = Path.Combine(dir, "dnsmasq.conf");
        var managedName = "zz-managed.conf";
        ConfigSetCache? cache = null;
        try
        {
            File.WriteAllText(mainPath, "port=53\n");
            var options = Options.Create(new DnsmasqOptions { MainConfigPath = mainPath, ManagedFileName = managedName });
            cache = new ConfigSetCache(options, NullLogger<ConfigSetCache>.Instance);
            var setService = new DnsmasqConfigSetService(cache);
            var configService = new DnsmasqConfigService(setService, cache, NullLogger<DnsmasqConfigService>.Instance);
            var changes = new List<PendingEffectiveConfigChange>
            {
                new(EffectiveConfigSections.SectionResolver, DnsmasqConfKeys.Server, null, new List<string> { "1.1.1.1", "8.8.8.8" }, null)
            };
            await configService.ApplyEffectiveConfigChangesAsync(changes);
            cache.Invalidate();
            var snapshot = await cache.GetSnapshotAsync();
            Assert.Equal(2, snapshot.Config.ServerValues.Count);
            Assert.Contains("1.1.1.1", snapshot.Config.ServerValues);
            Assert.Contains("8.8.8.8", snapshot.Config.ServerValues);
            Assert.Equal(2, snapshot.Sources.ServerValues.Count);
        }
        finally
        {
            cache?.Dispose();
            if (Directory.Exists(dir))
                Directory.Delete(dir, recursive: true);
        }
    }

    [Fact]
    public async Task ApplyChanges_Conntrack_RoundTripsToEffectiveState()
    {
        var dir = Path.Combine(Path.GetTempPath(), "dnsmasq-roundtrip-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        var mainPath = Path.Combine(dir, "dnsmasq.conf");
        var managedName = "zz-managed.conf";
        ConfigSetCache? cache = null;
        try
        {
            File.WriteAllText(mainPath, "port=53\n");
            var options = Options.Create(new DnsmasqOptions { MainConfigPath = mainPath, ManagedFileName = managedName });
            cache = new ConfigSetCache(options, NullLogger<ConfigSetCache>.Instance);
            var setService = new DnsmasqConfigSetService(cache);
            var configService = new DnsmasqConfigService(setService, cache, NullLogger<DnsmasqConfigService>.Instance);
            var changes = new List<PendingEffectiveConfigChange>
            {
                new(EffectiveConfigSections.SectionResolver, DnsmasqConfKeys.Conntrack, false, true, null)
            };
            await configService.ApplyEffectiveConfigChangesAsync(changes);
            cache.Invalidate();
            var snapshot = await cache.GetSnapshotAsync();
            Assert.True(snapshot.Config.Conntrack);
            Assert.NotNull(snapshot.Sources.Conntrack);
        }
        finally
        {
            cache?.Dispose();
            if (Directory.Exists(dir))
                Directory.Delete(dir, recursive: true);
        }
    }

    [Fact]
    public async Task ApplyChanges_Do0x20_Disabled_WritesNo0x20Encode()
    {
        var dir = Path.Combine(Path.GetTempPath(), "dnsmasq-apply-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        var mainPath = Path.Combine(dir, "dnsmasq.conf");
        var managedName = "zz-managed.conf";
        var managedPath = Path.Combine(dir, managedName);
        ConfigSetCache? cache = null;
        try
        {
            File.WriteAllText(mainPath, "port=53\n");
            var options = Options.Create(new DnsmasqOptions { MainConfigPath = mainPath, ManagedFileName = managedName });
            cache = new ConfigSetCache(options, NullLogger<ConfigSetCache>.Instance);
            var setService = new DnsmasqConfigSetService(cache);
            var configService = new DnsmasqConfigService(setService, cache, NullLogger<DnsmasqConfigService>.Instance);

            var changes = new List<PendingEffectiveConfigChange>
            {
                new(EffectiveConfigSections.SectionResolver, DnsmasqConfKeys.Do0x20Encode, ExplicitToggleState.Default, ExplicitToggleState.Disabled, null)
            };
            await configService.ApplyEffectiveConfigChangesAsync(changes);

            var content = await File.ReadAllTextAsync(managedPath);
            Assert.Contains("no-0x20-encode", content);
            Assert.DoesNotContain("do-0x20-encode", content);
        }
        finally
        {
            cache?.Dispose();
            if (Directory.Exists(dir))
                Directory.Delete(dir, recursive: true);
        }
    }

    [Fact]
    public async Task ApplyChanges_UseStaleCache_Value_WritesKeyEqualsValue()
    {
        var dir = Path.Combine(Path.GetTempPath(), "dnsmasq-apply-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        var mainPath = Path.Combine(dir, "dnsmasq.conf");
        var managedName = "zz-managed.conf";
        var managedPath = Path.Combine(dir, managedName);
        ConfigSetCache? cache = null;
        try
        {
            File.WriteAllText(mainPath, "port=53\n");
            var options = Options.Create(new DnsmasqOptions { MainConfigPath = mainPath, ManagedFileName = managedName });
            cache = new ConfigSetCache(options, NullLogger<ConfigSetCache>.Instance);
            var setService = new DnsmasqConfigSetService(cache);
            var configService = new DnsmasqConfigService(setService, cache, NullLogger<DnsmasqConfigService>.Instance);

            var changes = new List<PendingEffectiveConfigChange>
            {
                new(EffectiveConfigSections.SectionCache, DnsmasqConfKeys.UseStaleCache, null, "60", null)
            };
            await configService.ApplyEffectiveConfigChangesAsync(changes);

            var content = await File.ReadAllTextAsync(managedPath);
            Assert.Contains("use-stale-cache=60", content);
        }
        finally
        {
            cache?.Dispose();
            if (Directory.Exists(dir))
                Directory.Delete(dir, recursive: true);
        }
    }

    [Fact]
    public async Task ApplyChanges_AddMac_KeyOnly_WritesBareKey()
    {
        var dir = Path.Combine(Path.GetTempPath(), "dnsmasq-apply-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        var mainPath = Path.Combine(dir, "dnsmasq.conf");
        var managedName = "zz-managed.conf";
        var managedPath = Path.Combine(dir, managedName);
        ConfigSetCache? cache = null;
        try
        {
            File.WriteAllText(mainPath, "port=53\n");
            var options = Options.Create(new DnsmasqOptions { MainConfigPath = mainPath, ManagedFileName = managedName });
            cache = new ConfigSetCache(options, NullLogger<ConfigSetCache>.Instance);
            var setService = new DnsmasqConfigSetService(cache);
            var configService = new DnsmasqConfigService(setService, cache, NullLogger<DnsmasqConfigService>.Instance);

            var changes = new List<PendingEffectiveConfigChange>
            {
                new(EffectiveConfigSections.SectionCache, DnsmasqConfKeys.AddMac, null, "", null)
            };
            await configService.ApplyEffectiveConfigChangesAsync(changes);

            var content = await File.ReadAllTextAsync(managedPath);
            Assert.Contains("add-mac", content);
            Assert.DoesNotContain("add-mac=", content);
        }
        finally
        {
            cache?.Dispose();
            if (Directory.Exists(dir))
                Directory.Delete(dir, recursive: true);
        }
    }

    [Fact]
    public async Task ApplyChanges_AddMac_ValueBase64_WritesKeyEqualsValue()
    {
        var dir = Path.Combine(Path.GetTempPath(), "dnsmasq-apply-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        var mainPath = Path.Combine(dir, "dnsmasq.conf");
        var managedName = "zz-managed.conf";
        var managedPath = Path.Combine(dir, managedName);
        ConfigSetCache? cache = null;
        try
        {
            File.WriteAllText(mainPath, "port=53\n");
            var options = Options.Create(new DnsmasqOptions { MainConfigPath = mainPath, ManagedFileName = managedName });
            cache = new ConfigSetCache(options, NullLogger<ConfigSetCache>.Instance);
            var setService = new DnsmasqConfigSetService(cache);
            var configService = new DnsmasqConfigService(setService, cache, NullLogger<DnsmasqConfigService>.Instance);

            var changes = new List<PendingEffectiveConfigChange>
            {
                new(EffectiveConfigSections.SectionCache, DnsmasqConfKeys.AddMac, null, "base64", null)
            };
            await configService.ApplyEffectiveConfigChangesAsync(changes);

            var content = await File.ReadAllTextAsync(managedPath);
            Assert.Contains("add-mac=base64", content);
        }
        finally
        {
            cache?.Dispose();
            if (Directory.Exists(dir))
                Directory.Delete(dir, recursive: true);
        }
    }

    [Fact]
    public async Task ApplyChanges_AddSubnet_KeyOnly_WritesBareKey()
    {
        var dir = Path.Combine(Path.GetTempPath(), "dnsmasq-apply-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        var mainPath = Path.Combine(dir, "dnsmasq.conf");
        var managedName = "zz-managed.conf";
        var managedPath = Path.Combine(dir, managedName);
        ConfigSetCache? cache = null;
        try
        {
            File.WriteAllText(mainPath, "port=53\n");
            var options = Options.Create(new DnsmasqOptions { MainConfigPath = mainPath, ManagedFileName = managedName });
            cache = new ConfigSetCache(options, NullLogger<ConfigSetCache>.Instance);
            var setService = new DnsmasqConfigSetService(cache);
            var configService = new DnsmasqConfigService(setService, cache, NullLogger<DnsmasqConfigService>.Instance);

            var changes = new List<PendingEffectiveConfigChange>
            {
                new(EffectiveConfigSections.SectionCache, DnsmasqConfKeys.AddSubnet, null, "", null)
            };
            await configService.ApplyEffectiveConfigChangesAsync(changes);

            var content = await File.ReadAllTextAsync(managedPath);
            Assert.Contains("add-subnet", content);
            Assert.DoesNotContain("add-subnet=", content);
        }
        finally
        {
            cache?.Dispose();
            if (Directory.Exists(dir))
                Directory.Delete(dir, recursive: true);
        }
    }

    [Fact]
    public async Task ApplyChanges_Umbrella_KeyOnly_WritesBareKey()
    {
        var dir = Path.Combine(Path.GetTempPath(), "dnsmasq-apply-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        var mainPath = Path.Combine(dir, "dnsmasq.conf");
        var managedName = "zz-managed.conf";
        var managedPath = Path.Combine(dir, managedName);
        ConfigSetCache? cache = null;
        try
        {
            File.WriteAllText(mainPath, "port=53\n");
            var options = Options.Create(new DnsmasqOptions { MainConfigPath = mainPath, ManagedFileName = managedName });
            cache = new ConfigSetCache(options, NullLogger<ConfigSetCache>.Instance);
            var setService = new DnsmasqConfigSetService(cache);
            var configService = new DnsmasqConfigService(setService, cache, NullLogger<DnsmasqConfigService>.Instance);

            var changes = new List<PendingEffectiveConfigChange>
            {
                new(EffectiveConfigSections.SectionCache, DnsmasqConfKeys.Umbrella, null, "", null)
            };
            await configService.ApplyEffectiveConfigChangesAsync(changes);

            var content = await File.ReadAllTextAsync(managedPath);
            Assert.Contains("umbrella", content);
            Assert.DoesNotContain("umbrella=", content);
        }
        finally
        {
            cache?.Dispose();
            if (Directory.Exists(dir))
                Directory.Delete(dir, recursive: true);
        }
    }

    [Fact]
    public async Task ApplyEffectiveConfigChangesAsync_Leasequery_MultiKeyOnlyOrValue_DoesNotDuplicateReadonlyValues()
    {
        var dir = Path.Combine(Path.GetTempPath(), "dnsmasq-apply-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        var mainPath = Path.Combine(dir, "dnsmasq.conf");
        var readonlyPath = Path.Combine(dir, "readonly.conf");
        var managedName = "zz-managed.conf";
        var managedPath = Path.Combine(dir, managedName);
        ConfigSetCache? cache = null;
        try
        {
            File.WriteAllText(mainPath, $"port=53\nconf-file={readonlyPath}\n");
            File.WriteAllText(readonlyPath, "leasequery\nleasequery=10.0.0.0/24\n");
            File.WriteAllText(managedPath, "leasequery=172.16.0.0/16\n");

            var options = Options.Create(new DnsmasqOptions { MainConfigPath = mainPath, ManagedFileName = managedName });
            cache = new ConfigSetCache(options, NullLogger<ConfigSetCache>.Instance);
            cache.Invalidate();
            var setService = new DnsmasqConfigSetService(cache);
            var configService = new DnsmasqConfigService(setService, cache, NullLogger<DnsmasqConfigService>.Instance);

            // Simulate UI effective values (readonly + managed). Save should write only managed-only values.
            var changes = new List<PendingEffectiveConfigChange>
            {
                new(
                    EffectiveConfigSections.SectionDhcp,
                    DnsmasqConfKeys.Leasequery,
                    null,
                    new List<string> { "", "10.0.0.0/24", "192.168.50.0/24" },
                    null)
            };

            await configService.ApplyEffectiveConfigChangesAsync(changes);

            var lines = (await File.ReadAllLinesAsync(managedPath))
                .Select(l => l.Trim())
                .Where(l => l.StartsWith("leasequery", StringComparison.Ordinal))
                .ToList();

            Assert.Single(lines);
            Assert.Equal("leasequery=192.168.50.0/24", lines[0]);
        }
        finally
        {
            cache?.Dispose();
            if (Directory.Exists(dir))
                Directory.Delete(dir, recursive: true);
        }
    }
}
