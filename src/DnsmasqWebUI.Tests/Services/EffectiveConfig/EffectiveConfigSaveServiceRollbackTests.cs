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

public class EffectiveConfigSaveServiceRollbackTests
{
    [Fact]
    public async Task SaveAsync_WhenValidationFails_RestoresExistingConfigAndRemovesNewManagedHostsFile()
    {
        var directory = Path.Combine(Path.GetTempPath(), "dnsmasq-save-rollback-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        ConfigSetCache? cache = null;

        try
        {
            var mainPath = Path.Combine(directory, "dnsmasq.conf");
            File.WriteAllText(mainPath, "port=53\n");
            var options = Options.Create(new DnsmasqOptions
            {
                MainConfigPath = mainPath,
                ManagedFileName = "zz-managed.conf",
                ManagedHostsFileName = "zz-managed.hosts"
            });
            cache = new ConfigSetCache(options, NullLogger<ConfigSetCache>.Instance);
            var setService = new DnsmasqConfigSetService(cache);
            var set = await setService.GetConfigSetAsync();
            var originalManagedConfig = "# existing managed configuration\nport=53\n";
            File.WriteAllText(set.ManagedFilePath!, originalManagedConfig);
            Assert.False(File.Exists(set.ManagedHostsFilePath!));

            var saveService = new EffectiveConfigSaveService(
                setService,
                new DnsmasqConfigService(setService, cache, NullLogger<DnsmasqConfigService>.Instance),
                cache,
                new NoopHostsFileService(),
                new StubValidationService(success: false),
                new EffectiveConfigSemanticValidationService(new OptionSemanticValidator(Array.Empty<IOptionSemanticHandler>())),
                new UnexpectedReloadService(),
                new StubVersionService(SupportedVersion()),
                NullLogger<EffectiveConfigSaveService>.Instance);

            var result = await saveService.SaveAsync(
            [
                new PendingOptionChange(
                    EffectiveConfigSections.SectionResolver,
                    DnsmasqConfKeys.Port,
                    "53",
                    "5353",
                    set.ManagedFilePath)
            ]);

            Assert.False(result.Saved);
            Assert.True(result.RolledBack);
            Assert.Equal(EffectiveConfigSaveResult.ErrorCodes.ValidateFailed, result.ErrorCode);
            Assert.Equal(originalManagedConfig, File.ReadAllText(set.ManagedFilePath!));
            Assert.False(File.Exists(set.ManagedHostsFilePath!));
            Assert.Single(result.Backups);
        }
        finally
        {
            cache?.Dispose();
            if (Directory.Exists(directory))
                Directory.Delete(directory, recursive: true);
        }
    }

    private static DnsmasqVersionInfo SupportedVersion() => new(
        new Version(2, 91),
        new Version(2, 91),
        ProbeSucceeded: true,
        IsSupported: true,
        "dnsmasq --version",
        null,
        new DnsmasqCompileCapabilities(true, true, true, false, new HashSet<string>(StringComparer.OrdinalIgnoreCase)));

    private sealed class StubVersionService(DnsmasqVersionInfo version) : IDnsmasqVersionService
    {
        public Task<DnsmasqVersionInfo> GetVersionInfoAsync(CancellationToken ct = default) => Task.FromResult(version);
    }

    private sealed class StubValidationService(bool success) : IConfigValidationService
    {
        public Task<ConfigValidationResult> ValidateAsync(CancellationToken ct = default) =>
            Task.FromResult(new ConfigValidationResult(success, true, success ? 0 : 1, null, null, "validation failed"));
    }

    private sealed class UnexpectedReloadService : IReloadService
    {
        public bool RestartCommandConfigured => true;
        public bool ReloadCommandConfigured => true;

        public Task<ReloadResult> ReloadAsync(CancellationToken ct = default) =>
            throw new InvalidOperationException("Reload must not be attempted after a validation failure.");

        public Task<ReloadResult> RestartAsync(CancellationToken ct = default) =>
            throw new InvalidOperationException("Restart must not be attempted after a validation failure.");
    }

    private sealed class NoopHostsFileService : IHostsFileService
    {
        public Task<IReadOnlyList<HostEntry>> ReadAsync(CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<HostEntry>>(Array.Empty<HostEntry>());

        public Task WriteAsync(IReadOnlyList<HostEntry> entries, CancellationToken ct = default) => Task.CompletedTask;
    }
}
