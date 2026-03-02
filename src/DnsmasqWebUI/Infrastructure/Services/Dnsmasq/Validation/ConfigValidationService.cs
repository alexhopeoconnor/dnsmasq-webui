using DnsmasqWebUI.Infrastructure.Services.Common.Process.Abstractions;
using DnsmasqWebUI.Infrastructure.Services.Dnsmasq.Validation.Abstractions;
using DnsmasqWebUI.Models.Config;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DnsmasqWebUI.Infrastructure.Services.Dnsmasq.Validation;

public sealed class ConfigValidationService : IConfigValidationService
{
    private readonly DnsmasqOptions _options;
    private readonly IProcessRunner _processRunner;
    private readonly ILogger<ConfigValidationService> _logger;

    public ConfigValidationService(
        IOptions<DnsmasqOptions> options,
        IProcessRunner processRunner,
        ILogger<ConfigValidationService> logger)
    {
        _options = options.Value;
        _processRunner = processRunner;
        _logger = logger;
    }

    public async Task<ConfigValidationResult> ValidateAsync(CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(_options.ValidateCommand))
        {
            return new ConfigValidationResult(
                Success: true,
                Attempted: false,
                ExitCode: 0,
                StdOut: null,
                StdErr: null,
                UserMessage: "Validation command not configured.");
        }

        var command = _options.ValidateCommand!
            .Replace("{{MainConfigPath}}", _options.MainConfigPath, StringComparison.Ordinal);

        var run = await _processRunner.RunAsync(command, _options.ValidateTimeout, ct);

        var stderr = run.Stderr;
        if (run.TimedOut)
            stderr = (string.IsNullOrEmpty(stderr) ? "" : stderr + "\n") +
                     $"Validation timed out after {_options.ValidateTimeoutSeconds} seconds.";
        if (!string.IsNullOrWhiteSpace(run.ExceptionMessage))
            stderr = (string.IsNullOrEmpty(stderr) ? "" : stderr + "\n") + run.ExceptionMessage;

        var success = run.ExitCode == 0;
        if (!success)
            _logger.LogWarning("Config validation failed: exit {ExitCode}, stderr: {Stderr}", run.ExitCode, stderr);

        return new ConfigValidationResult(
            Success: success,
            Attempted: true,
            ExitCode: run.ExitCode ?? -1,
            StdOut: run.Stdout,
            StdErr: stderr,
            UserMessage: success
                ? "Configuration validated."
                : "Configuration validation failed.");
    }
}
