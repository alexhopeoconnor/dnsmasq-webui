using DnsmasqWebUI.Infrastructure.Services.Dnsmasq.Config;
using DnsmasqWebUI.Infrastructure.Services.Dnsmasq.Config.Abstractions;
using DnsmasqWebUI.Infrastructure.Services.Dnsmasq.Reload.Abstractions;
using DnsmasqWebUI.Models.Config;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace DnsmasqWebUI.Tests.Services.Dnsmasq.Config;

public sealed class EnsureManagedConfigHostedServiceTests
{
    [Fact]
    public async Task StartAsync_when_it_creates_the_overlay_reloads_dnsmasq_after_writing_it()
    {
        var fixture = CreateFixture(mainContent: "port=53\n");
        try
        {
            await fixture.Service.StartAsync(CancellationToken.None);

            var mainLines = await File.ReadAllLinesAsync(fixture.MainConfigPath);
            Assert.Equal($"conf-file={fixture.ManagedConfigPath}", mainLines[^1]);
            Assert.True(File.Exists(fixture.ManagedConfigPath));
            Assert.True(File.Exists(fixture.ManagedHostsPath));
            Assert.Contains($"addn-hosts={fixture.ManagedHostsPath}", await File.ReadAllTextAsync(fixture.ManagedConfigPath));
            Assert.Equal(1, fixture.Reload.RestartCalls);
            Assert.Equal(0, fixture.Reload.ReloadCalls);
        }
        finally
        {
            fixture.Dispose();
        }
    }

    [Fact]
    public async Task StartAsync_when_the_overlay_is_already_in_place_does_not_reload_dnsmasq()
    {
        var fixture = CreateFixture(
            mainContent: "port=53\n\nconf-file={managed}\n",
            managedContent: "addn-hosts={hosts}\n");
        try
        {
            await fixture.Service.StartAsync(CancellationToken.None);

            Assert.Equal(0, fixture.Reload.Calls);
        }
        finally
        {
            fixture.Dispose();
        }
    }

