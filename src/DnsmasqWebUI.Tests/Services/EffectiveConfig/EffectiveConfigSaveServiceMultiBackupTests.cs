using DnsmasqWebUI.Infrastructure.Services.Dnsmasq.Config;
using DnsmasqWebUI.Infrastructure.Services.Dnsmasq.Hosts.Abstractions;
using DnsmasqWebUI.Infrastructure.Services.Dnsmasq.Reload.Abstractions;
using DnsmasqWebUI.Infrastructure.Services.Dnsmasq.Validation.Abstractions;
using DnsmasqWebUI.Infrastructure.Services.Dnsmasq.Version.Abstractions;
using DnsmasqWebUI.Models.Dnsmasq.EffectiveConfig;
using DnsmasqWebUI.Models.Hosts;
using DnsmasqWebUI.Infrastructure.Services.Dnsmasq.Config.Abstractions;
using DnsmasqWebUI.Infrastructure.Services.EffectiveConfig;
using DnsmasqWebUI.Infrastructure.Services.EffectiveConfig.Metadata;
using DnsmasqWebUI.Infrastructure.Services.EffectiveConfig.Validation;
using DnsmasqWebUI.Infrastructure.Services.EffectiveConfig.Validation.Abstractions;
using DnsmasqWebUI.Models.Config;
using DnsmasqWebUI.Models.Contracts;
using DnsmasqWebUI.Models.Dhcp;
using DnsmasqWebUI.Models.Dnsmasq;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace DnsmasqWebUI.Tests.Services.EffectiveConfig;

