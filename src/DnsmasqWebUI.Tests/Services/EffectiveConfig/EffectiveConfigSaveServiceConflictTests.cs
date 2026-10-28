using DnsmasqWebUI.Infrastructure.Services.Dnsmasq.Config;
using DnsmasqWebUI.Infrastructure.Services.Dnsmasq.Config.Abstractions;
using DnsmasqWebUI.Infrastructure.Services.Dnsmasq.Hosts.Abstractions;
using DnsmasqWebUI.Infrastructure.Services.Dnsmasq.Reload.Abstractions;
using DnsmasqWebUI.Infrastructure.Services.Dnsmasq.Validation.Abstractions;
using DnsmasqWebUI.Infrastructure.Services.Dnsmasq.Version.Abstractions;
using DnsmasqWebUI.Infrastructure.Services.EffectiveConfig;
using DnsmasqWebUI.Infrastructure.Services.EffectiveConfig.Metadata;
using DnsmasqWebUI.Infrastructure.Services.EffectiveConfig.Validation;
using DnsmasqWebUI.Infrastructure.Services.EffectiveConfig.Validation.Abstractions;
using DnsmasqWebUI.Models.Config;
using DnsmasqWebUI.Models.Contracts;
using DnsmasqWebUI.Models.Dhcp;
using DnsmasqWebUI.Models.Dnsmasq;
using DnsmasqWebUI.Models.Dnsmasq.EffectiveConfig;
using DnsmasqWebUI.Models.Hosts;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace DnsmasqWebUI.Tests.Services.EffectiveConfig;

/// <summary>
/// Optimistic concurrency coverage for the effective-config save path. These tests deliberately
/// prime a cache before changing disk state, proving the guard reads fresh source state rather than
/// trusting the browser/cache snapshot that created the draft.
/// </summary>
public sealed class EffectiveConfigSaveServiceConflictTests
{
    [Fact]
    public async Task SaveAsync_WhenEditedEffectiveOptionChangedOutsideDraft_BlocksBeforeWriteValidationOrReload()
    {
        var directory = CreateDirectory();
        ConfigSetCache? cache = null;
        try
        {
            var mainPath = Path.Combine(directory, "dnsmasq.conf");
            File.WriteAllText(mainPath, "port=53\n");
            cache = CreateCache(mainPath);
            var setService = new DnsmasqConfigSetService(cache);
            await setService.GetConfigSetAsync(); // Browser's original status/cache snapshot.

            // A local administrator edits the field while a browser draft is open.
            File.WriteAllText(mainPath, "port=5353\n");

            var configService = new RecordingConfigService();
            var hostsService = new RecordingHostsFileService();
            var validation = new CountingValidationService();
            var reload = new CountingReloadService();
            var service = CreateSaveService(setService, cache, configService, hostsService, validation, reload);

            var result = await service.SaveAsync(
            [
                new PendingOptionChange(
                    EffectiveConfigSections.SectionResolver,
                    DnsmasqConfKeys.Port,
                    53,
                    5354,
                    mainPath)
            ]);

            Assert.Equal(EffectiveConfigSaveResult.ErrorCodes.StaleDraftConflict, result.ErrorCode);
            Assert.False(result.Saved);
            Assert.Empty(result.Backups);
            Assert.Contains("port", result.UserMessage, StringComparison.Ordinal);
            Assert.Equal(0, configService.ApplyCallCount);
            Assert.Equal(0, hostsService.WriteCallCount);
            Assert.Equal(0, validation.CallCount);
            Assert.Equal(0, reload.CallCount);
            Assert.Equal("port=5353\n", File.ReadAllText(mainPath));
        }
        finally
        {
            cache?.Dispose();
            DeleteDirectory(directory);
        }
    }