    [Fact]
    public async Task StartAsync_when_the_startup_reload_fails_restores_the_files_it_created_before_stopping()
    {
        const string originalMain = "port=53\n";
        var fixture = CreateFixture(
            mainContent: originalMain,
            reloadResults:
            [
                new ReloadResult(false, 17, "reload output", "reload error"),
                new ReloadResult(true, 0, null, null)
            ]);
        try
        {
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => fixture.Service.StartAsync(CancellationToken.None));

            Assert.Contains("could not be restarted", exception.Message, StringComparison.OrdinalIgnoreCase);
            Assert.Equal(originalMain, await File.ReadAllTextAsync(fixture.MainConfigPath));
            Assert.False(File.Exists(fixture.ManagedConfigPath));
            Assert.False(File.Exists(fixture.ManagedHostsPath));
            Assert.Equal(2, fixture.Reload.RestartCalls);
            Assert.Equal(0, fixture.Reload.ReloadCalls);
        }
        finally
        {
            fixture.Dispose();
        }
    }

    [Fact]
    public async Task StartAsync_without_a_restart_command_stops_before_changing_overlay_even_when_reload_is_configured()
    {
        const string originalMain = "port=53\n";
        var fixture = CreateFixture(
            mainContent: originalMain,
            restartCommand: null,
            reloadCommand: "pkill -HUP -x dnsmasq");
        try
        {
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => fixture.Service.StartAsync(CancellationToken.None));

            Assert.Contains("RestartCommand", exception.Message, StringComparison.Ordinal);
            Assert.Equal(0, fixture.Reload.Calls);
            Assert.Equal(originalMain, await File.ReadAllTextAsync(fixture.MainConfigPath));
            Assert.False(File.Exists(fixture.ManagedConfigPath));
            Assert.False(File.Exists(fixture.ManagedHostsPath));
        }
        finally
        {
            fixture.Dispose();
        }
    }

    private static HostedServiceFixture CreateFixture(
        string mainContent,
        string? managedContent = null,
        ReloadResult[]? reloadResults = null,
        string? restartCommand = "restart dnsmasq",
        string? reloadCommand = null)
    {
        var directory = Path.Combine(Path.GetTempPath(), "dnsmasq-startup-overlay-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        var mainConfigPath = Path.Combine(directory, "dnsmasq.conf");
        var managedConfigPath = Path.Combine(directory, "zz-managed.conf");
        var managedHostsPath = Path.Combine(directory, "zz-managed.hosts");
        File.WriteAllText(
            mainConfigPath,
            mainContent
                .Replace("{managed}", managedConfigPath, StringComparison.Ordinal)
                .Replace("{hosts}", managedHostsPath, StringComparison.Ordinal));
        if (managedContent != null)
        {
            File.WriteAllText(
                managedConfigPath,
                managedContent
                    .Replace("{managed}", managedConfigPath, StringComparison.Ordinal)
                    .Replace("{hosts}", managedHostsPath, StringComparison.Ordinal));
        }

        var options = Options.Create(new DnsmasqOptions
        {
            MainConfigPath = mainConfigPath,
            ManagedFileName = Path.GetFileName(managedConfigPath),
            ManagedHostsFileName = Path.GetFileName(managedHostsPath),
            RestartCommand = restartCommand,
            ReloadCommand = reloadCommand
        });
        var configCache = new ConfigSetCache(options, NullLogger<ConfigSetCache>.Instance);
        var configSetService = new DnsmasqConfigSetService(configCache);
        var configService = new DnsmasqConfigService(
            configSetService,
            configCache,
            NullLogger<DnsmasqConfigService>.Instance);
        var reload = new RecordingReloadService(
            !string.IsNullOrWhiteSpace(restartCommand),
            !string.IsNullOrWhiteSpace(reloadCommand),
            reloadResults ?? [new ReloadResult(true, 0, null, null)]);
        var provider = new ServiceCollection()
            .AddSingleton<IConfigSetCache>(configCache)
            .AddSingleton<IDnsmasqConfigSetService>(configSetService)
            .AddSingleton<IDnsmasqConfigService>(configService)
            .AddScoped<IReloadService>(_ => reload)
            .BuildServiceProvider();
        var service = new EnsureManagedConfigHostedService(
            provider.GetRequiredService<IServiceScopeFactory>(),
            options,
            NullLogger<EnsureManagedConfigHostedService>.Instance);

        return new HostedServiceFixture(
            directory,
            mainConfigPath,
            managedConfigPath,
            managedHostsPath,
            configCache,
            provider,
            reload,
            service);
    }

    private sealed class HostedServiceFixture : IDisposable
    {
        public HostedServiceFixture(
            string directory,
            string mainConfigPath,
            string managedConfigPath,
            string managedHostsPath,
            ConfigSetCache configCache,
            ServiceProvider provider,
            RecordingReloadService reload,
            EnsureManagedConfigHostedService service)
        {
            Directory = directory;
            MainConfigPath = mainConfigPath;
            ManagedConfigPath = managedConfigPath;
            ManagedHostsPath = managedHostsPath;
            ConfigCache = configCache;
            Provider = provider;
            Reload = reload;
            Service = service;
        }

        public string Directory { get; }
        public string MainConfigPath { get; }
        public string ManagedConfigPath { get; }
        public string ManagedHostsPath { get; }
        public ConfigSetCache ConfigCache { get; }
        public ServiceProvider Provider { get; }
        public RecordingReloadService Reload { get; }
        public EnsureManagedConfigHostedService Service { get; }

        public void Dispose()
        {
            ConfigCache.Dispose();
            Provider.Dispose();
            if (System.IO.Directory.Exists(Directory))
                System.IO.Directory.Delete(Directory, recursive: true);
        }
    }

    private sealed class RecordingReloadService(
        bool restartCommandConfigured,
        bool reloadCommandConfigured,
        params ReloadResult[] results) : IReloadService
    {
        public int RestartCalls { get; private set; }
        public int ReloadCalls { get; private set; }
        public int Calls => RestartCalls + ReloadCalls;
        public bool RestartCommandConfigured => restartCommandConfigured;
        public bool ReloadCommandConfigured => reloadCommandConfigured;

        public Task<ReloadResult> ReloadAsync(CancellationToken ct = default)
        {
            ReloadCalls++;
            return Task.FromResult(NextResult());
        }

        public Task<ReloadResult> RestartAsync(CancellationToken ct = default)
        {
            RestartCalls++;
            return Task.FromResult(NextResult());
        }

        private ReloadResult NextResult()
        {
            var index = Math.Min(Calls - 1, results.Length - 1);
            return results[index];
        }
    }
}
