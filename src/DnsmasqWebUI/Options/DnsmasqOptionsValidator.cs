using Microsoft.Extensions.Options;

namespace DnsmasqWebUI.Options;

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

        if (string.IsNullOrWhiteSpace(options.HostsPath))
        {
            failures.Add("Dnsmasq:HostsPath is required. Set it in appsettings.json (e.g. \"HostsPath\": \"/etc/hosts\") or via the Dnsmasq__HostsPath environment variable.");
        }
        else
        {
            var hostsPath = Path.GetFullPath(options.HostsPath.Trim());
            if (!File.Exists(hostsPath))
            {
                failures.Add($"Hosts file not found: {hostsPath}. Ensure Dnsmasq:HostsPath points to an existing hosts file (e.g. /etc/hosts). Override with Dnsmasq__HostsPath if using a different path.");
            }
        }

        if (failures.Count == 0)
            return ValidateOptionsResult.Success;

        return ValidateOptionsResult.Fail(failures);
    }
}
