using DnsmasqWebUI.Infrastructure.Services.Dnsmasq.Config;
using DnsmasqWebUI.Infrastructure.Services.Dnsmasq.Hosts.Abstractions;
using DnsmasqWebUI.Infrastructure.Services.Dnsmasq.Reload.Abstractions;
using DnsmasqWebUI.Infrastructure.Services.Dnsmasq.Validation.Abstractions;
using DnsmasqWebUI.Infrastructure.Services.Dnsmasq.Version.Abstractions;
using DnsmasqWebUI.Models.Hosts;
using DnsmasqWebUI.Infrastructure.Services.EffectiveConfig;
using DnsmasqWebUI.Infrastructure.Services.EffectiveConfig.Metadata;
using DnsmasqWebUI.Infrastructure.Services.EffectiveConfig.Validation;
using DnsmasqWebUI.Infrastructure.Services.EffectiveConfig.Validation.Abstractions;
using DnsmasqWebUI.Models.Config;
using DnsmasqWebUI.Models.Dnsmasq;
using DnsmasqWebUI.Models.Dnsmasq.EffectiveConfig;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace DnsmasqWebUI.Tests.Services.EffectiveConfig;

/// <summary>
/// Save path capability guard: unsupported-option changes return UnsupportedCapabilities
/// before validation or reload can run.
/// </summary>
public class EffectiveConfigSaveServiceCapabilityTests
{
    [Fact]
    public async Task SaveAsync_WhenCapabilitiesMissingDnssec_ReturnsUnsupportedCapabilitiesErrorCode()
    {
        var dir = Path.Combine(Path.GetTempPath(), "dnsmasq-cap-" + Guid.NewGuid().ToString("N"));
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

            var versionService = new StubVersionService(CreateSupportedVersionInfo(dnssec: false));
            var semanticValidationService = new EffectiveConfigSemanticValidationService(new OptionSemanticValidator(Array.Empty<IOptionSemanticHandler>()));

            var saveService = new EffectiveConfigSaveService(
                setService,
                configService,
                cache,
                new StubHostsFileService(),
                new UnexpectedValidationService(),
                semanticValidationService,
                new UnexpectedReloadService(),
                versionService,
                NullLogger<EffectiveConfigSaveService>.Instance);

            var changes = new List<PendingDnsmasqChange>
            {
                new PendingOptionChange(EffectiveConfigSections.SectionDnssec, DnsmasqConfKeys.DnssecCheckUnsigned, null, "no", null)
            };

            var result = await saveService.SaveAsync(changes);

            Assert.Equal(EffectiveConfigSaveResult.ErrorCodes.UnsupportedCapabilities, result.ErrorCode);
            Assert.False(File.Exists(Path.Combine(dir, managedName)));
        }
        finally
        {
            cache?.Dispose();
            if (Directory.Exists(dir))
                Directory.Delete(dir, recursive: true);
        }
    }

    private sealed class StubVersionService : IDnsmasqVersionService
    {
        private readonly DnsmasqVersionInfo _info;

        public StubVersionService(DnsmasqVersionInfo info) => _info = info;

        public Task<DnsmasqVersionInfo> GetVersionInfoAsync(CancellationToken ct = default) =>
            Task.FromResult(_info);
    }

    private static DnsmasqVersionInfo CreateSupportedVersionInfo(bool dnssec)
    {
        var capabilities = new DnsmasqCompileCapabilities(
            Dhcp: true,
            Tftp: true,
            Dnssec: dnssec,
            Dbus: false,
            new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "DHCP", "TFTP" });

        return new DnsmasqVersionInfo(
            new Version(2, 91),
            new Version(2, 91),
            ProbeSucceeded: true,
            IsSupported: true,
            "dnsmasq --version",
            null,
            capabilities);
    }

    private sealed class UnexpectedValidationService : IConfigValidationService
    {
        public Task<ConfigValidationResult> ValidateAsync(CancellationToken ct = default) =>
            throw new InvalidOperationException("ValidateAsync should not be called when capabilities are unsupported.");
    }

    private sealed class UnexpectedReloadService : IReloadService
    {
        public Task<ReloadResult> ReloadAsync(CancellationToken ct = default) =>
            throw new InvalidOperationException("ReloadAsync should not be called when capabilities are unsupported.");
    }

    private sealed class StubHostsFileService : IHostsFileService
    {
        public Task<IReadOnlyList<HostEntry>> ReadAsync(CancellationToken ct = default) => Task.FromResult<IReadOnlyList<HostEntry>>(new List<HostEntry>());
        public Task WriteAsync(IReadOnlyList<HostEntry> entries, CancellationToken ct = default) => Task.CompletedTask;
    }
}
