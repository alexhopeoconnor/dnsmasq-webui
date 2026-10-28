using DnsmasqWebUI.Infrastructure.Services.Dnsmasq.Config;
using DnsmasqWebUI.Infrastructure.Services.Dnsmasq.Hosts;
using DnsmasqWebUI.Infrastructure.Services.Dnsmasq.Hosts.Abstractions;
using DnsmasqWebUI.Models.Config;
using DnsmasqWebUI.Models.Contracts;
using DnsmasqWebUI.Models.Hosts;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace DnsmasqWebUI.Tests.Services.Hosts;

public sealed class HostsFileServiceTests
{
    [Fact]
    public async Task WriteAsync_rejects_invalid_structured_entries_before_touching_the_managed_file()
    {
        var fixture = CreateFixture();
        try
        {
            var entry = new HostEntry
            {
                Address = "192.0.2.1",
                Names = new List<string> { "two words" }
            };

            var exception = await Assert.ThrowsAsync<ArgumentException>(() => fixture.Service.WriteAsync(new[] { entry }));

            Assert.Contains("whitespace", exception.Message, StringComparison.OrdinalIgnoreCase);
            Assert.False(File.Exists(fixture.ManagedHostsPath));
            Assert.Null(fixture.Cache.LastNotifiedEntries);
        }
        finally
        {
            fixture.Dispose();
        }
    }

    [Fact]
    public async Task WriteAsync_writes_valid_entries_and_notifies_the_cache_only_after_replacement()
    {
        var fixture = CreateFixture();
        try
        {
            var entries = new[]
            {
                new HostEntry
                {
                    Address = "192.0.2.1",
                    Names = new List<string> { "router", "gateway" },
                    InlineComment = "managed locally"
                }
            };

            await fixture.Service.WriteAsync(entries);

            Assert.Equal("192.0.2.1 router gateway # managed locally" + Environment.NewLine, await File.ReadAllTextAsync(fixture.ManagedHostsPath));
            Assert.Same(entries, fixture.Cache.LastNotifiedEntries);
            Assert.Empty(Directory.GetFiles(fixture.Directory, ".zz-managed.hosts.*.tmp"));
        }
        finally
        {
            fixture.Dispose();
        }
    }

    private static HostsFileFixture CreateFixture()
    {
        var directory = Path.Combine(Path.GetTempPath(), "dnsmasq-hosts-write-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        var mainConfigPath = Path.Combine(directory, "dnsmasq.conf");
        var managedHostsPath = Path.Combine(directory, "zz-managed.hosts");
        File.WriteAllText(mainConfigPath, "port=53\n");

        var options = Options.Create(new DnsmasqOptions
        {
            MainConfigPath = mainConfigPath,
            ManagedFileName = "zz-managed.conf",
            ManagedHostsFileName = Path.GetFileName(managedHostsPath)
        });
        var configCache = new ConfigSetCache(options, NullLogger<ConfigSetCache>.Instance);
        var recordingCache = new RecordingHostsCache();
        var service = new HostsFileService(
            new DnsmasqConfigSetService(configCache),
            recordingCache,
            NullLogger<HostsFileService>.Instance);
        return new HostsFileFixture(directory, managedHostsPath, configCache, recordingCache, service);
    }

    private sealed class HostsFileFixture : IDisposable
    {
        public HostsFileFixture(
            string directory,
            string managedHostsPath,
            ConfigSetCache configCache,
            RecordingHostsCache cache,
            HostsFileService service)
        {
            Directory = directory;
            ManagedHostsPath = managedHostsPath;
            ConfigCache = configCache;
            Cache = cache;
            Service = service;
        }

        public string Directory { get; }
        public string ManagedHostsPath { get; }
        public ConfigSetCache ConfigCache { get; }
        public RecordingHostsCache Cache { get; }
        public HostsFileService Service { get; }

        public void Dispose()
        {
            ConfigCache.Dispose();
            if (System.IO.Directory.Exists(Directory))
                System.IO.Directory.Delete(Directory, recursive: true);
        }
    }

    private sealed class RecordingHostsCache : IHostsCache
    {
        public IReadOnlyList<HostEntry>? LastNotifiedEntries { get; private set; }

        public Task<HostsSnapshot> GetSnapshotAsync(CancellationToken ct = default) =>
            throw new NotSupportedException();

        public Task<IReadOnlyList<HostSource>> GetSourcesAsync(CancellationToken ct = default) =>
            throw new NotSupportedException();

        public void Invalidate()
        {
        }

        public void NotifyWeWroteManagedHosts(IReadOnlyList<HostEntry> entries) => LastNotifiedEntries = entries;

        public Task<IReadOnlyList<HostsPageRow>> GetUnifiedRowsAsync(
            bool expandHosts,
            bool noHosts,
            string? managedHostsPath,
            CancellationToken ct = default) =>
            throw new NotSupportedException();

        public Task<IReadOnlyList<HostsPageRow>> GetUnifiedRowsAsync(CancellationToken ct = default) =>
            throw new NotSupportedException();
    }
}
