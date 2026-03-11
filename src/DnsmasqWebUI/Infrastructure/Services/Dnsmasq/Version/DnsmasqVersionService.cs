using DnsmasqWebUI.Infrastructure.Serialization.Parsers.Dnsmasq;
using DnsmasqWebUI.Infrastructure.Services.Common.Process.Abstractions;
using DnsmasqWebUI.Infrastructure.Services.Dnsmasq.Version.Abstractions;
using DnsmasqWebUI.Models.Config;
using DnsmasqWebUI.Models.Dnsmasq;
using Microsoft.Extensions.Options;

namespace DnsmasqWebUI.Infrastructure.Services.Dnsmasq.Version;

public sealed class DnsmasqVersionService : IDnsmasqVersionService
{
    private readonly DnsmasqOptions _options;
    private readonly IProcessRunner _processRunner;

    public DnsmasqVersionService(
        IOptions<DnsmasqOptions> options,
        IProcessRunner processRunner)
    {
        _options = options.Value;
        _processRunner = processRunner;
    }

    public async Task<DnsmasqVersionInfo> GetVersionInfoAsync(CancellationToken ct = default)
    {
        var command = _options.VersionCommand?.Trim();
        var minimumVersion = System.Version.TryParse(_options.MinimumVersion, out var minVer)
            ? minVer
            : new System.Version(2, 91);

        if (string.IsNullOrWhiteSpace(command))
        {
            var emptyCaps = new DnsmasqCompileCapabilities(false, false, false, false, new HashSet<string>(StringComparer.OrdinalIgnoreCase));
            return new DnsmasqVersionInfo(
                InstalledVersion: null,
                MinimumVersion: minimumVersion,
                ProbeSucceeded: false,
                IsSupported: false,
                ProbeCommand: "",
                Error: "Version command is not configured.",
                Capabilities: emptyCaps);
        }

        var result = await _processRunner.RunAsync(command, _options.VersionTimeout, ct);

        var error = result.TimedOut
            ? "Version command timed out."
            : !string.IsNullOrWhiteSpace(result.ExceptionMessage)
                ? result.ExceptionMessage
                : null;

        var installed = DnsmasqVersionParser.TryParse(result.Stdout, result.Stderr);
        if (installed == null && error == null)
            error = "Could not parse version from command output.";

        var probeSucceeded = installed != null;
        var isSupported = probeSucceeded && installed!.CompareTo(minimumVersion) >= 0;
        var capabilities = DnsmasqCompileOptionsParser.Parse(result.Stdout, result.Stderr);

        return new DnsmasqVersionInfo(
            InstalledVersion: installed,
            MinimumVersion: minimumVersion,
            ProbeSucceeded: probeSucceeded,
            IsSupported: isSupported,
            ProbeCommand: command,
            Error: error,
            Capabilities: capabilities);
    }
}
