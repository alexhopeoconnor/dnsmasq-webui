namespace DnsmasqWebUI.Models;

/// <summary>Dnsmasq service and config paths status returned by api/status.</summary>
public record DnsmasqServiceStatus(
    string? SystemHostsPath,
    bool SystemHostsPathExists,
    bool NoHosts,
    IReadOnlyList<string> AddnHostsPaths,
    EffectiveDnsmasqConfig? EffectiveConfig,
    string? MainConfigPath,
    string? ManagedFilePath,
    string? LeasesPath,
    bool MainConfigPathExists,
    bool ManagedFilePathExists,
    bool LeasesPathConfigured,
    bool LeasesPathExists,
    bool ReloadCommandConfigured,
    bool StatusCommandConfigured,
    bool StatusShowConfigured,
    bool LogsConfigured,
    string DnsmasqStatus,
    int? StatusCommandExitCode,
    string? StatusCommandStdout,
    string? StatusCommandStderr,
    string? StatusShowOutput,
    string? LogsOutput
);
