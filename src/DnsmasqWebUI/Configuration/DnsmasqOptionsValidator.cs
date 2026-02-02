using Microsoft.Extensions.Options;

namespace DnsmasqWebUI.Configuration;

/// <summary>
/// Validates required dnsmasq options at startup. If config is missing or default paths don't point at existing files,
/// the application exits with a detailed error instead of failing later at runtime.
/// </summary>
public sealed class DnsmasqOptionsValidator : IValidateOptions<DnsmasqOptions>
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

        // SystemHostsPath is optional. When set, the UI shows system hosts as read-only. The app only writes to the
        // managed hosts file (ManagedHostsFileName in the same directory as main config). Hosts UI is available when
        // MainConfigPath is set (managed hosts path is then derived from it and ManagedHostsFileName).

        if (failures.Count == 0)
            return ValidateOptionsResult.Success;

        return ValidateOptionsResult.Fail(failures);
    }
}
