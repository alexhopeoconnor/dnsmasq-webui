using DnsmasqWebUI.Infrastructure.Services.EffectiveConfig.Metadata;
using DnsmasqWebUI.Infrastructure.Services.Dnsmasq.Config;
using DnsmasqWebUI.Models.Config;
using DnsmasqWebUI.Models.Contracts;
using DnsmasqWebUI.Models.Dnsmasq.EffectiveConfig;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace DnsmasqWebUI.Tests.Services.Dnsmasq.Config;

public class LeasequeryBareDirectiveTests
{
    [Fact]
    public async Task ManagedBareLeasequery_CachesSourceAndRoundTripsThroughWriter()
    {
        var dir = Path.Combine(Path.GetTempPath(), "dnsmasq-leasequery-bare-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        var mainPath = Path.Combine(dir, "dnsmasq.conf");
        const string managedName = "zz-managed.conf";
        var managedPath = Path.Combine(dir, managedName);
        ConfigSetCache? cache = null;
        try
        {
            File.WriteAllText(mainPath, "port=53\n");
            File.WriteAllText(managedPath, "leasequery\n");
            cache = new ConfigSetCache(
                Options.Create(new DnsmasqOptions { MainConfigPath = mainPath, ManagedFileName = managedName }),
                NullLogger<ConfigSetCache>.Instance);

            var before = await cache.GetSnapshotAsync();
            AssertBareManagedLeasequery(before, managedPath, expectedLineNumber: 1);

            var service = new DnsmasqConfigService(
                new DnsmasqConfigSetService(cache),
                cache,
                NullLogger<DnsmasqConfigService>.Instance);
            await service.ApplyEffectiveConfigChangesAsync(
            [
                new PendingEffectiveConfigChange(
                    EffectiveConfigSections.SectionDhcp,
                    DnsmasqConfKeys.Leasequery,
                    before.Config.LeasequeryValues,
                    new List<string> { "" })
            ]);

            var leasequeryLines = (await File.ReadAllLinesAsync(managedPath))
                .Select(line => line.Trim())
                .Where(line => line.StartsWith(DnsmasqConfKeys.Leasequery, StringComparison.Ordinal))
                .ToList();
            Assert.Equal([DnsmasqConfKeys.Leasequery], leasequeryLines);

            var after = await cache.GetSnapshotAsync();
            AssertBareManagedLeasequery(after, managedPath, expectedLineNumber: 2);
        }
        finally
        {
            cache?.Dispose();
            if (Directory.Exists(dir))
                Directory.Delete(dir, recursive: true);
        }
    }

    private static void AssertBareManagedLeasequery(
        ConfigSetSnapshot snapshot,
        string managedPath,
        int expectedLineNumber)
    {
        Assert.Equal([""], snapshot.Config.LeasequeryValues);
        var value = Assert.Single(snapshot.Sources.LeasequeryValues);
        Assert.Equal("", value.Value);
        Assert.NotNull(value.Source);
        Assert.True(value.Source.IsManaged);
        Assert.Equal(Path.GetFullPath(managedPath), value.Source.FilePath, StringComparer.OrdinalIgnoreCase);
        Assert.Equal(expectedLineNumber, value.Source.LineNumber);
    }
}
