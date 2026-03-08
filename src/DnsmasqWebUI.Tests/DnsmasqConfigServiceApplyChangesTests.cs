using DnsmasqWebUI.Infrastructure.Helpers.Config;
using DnsmasqWebUI.Infrastructure.Services.Dnsmasq.Config;
using DnsmasqWebUI.Models.Config;
using DnsmasqWebUI.Models.Dnsmasq.EffectiveConfig;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace DnsmasqWebUI.Tests;

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
}
