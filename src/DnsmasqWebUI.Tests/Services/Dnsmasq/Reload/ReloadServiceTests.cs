using DnsmasqWebUI.Infrastructure.Services.Common.Process.Abstractions;
using DnsmasqWebUI.Infrastructure.Services.Dnsmasq.Reload;
using DnsmasqWebUI.Models.Config;
using DnsmasqWebUI.Models.Contracts;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace DnsmasqWebUI.Tests.Services.Dnsmasq.Reload;

public sealed class ReloadServiceTests
{
    [Fact]
    public async Task RestartAsync_WhenOnlyReloadCommandIsConfigured_DoesNotFallBackToReloadCommand()
    {
        var runner = new RecordingProcessRunner();
        var service = new ReloadService(
            runner,
            Options.Create(new DnsmasqOptions
            {
                ReloadCommand = "pkill -HUP -x dnsmasq",
                RestartCommand = null
            }),
            NullLogger<ReloadService>.Instance);

        var restart = await service.RestartAsync();

        Assert.False(restart.Success);
        Assert.Equal(-1, restart.ExitCode);
        Assert.Contains("RestartCommand", restart.StdErr, StringComparison.Ordinal);
        Assert.Empty(runner.Commands);

        var reload = await service.ReloadAsync();

        Assert.True(reload.Success);
        Assert.Equal(["pkill -HUP -x dnsmasq"], runner.Commands);
    }

    [Fact]
    public async Task ReloadAsync_WhenOnlyRestartCommandIsConfigured_DoesNotMisreportAFullRestartAsReload()
    {
        var runner = new RecordingProcessRunner();
        var service = new ReloadService(
            runner,
            Options.Create(new DnsmasqOptions
            {
                ReloadCommand = null,
                RestartCommand = "systemctl restart dnsmasq"
            }),
            NullLogger<ReloadService>.Instance);

        var reload = await service.ReloadAsync();

        Assert.False(reload.Success);
        Assert.Equal(-1, reload.ExitCode);
        Assert.Contains("ReloadCommand", reload.StdErr, StringComparison.Ordinal);
        Assert.Empty(runner.Commands);

        var restart = await service.RestartAsync();

        Assert.True(restart.Success);
        Assert.Equal(["systemctl restart dnsmasq"], runner.Commands);
    }

    private sealed class RecordingProcessRunner : IProcessRunner
    {
        public List<string?> Commands { get; } = [];

        public Task<ProcessRunResult> RunAsync(string? command, TimeSpan timeout, CancellationToken ct = default)
        {
            Commands.Add(command);
            return Task.FromResult(new ProcessRunResult(0, "", "", TimedOut: false));
        }

        public Task<ProcessRunResult> RunAsync(string? command, TimeSpan timeout, int? maxOutputChars, CancellationToken ct = default) =>
            RunAsync(command, timeout, ct);

        public Task<IProcessHandle> StartAsync(string command, int? maxOutputChars = null, CancellationToken ct = default) =>
            throw new NotSupportedException();
    }
}