    [Fact]
    public async Task SaveAsync_WhenManagedHostsChangedOutsideDraft_BlocksBeforeWriteValidationOrReload()
    {
        var directory = CreateDirectory();
        ConfigSetCache? cache = null;
        try
        {
            var mainPath = Path.Combine(directory, "dnsmasq.conf");
            File.WriteAllText(mainPath, "port=53\n");
            cache = CreateCache(mainPath);
            var setService = new DnsmasqConfigSetService(cache);
            var set = await setService.GetConfigSetAsync();
            var managedHostsPath = set.ManagedHostsFilePath!;
            File.WriteAllText(managedHostsPath, "10.0.0.1 alpha\n");

            var baseline = new List<HostEntry>
            {
                new() { Address = "10.0.0.1", Names = ["alpha"], RawLine = "10.0.0.1 alpha" }
            };
            var draft = new List<HostEntry>
            {
                new() { Address = "10.0.0.1", Names = ["alpha"], RawLine = "10.0.0.1 alpha" },
                new() { Address = "10.0.0.2", Names = ["beta"], RawLine = "10.0.0.2 beta" }
            };

            // This change happens after the Hosts page read the baseline above.
            File.WriteAllText(managedHostsPath, "10.0.0.99 alpha\n");

            var configService = new RecordingConfigService();
            var hostsService = new RecordingHostsFileService();
            var validation = new CountingValidationService();
            var reload = new CountingReloadService();
            var service = CreateSaveService(setService, cache, configService, hostsService, validation, reload);

            var result = await service.SaveAsync(
            [new PendingManagedHostsChange(baseline, draft, managedHostsPath)]);

            Assert.Equal(EffectiveConfigSaveResult.ErrorCodes.StaleDraftConflict, result.ErrorCode);
            Assert.False(result.Saved);
            Assert.Empty(result.Backups);
            Assert.Contains("Hosts", result.UserMessage, StringComparison.Ordinal);
            Assert.Equal(0, configService.ApplyCallCount);
            Assert.Equal(0, hostsService.WriteCallCount);
            Assert.Equal(0, validation.CallCount);
            Assert.Equal(0, reload.CallCount);
            Assert.Equal("10.0.0.99 alpha\n", File.ReadAllText(managedHostsPath));
        }
        finally
        {
            cache?.Dispose();
            DeleteDirectory(directory);
        }
    }

    [Fact]
    public async Task SaveAsync_WhenUnrelatedConfigFieldChanges_AllowsCurrentDraftToSave()
    {
        var directory = CreateDirectory();
        ConfigSetCache? cache = null;
        try
        {
            var mainPath = Path.Combine(directory, "dnsmasq.conf");
            File.WriteAllText(mainPath, "port=53\ncache-size=100\n");
            cache = CreateCache(mainPath);
            var setService = new DnsmasqConfigSetService(cache);
            await setService.GetConfigSetAsync();

            // The app's writer only changes port, so a cache-size edit remains safe to preserve.
            File.WriteAllText(mainPath, "port=53\ncache-size=200\n");

            var configService = new RecordingConfigService();
            var hostsService = new RecordingHostsFileService();
            var validation = new CountingValidationService();
            var reload = new CountingReloadService();
            var service = CreateSaveService(setService, cache, configService, hostsService, validation, reload);

            var result = await service.SaveAsync(
            [
                new PendingOptionChange(
                    EffectiveConfigSections.SectionResolver,
                    DnsmasqConfKeys.Port,
                    53,
                    5353,
                    mainPath)
            ]);

            Assert.Null(result.ErrorCode);
            Assert.True(result.Saved);
            Assert.True(result.Validated);
            Assert.True(result.Restarted);
            Assert.Equal(1, configService.ApplyCallCount);
            Assert.Equal(0, hostsService.WriteCallCount);
            Assert.Equal(1, validation.CallCount);
            Assert.Equal(1, reload.CallCount);
            Assert.Equal(1, reload.RestartCallCount);
            Assert.Equal(0, reload.ReloadCallCount);
        }
        finally
        {
            cache?.Dispose();
            DeleteDirectory(directory);
        }
    }

