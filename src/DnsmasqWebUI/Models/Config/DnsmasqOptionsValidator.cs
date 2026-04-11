using DnsmasqWebUI.Infrastructure.Services.Registration.Abstractions;
using Microsoft.Extensions.Options;

namespace DnsmasqWebUI.Models.Config;

/// <summary>
/// Validates required dnsmasq options at startup. If config is missing or default paths don't point at existing files,
/// the application exits with a detailed error instead of failing later at runtime.
/// Registered via assembly scanning (<see cref="IApplicationOptionsValidator{TOptions}"/>).
/// </summary>
public sealed class DnsmasqOptionsValidator : IApplicationOptionsValidator<DnsmasqOptions>
{
    public ValidateOptionsResult Validate(string? name, DnsmasqOptions options)
    {
        if (options == null)
            return ValidateOptionsResult.Fail("Dnsmasq options are not configured. Add a 'Dnsmasq' section in appsettings.json or set Dnsmasq__* environment variables.");

        var failures = new List<string>();

        if (string.IsNullOrWhiteSpace(options.MainConfigPath))
        {
            failures.Add("Dnsmasq:MainConfigPath is required. Set it in appsettings.json (e.g. \"MainConfigPath\": \"/etc/dnsmasq.conf\") or via the Dnsmasq__MainConfigPath environment variable.");
        }
        else
        {
            var mainPath = Path.GetFullPath(options.MainConfigPath.Trim());
            if (!File.Exists(mainPath))
            {
                failures.Add($"Main dnsmasq config file not found: {mainPath}. Ensure Dnsmasq:MainConfigPath points to an existing dnsmasq config file, or create the file. Override with Dnsmasq__MainConfigPath if using a different path.");
            }
        }

        if (!string.IsNullOrWhiteSpace(options.ManagedFilesDirectory) &&
            !Path.IsPathRooted(options.ManagedFilesDirectory.Trim()))
        {
            failures.Add("Dnsmasq:ManagedFilesDirectory must be an absolute path when set.");
        }

        if (HasDirectorySeparators(options.ManagedFileName))
            failures.Add("Dnsmasq:ManagedFileName must be a file name, not a path.");

        if (HasDirectorySeparators(options.ManagedHostsFileName))
            failures.Add("Dnsmasq:ManagedHostsFileName must be a file name, not a path.");

        if (failures.Count == 0)
        {
            var paths = DnsmasqManagedPathSet.FromOptions(options);
            if (string.Equals(paths.MainConfigPath, paths.ManagedFilePath, StringComparison.Ordinal))
                failures.Add("Resolved managed config path must differ from Dnsmasq:MainConfigPath.");
            if (string.Equals(paths.MainConfigPath, paths.ManagedHostsFilePath, StringComparison.Ordinal))
                failures.Add("Resolved managed hosts path must differ from Dnsmasq:MainConfigPath.");
            if (string.Equals(paths.ManagedFilePath, paths.ManagedHostsFilePath, StringComparison.Ordinal))
                failures.Add("Resolved managed config path and managed hosts path must differ.");
        }

        const int minTimeoutSeconds = 1;
        const int maxTimeoutSeconds = 600;
        if (options.RestartTimeoutSeconds < minTimeoutSeconds || options.RestartTimeoutSeconds > maxTimeoutSeconds)
            failures.Add($"Dnsmasq:RestartTimeoutSeconds must be between {minTimeoutSeconds} and {maxTimeoutSeconds}. Current value: {options.RestartTimeoutSeconds}.");
        if (options.StatusTimeoutSeconds < minTimeoutSeconds || options.StatusTimeoutSeconds > maxTimeoutSeconds)
            failures.Add($"Dnsmasq:StatusTimeoutSeconds must be between {minTimeoutSeconds} and {maxTimeoutSeconds}. Current value: {options.StatusTimeoutSeconds}.");
        if (options.StatusShowTimeoutSeconds < minTimeoutSeconds || options.StatusShowTimeoutSeconds > maxTimeoutSeconds)
            failures.Add($"Dnsmasq:StatusShowTimeoutSeconds must be between {minTimeoutSeconds} and {maxTimeoutSeconds}. Current value: {options.StatusShowTimeoutSeconds}.");
        if (options.LogsTimeoutSeconds < minTimeoutSeconds || options.LogsTimeoutSeconds > maxTimeoutSeconds)
            failures.Add($"Dnsmasq:LogsTimeoutSeconds must be between {minTimeoutSeconds} and {maxTimeoutSeconds}. Current value: {options.LogsTimeoutSeconds}.");
        if (options.ValidateTimeoutSeconds < minTimeoutSeconds || options.ValidateTimeoutSeconds > maxTimeoutSeconds)
            failures.Add($"Dnsmasq:ValidateTimeoutSeconds must be between {minTimeoutSeconds} and {maxTimeoutSeconds}. Current value: {options.ValidateTimeoutSeconds}.");
        if (options.VersionTimeoutSeconds < minTimeoutSeconds || options.VersionTimeoutSeconds > maxTimeoutSeconds)
            failures.Add($"Dnsmasq:VersionTimeoutSeconds must be between {minTimeoutSeconds} and {maxTimeoutSeconds}. Current value: {options.VersionTimeoutSeconds}.");

        if (!string.IsNullOrWhiteSpace(options.VersionCommand) && !Version.TryParse(options.MinimumVersion, out _))
            failures.Add($"Dnsmasq:MinimumVersion must be a valid version (e.g. 2.91). Current value: {options.MinimumVersion}.");

        if (failures.Count == 0)
            return ValidateOptionsResult.Success;

        return ValidateOptionsResult.Fail(failures);
    }

    private static bool HasDirectorySeparators(string? value) =>
        !string.IsNullOrWhiteSpace(value)
        && value.IndexOfAny(new[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar }) >= 0;
}
