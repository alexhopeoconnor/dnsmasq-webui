namespace DnsmasqWebUI.Models.Config;

/// <summary>
/// Canonical dnsmasq file locations derived from application options.
/// MainConfigPath determines which config file the app edits; ManagedFilesDirectory controls
/// where the app-managed config and hosts files are written when configured.
/// </summary>
public sealed record DnsmasqManagedPathSet(
    string MainConfigPath,
    string ManagedFilesDirectory,
    string ManagedFilePath,
    string ManagedHostsFilePath)
{
    public static DnsmasqManagedPathSet? TryFromOptions(DnsmasqOptions options)
    {
        if (options == null || string.IsNullOrWhiteSpace(options.MainConfigPath))
            return null;

        return FromOptions(options);
    }

    public static DnsmasqManagedPathSet FromOptions(DnsmasqOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        if (string.IsNullOrWhiteSpace(options.MainConfigPath))
            throw new InvalidOperationException("Dnsmasq:MainConfigPath is required.");

        var mainConfigPath = Path.GetFullPath(options.MainConfigPath.Trim());
        var mainDir = Path.GetDirectoryName(mainConfigPath) ?? "";
        var managedFilesDirectory = string.IsNullOrWhiteSpace(options.ManagedFilesDirectory)
            ? mainDir
            : Path.GetFullPath(options.ManagedFilesDirectory.Trim());

        var managedFileName = GetFileNameOrDefault(options.ManagedFileName, "zz-dnsmasq-webui.conf");
        var managedHostsFileName = GetFileNameOrDefault(options.ManagedHostsFileName, "zz-dnsmasq-webui.hosts");

        return new DnsmasqManagedPathSet(
            MainConfigPath: mainConfigPath,
            ManagedFilesDirectory: managedFilesDirectory,
            ManagedFilePath: Path.Combine(managedFilesDirectory, managedFileName),
            ManagedHostsFilePath: Path.Combine(managedFilesDirectory, managedHostsFileName));
    }

    private static string GetFileNameOrDefault(string? value, string fallback) =>
        string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
}
