using DnsmasqWebUI.Models.Config;

namespace DnsmasqWebUI.Tests.Models.Config;

public sealed class DnsmasqOptionsValidatorTests
{
    [Fact]
    public void Validate_WithRelativeManagedFilesDirectory_Fails()
    {
        var dir = Path.Combine(Path.GetTempPath(), "dnsmasq-validator-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        var mainPath = Path.Combine(dir, "dnsmasq.conf");
        File.WriteAllText(mainPath, "port=53\n");

        try
        {
            var validator = new DnsmasqOptionsValidator();
            var options = new DnsmasqOptions
            {
                MainConfigPath = mainPath,
                ManagedFilesDirectory = "repo/dnsmasq"
            };

            var result = validator.Validate(null, options);

            Assert.False(result.Succeeded);
            Assert.NotNull(result.Failures);
            Assert.Contains(result.Failures!, f => f.Contains("ManagedFilesDirectory", StringComparison.Ordinal));
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    [Fact]
    public void Validate_WithManagedFilesDirectoryAndFileNames_Succeeds()
    {
        var dir = Path.Combine(Path.GetTempPath(), "dnsmasq-validator-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        var mainPath = Path.Combine(dir, "dnsmasq.conf");
        File.WriteAllText(mainPath, "port=53\n");

        try
        {
            var validator = new DnsmasqOptionsValidator();
            var options = new DnsmasqOptions
            {
                MainConfigPath = mainPath,
                ManagedFilesDirectory = Path.Combine(dir, "repo"),
                ManagedFileName = "zz-webui.conf",
                ManagedHostsFileName = "zz-webui.hosts"
            };

            var result = validator.Validate(null, options);

            Assert.True(result.Succeeded);
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }
}