/// <summary>
/// Tests for multi-file backup/restore and managed-hosts-only save.
/// </summary>
public class EffectiveConfigSaveServiceMultiBackupTests
{
    [Fact]
    public async Task SaveAsync_WhenManagedConfigChangesAndOnlyReloadIsConfigured_BlocksBeforeWriting()
    {
        var dir = Path.Combine(Path.GetTempPath(), "dnsmasq-restart-contract-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        ConfigSetCache? cache = null;
        try
        {
            var mainPath = Path.Combine(dir, "dnsmasq.conf");
            File.WriteAllText(mainPath, "port=53\n");
            var options = Options.Create(new DnsmasqOptions
            {
                MainConfigPath = mainPath,
                ManagedFileName = "zz-managed.conf",
                ManagedHostsFileName = "zz-dnsmasq-webui.hosts"
            });
            cache = new ConfigSetCache(options, NullLogger<ConfigSetCache>.Instance);
            var setService = new DnsmasqConfigSetService(cache);
            var set = await setService.GetConfigSetAsync();
            var config = new StubConfigService();
            var validation = new StubValidationService(success: true);
            var reload = new StubReloadService(
                success: true,
                restartCommandConfigured: false,
                reloadCommandConfigured: true);
            var saveService = new EffectiveConfigSaveService(
                setService,
                config,
                cache,
                new StubHostsFileService(),
                validation,
                new EffectiveConfigSemanticValidationService(new OptionSemanticValidator(Array.Empty<IOptionSemanticHandler>())),
                reload,
                new StubVersionService(CreateSupportedVersionInfo()),
                NullLogger<EffectiveConfigSaveService>.Instance);

            var result = await saveService.SaveAsync(
            [
                new PendingOptionChange(
                    EffectiveConfigSections.SectionResolver,
                    DnsmasqConfKeys.Port,
                    "53",
                    "5353",
                    mainPath)
            ]);

            Assert.False(result.Saved);
            Assert.Equal(EffectiveConfigSaveResult.ErrorCodes.RestartCommandRequired, result.ErrorCode);
            Assert.Contains("RestartCommand", result.UserMessage, StringComparison.Ordinal);
            Assert.Empty(result.Backups);
            Assert.Equal(0, config.ApplyCalls);
            Assert.Equal(0, validation.Calls);
            Assert.Equal(0, reload.RestartCalls);
            Assert.Equal(0, reload.ReloadCalls);
            Assert.False(File.Exists(set.ManagedFilePath!));
        }
        finally
        {
            cache?.Dispose();
            if (Directory.Exists(dir))
                Directory.Delete(dir, recursive: true);
        }
    }

    [Fact]
    public async Task SaveAsync_WhenOnlyManagedHostsChange_BacksUpManagedHostsFile_WritesHosts_ValidatesAndReloads()
    {
        var dir = Path.Combine(Path.GetTempPath(), "dnsmasq-hosts-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        var mainPath = Path.Combine(dir, "dnsmasq.conf");
        ConfigSetCache? cache = null;
        try
        {
            File.WriteAllText(mainPath, "port=53\n");
            var options = Options.Create(new DnsmasqOptions
            {
                MainConfigPath = mainPath,
                ManagedFileName = "zz-managed.conf",
                ManagedHostsFileName = "zz-dnsmasq-webui.hosts"
            });
            cache = new ConfigSetCache(options, NullLogger<ConfigSetCache>.Instance);
            var setService = new DnsmasqConfigSetService(cache);
            var configService = new DnsmasqConfigService(setService, cache, NullLogger<DnsmasqConfigService>.Instance);

            var set = await setService.GetConfigSetAsync();
            var managedHostsPath = set.ManagedHostsFilePath!;
            File.WriteAllText(managedHostsPath, "192.168.1.1 host1\n");

            var writtenEntries = new List<IReadOnlyList<HostEntry>>();
            var capturingHostsService = new CapturingHostsFileService(writtenEntries);

            var versionService = new StubVersionService(CreateSupportedVersionInfo());
            var semanticValidationService = new EffectiveConfigSemanticValidationService(new OptionSemanticValidator(Array.Empty<IOptionSemanticHandler>()));
            var reload = new StubReloadService(success: true);
            var saveService = new EffectiveConfigSaveService(
                setService,
                configService,
                cache,
                capturingHostsService,
                new StubValidationService(success: true),
                semanticValidationService,
                reload,
                versionService,
                NullLogger<EffectiveConfigSaveService>.Instance);

            var oldEntries = new List<HostEntry> { new() { Address = "192.168.1.1", Names = new List<string> { "host1" }, Id = "192.168.1.1|host1" } };
            var newEntries = new List<HostEntry>
            {
                new() { Address = "192.168.1.1", Names = new List<string> { "host1" }, Id = "192.168.1.1|host1" },
                new() { Address = "192.168.1.2", Names = new List<string> { "host2" }, Id = "192.168.1.2|host2" }
            };
            var changes = new List<PendingDnsmasqChange>
            {
                new PendingManagedHostsChange(oldEntries, newEntries, managedHostsPath)
            };

            var result = await saveService.SaveAsync(changes);

            Assert.True(result.Saved);
            Assert.True(result.Validated);
            Assert.False(result.Restarted);
            Assert.True(result.Reloaded);
            Assert.Null(result.ErrorCode);
            Assert.Single(result.Backups);
            Assert.Equal(DnsmasqManagedTargetKind.ManagedHosts, result.Backups[0].Kind);
            Assert.Equal(managedHostsPath, result.Backups[0].TargetPath);
            Assert.True(File.Exists(result.Backups[0].BackupPath));
            Assert.Single(writtenEntries);
            Assert.Equal(2, writtenEntries[0].Count);
            Assert.Equal(1, reload.ReloadCalls);
            Assert.Equal(0, reload.RestartCalls);
        }
        finally
        {
            cache?.Dispose();
            if (Directory.Exists(dir))
                Directory.Delete(dir, recursive: true);
        }
    }

    [Fact]
    public async Task RestoreAsync_WhenBackupsContainTwoTargets_RestoresBothThenReloads()
    {
        var dir = Path.Combine(Path.GetTempPath(), "dnsmasq-restore-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        var mainPath = Path.Combine(dir, "dnsmasq.conf");
        File.WriteAllText(mainPath, "port=53\n");
        var target1 = Path.Combine(dir, "target1.conf");
        var target2 = Path.Combine(dir, "target2.hosts");
        var backup1 = Path.Combine(dir, "backup1.bak");
        var backup2 = Path.Combine(dir, "backup2.bak");
        File.WriteAllText(target1, "original1");
        File.WriteAllText(target2, "original2");
        File.WriteAllText(backup1, "restored1");
        File.WriteAllText(backup2, "restored2");

        var backups = new List<DnsmasqManagedBackup>
        {
            new(DnsmasqManagedTargetKind.ManagedConfig, target1, backup1),
            new(DnsmasqManagedTargetKind.ManagedHosts, target2, backup2)
        };

        var options = Options.Create(new DnsmasqOptions { MainConfigPath = mainPath, ManagedFileName = "x.conf" });
        var cache = new ConfigSetCache(options, NullLogger<ConfigSetCache>.Instance);
        var setService = new DnsmasqConfigSetService(cache);
        try
        {
            var reload = new StubReloadService(success: true);
            var saveService = new EffectiveConfigSaveService(
                setService,
                new StubConfigService(),
                cache,
                new StubHostsFileService(),
                new StubValidationService(success: true),
                new EffectiveConfigSemanticValidationService(new OptionSemanticValidator(Array.Empty<IOptionSemanticHandler>())),
                reload,
                new StubVersionService(CreateSupportedVersionInfo()),
                NullLogger<EffectiveConfigSaveService>.Instance);

            var result = await saveService.RestoreAsync(backups);

            Assert.True(result.Restored);
            Assert.True(result.Restarted);
            Assert.False(result.Reloaded);
            Assert.Equal("restored1", File.ReadAllText(target1));
            Assert.Equal("restored2", File.ReadAllText(target2));
            Assert.Equal(1, reload.RestartCalls);
            Assert.Equal(0, reload.ReloadCalls);
        }
        finally
        {
            cache.Dispose();
            if (Directory.Exists(dir))
                Directory.Delete(dir, recursive: true);
        }
    }

    private static DnsmasqVersionInfo CreateSupportedVersionInfo()
    {
        var capabilities = new DnsmasqCompileCapabilities(
            Dhcp: true, Tftp: true, Dnssec: true, Dbus: false,
            new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "DHCP", "TFTP", "DNSSEC" });
        return new DnsmasqVersionInfo(
            new Version(2, 91), new Version(2, 91),
            ProbeSucceeded: true, IsSupported: true, "dnsmasq --version", null, capabilities);
    }

    private sealed class CapturingHostsFileService : IHostsFileService
    {
        private readonly List<IReadOnlyList<HostEntry>> _written;

        public CapturingHostsFileService(List<IReadOnlyList<HostEntry>> written) => _written = written;

        public Task<IReadOnlyList<HostEntry>> ReadAsync(CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<HostEntry>>(new List<HostEntry>());

        public Task WriteAsync(IReadOnlyList<HostEntry> entries, CancellationToken ct = default)
        {
            _written.Add(entries);
            return Task.CompletedTask;
        }
    }

    private sealed class StubVersionService : IDnsmasqVersionService
    {
        private readonly DnsmasqVersionInfo _info;
        public StubVersionService(DnsmasqVersionInfo info) => _info = info;
        public Task<DnsmasqVersionInfo> GetVersionInfoAsync(CancellationToken ct = default) => Task.FromResult(_info);
    }

    private sealed class StubValidationService : IConfigValidationService
    {
        private readonly bool _success;
        public StubValidationService(bool success) => _success = success;
        public int Calls { get; private set; }
        public Task<ConfigValidationResult> ValidateAsync(CancellationToken ct = default)
        {
            Calls++;
            return Task.FromResult(new ConfigValidationResult(_success, true, _success ? 0 : 1, null, null, null));
        }
    }

    private sealed class StubReloadService : IReloadService
    {
        private readonly bool _success;
        public StubReloadService(
            bool success,
            bool restartCommandConfigured = true,
            bool reloadCommandConfigured = true)
        {
            _success = success;
            RestartCommandConfigured = restartCommandConfigured;
            ReloadCommandConfigured = reloadCommandConfigured;
        }

        public bool RestartCommandConfigured { get; }
        public bool ReloadCommandConfigured { get; }
        public int RestartCalls { get; private set; }
        public int ReloadCalls { get; private set; }

        public Task<ReloadResult> ReloadAsync(CancellationToken ct = default) =>
            RunReloadAsync();

        public Task<ReloadResult> RestartAsync(CancellationToken ct = default) =>
            RunRestartAsync();

        private Task<ReloadResult> RunReloadAsync()
        {
            ReloadCalls++;
            return Task.FromResult(new ReloadResult(_success, _success ? 0 : 1, null, null));
        }

        private Task<ReloadResult> RunRestartAsync()
        {
            RestartCalls++;
            return Task.FromResult(new ReloadResult(_success, _success ? 0 : 1, null, null));
        }
    }

    private sealed class StubHostsFileService : IHostsFileService
    {
        public Task<IReadOnlyList<HostEntry>> ReadAsync(CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<HostEntry>>(new List<HostEntry>());
        public Task WriteAsync(IReadOnlyList<HostEntry> entries, CancellationToken ct = default) => Task.CompletedTask;
    }

    private sealed class StubConfigService : IDnsmasqConfigService
    {
        public int ApplyCalls { get; private set; }
        public Task ApplyEffectiveConfigChangesAsync(IReadOnlyList<PendingEffectiveConfigChange> changes, CancellationToken ct = default)
        {
            ApplyCalls++;
            return Task.CompletedTask;
        }
        public Task<IReadOnlyList<DhcpHostEntry>> ReadDhcpHostsAsync(CancellationToken ct = default) => Task.FromResult<IReadOnlyList<DhcpHostEntry>>(Array.Empty<DhcpHostEntry>());
        public Task WriteDhcpHostsAsync(IReadOnlyList<DhcpHostEntry> entries, CancellationToken ct = default) => Task.CompletedTask;
        public Task<ManagedConfigContent> ReadManagedConfigAsync(CancellationToken ct = default) => Task.FromResult(new ManagedConfigContent(Array.Empty<DnsmasqConfLine>(), ""));
        public Task WriteManagedConfigAsync(IReadOnlyList<DnsmasqConfLine> lines, CancellationToken ct = default) => Task.CompletedTask;
    }
}
