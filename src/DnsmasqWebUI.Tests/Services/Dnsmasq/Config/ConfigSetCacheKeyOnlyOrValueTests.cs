using DnsmasqWebUI.Infrastructure.Services.EffectiveConfig.Metadata;
using DnsmasqWebUI.Infrastructure.Services.Dnsmasq.Config;
using DnsmasqWebUI.Models.Config;
using DnsmasqWebUI.Models.Dnsmasq.EffectiveConfig;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace DnsmasqWebUI.Tests.Services.Dnsmasq.Config;

public class ConfigSetCacheKeyOnlyOrValueTests
{
    [Fact]
    public void KeyOnlyOrValueOptions_CoverEveryRegisteredOption()
    {
        var registered = typeof(DnsmasqConfKeys)
            .GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)
            .Select(field => field.GetRawConstantValue() as string)
            .Where(key => key is not null && EffectiveConfigWriteSemantics.GetBehavior(key) == EffectiveConfigWriteBehavior.KeyOnlyOrValue)
            .Select(key => key!)
            .ToHashSet(StringComparer.Ordinal);
        var covered = KeyOnlyOrValueOptions()
            .Select(data => ((KeyOnlyOrValueOption)data[0]).Key)
            .ToHashSet(StringComparer.Ordinal);

        Assert.True(registered.SetEquals(covered));
    }

    public static IEnumerable<object[]> KeyOnlyOrValueOptions()
    {
        yield return [new KeyOnlyOrValueOption(DnsmasqConfKeys.LogQueries, "extra", config => config.LogQueries, sources => sources.LogQueries)];
        yield return [new KeyOnlyOrValueOption(DnsmasqConfKeys.UseStaleCache, "60", config => config.UseStaleCache, sources => sources.UseStaleCache)];
        yield return [new KeyOnlyOrValueOption(DnsmasqConfKeys.AddMac, "base64", config => config.AddMac, sources => sources.AddMac)];
        yield return [new KeyOnlyOrValueOption(DnsmasqConfKeys.AddSubnet, "24,64", config => config.AddSubnet, sources => sources.AddSubnet)];
        yield return [new KeyOnlyOrValueOption(DnsmasqConfKeys.Umbrella, "example", config => config.Umbrella, sources => sources.Umbrella)];
        yield return [new KeyOnlyOrValueOption(DnsmasqConfKeys.ConnmarkAllowlistEnable, "0xff", config => config.ConnmarkAllowlistEnable, sources => sources.ConnmarkAllowlistEnable)];
        yield return [new KeyOnlyOrValueOption(DnsmasqConfKeys.DnssecCheckUnsigned, "no", config => config.DnssecCheckUnsigned, sources => sources.DnssecCheckUnsigned)];
        yield return [new KeyOnlyOrValueOption(DnsmasqConfKeys.DhcpGenerateNames, "tag:alpha", config => config.DhcpGenerateNames, sources => sources.DhcpGenerateNames)];
        yield return [new KeyOnlyOrValueOption(DnsmasqConfKeys.DhcpBroadcast, "tag:beta", config => config.DhcpBroadcast, sources => sources.DhcpBroadcast)];
        yield return [new KeyOnlyOrValueOption(DnsmasqConfKeys.BootpDynamic, "tag:gamma", config => config.BootpDynamic, sources => sources.BootpDynamic)];
    }

    [Theory]
    [MemberData(nameof(KeyOnlyOrValueOptions))]
    public async Task GetSnapshot_KeyOnlyOrValueAbsent_IsNullAndHasNoSource(KeyOnlyOrValueOption option)
    {
        var (dir, _, _, cache) = CreateCache("port=53\n", "");
        try
        {
            var snapshot = await cache.GetSnapshotAsync();

            Assert.Null(option.GetValue(snapshot.Config));
            Assert.Null(option.GetSource(snapshot.Sources));
        }
        finally
        {
            cache.Dispose();
            Directory.Delete(dir, recursive: true);
        }
    }

    [Theory]
    [MemberData(nameof(KeyOnlyOrValueOptions))]
    public async Task GetSnapshot_KeyOnlyOrValueManagedBare_OverridesValueAsEmptyAndKeepsItsSource(KeyOnlyOrValueOption option)
    {
        var (dir, _, managedPath, cache) = CreateCache($"port=53\n{option.Key}={option.Value}\n", $"{option.Key}\n");
        try
        {
            var snapshot = await cache.GetSnapshotAsync();

            Assert.Equal("", option.GetValue(snapshot.Config));
            AssertManagedSource(managedPath, option.GetSource(snapshot.Sources));
        }
        finally
        {
            cache.Dispose();
            Directory.Delete(dir, recursive: true);
        }
    }

    [Theory]
    [MemberData(nameof(KeyOnlyOrValueOptions))]
    public async Task GetSnapshot_KeyOnlyOrValueManagedValue_OverridesBareAndKeepsItsSource(KeyOnlyOrValueOption option)
    {
        var (dir, _, managedPath, cache) = CreateCache($"port=53\n{option.Key}\n", $"{option.Key}={option.Value}\n");
        try
        {
            var snapshot = await cache.GetSnapshotAsync();

            Assert.Equal(option.Value, option.GetValue(snapshot.Config));
            AssertManagedSource(managedPath, option.GetSource(snapshot.Sources));
        }
        finally
        {
            cache.Dispose();
            Directory.Delete(dir, recursive: true);
        }
    }

    private static (string Dir, string MainPath, string ManagedPath, ConfigSetCache Cache) CreateCache(
        string mainContent,
        string managedContent)
    {
        var dir = Path.Combine(Path.GetTempPath(), "dnsmasq-key-only-or-value-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        var mainPath = Path.Combine(dir, "dnsmasq.conf");
        const string managedName = "zz-managed.conf";
        var managedPath = Path.Combine(dir, managedName);
        File.WriteAllText(mainPath, mainContent);
        File.WriteAllText(managedPath, managedContent);
        var cache = new ConfigSetCache(
            Options.Create(new DnsmasqOptions { MainConfigPath = mainPath, ManagedFileName = managedName }),
            NullLogger<ConfigSetCache>.Instance);
        return (dir, mainPath, managedPath, cache);
    }

    private static void AssertManagedSource(string managedPath, ConfigValueSource? source)
    {
        Assert.NotNull(source);
        Assert.True(source.IsManaged);
        Assert.Equal(Path.GetFullPath(managedPath), source.FilePath, StringComparer.OrdinalIgnoreCase);
        Assert.Equal(1, source.LineNumber);
    }

    public sealed record KeyOnlyOrValueOption(
        string Key,
        string Value,
        Func<EffectiveDnsmasqConfig, string?> GetValue,
        Func<EffectiveConfigSources, ConfigValueSource?> GetSource);
}