    private static string CreateDirectory()
    {
        var directory = Path.Combine(Path.GetTempPath(), "dnsmasq-save-conflict-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        return directory;
    }

    private static void DeleteDirectory(string directory)
    {
        if (Directory.Exists(directory))
            Directory.Delete(directory, recursive: true);
    }

    private static ConfigSetCache CreateCache(string mainPath) =>
        new(
            Options.Create(new DnsmasqOptions
            {
                MainConfigPath = mainPath,
                ManagedFileName = "zz-dnsmasq-webui.conf",
                ManagedHostsFileName = "zz-dnsmasq-webui.hosts"
            }),
            NullLogger<ConfigSetCache>.Instance);

    private static EffectiveConfigSaveService CreateSaveService(
        IDnsmasqConfigSetService setService,
        IConfigSetCache cache,
        IDnsmasqConfigService configService,
        IHostsFileService hostsService,
        IConfigValidationService validation,
        IReloadService reload) =>
        new(
            setService,
            configService,
            cache,
            hostsService,
            validation,
            new EffectiveConfigSemanticValidationService(new OptionSemanticValidator(Array.Empty<IOptionSemanticHandler>())),
            reload,
            new SupportedVersionService(),
            NullLogger<EffectiveConfigSaveService>.Instance);

    private sealed class RecordingConfigService : IDnsmasqConfigService
    {
        public int ApplyCallCount { get; private set; }

        public Task ApplyEffectiveConfigChangesAsync(IReadOnlyList<PendingEffectiveConfigChange> changes, CancellationToken ct = default)
        {
            ApplyCallCount++;
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<DhcpHostEntry>> ReadDhcpHostsAsync(CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<DhcpHostEntry>>(Array.Empty<DhcpHostEntry>());

        public Task WriteDhcpHostsAsync(IReadOnlyList<DhcpHostEntry> entries, CancellationToken ct = default) => Task.CompletedTask;

        public Task<ManagedConfigContent> ReadManagedConfigAsync(CancellationToken ct = default) =>
            Task.FromResult(new ManagedConfigContent(Array.Empty<DnsmasqConfLine>(), ""));

        public Task WriteManagedConfigAsync(IReadOnlyList<DnsmasqConfLine> lines, CancellationToken ct = default) => Task.CompletedTask;
    }

    private sealed class RecordingHostsFileService : IHostsFileService
    {
        public int WriteCallCount { get; private set; }

        public Task<IReadOnlyList<HostEntry>> ReadAsync(CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<HostEntry>>(Array.Empty<HostEntry>());

        public Task WriteAsync(IReadOnlyList<HostEntry> entries, CancellationToken ct = default)
        {
            WriteCallCount++;
            return Task.CompletedTask;
        }
    }

    private sealed class CountingValidationService : IConfigValidationService
    {
        public int CallCount { get; private set; }

        public Task<ConfigValidationResult> ValidateAsync(CancellationToken ct = default)
        {
            CallCount++;
            return Task.FromResult(new ConfigValidationResult(true, true, 0, null, null, null));
        }
    }

    private sealed class CountingReloadService : IReloadService
    {
        public int CallCount { get; private set; }
        public int RestartCallCount { get; private set; }
        public int ReloadCallCount { get; private set; }
        public bool RestartCommandConfigured => true;
        public bool ReloadCommandConfigured => true;

        public Task<ReloadResult> ReloadAsync(CancellationToken ct = default)
        {
            CallCount++;
            ReloadCallCount++;
            return Task.FromResult(new ReloadResult(true, 0, null, null));
        }

        public Task<ReloadResult> RestartAsync(CancellationToken ct = default)
        {
            CallCount++;
            RestartCallCount++;
            return Task.FromResult(new ReloadResult(true, 0, null, null));
        }
    }

    private sealed class SupportedVersionService : IDnsmasqVersionService
    {
        private static readonly DnsmasqVersionInfo Version = new(
            new Version(2, 91),
            new Version(2, 91),
            ProbeSucceeded: true,
            IsSupported: true,
            "dnsmasq --version",
            null,
            new DnsmasqCompileCapabilities(true, true, true, false, new HashSet<string>(StringComparer.OrdinalIgnoreCase)));

        public Task<DnsmasqVersionInfo> GetVersionInfoAsync(CancellationToken ct = default) => Task.FromResult(Version);
    }
}
