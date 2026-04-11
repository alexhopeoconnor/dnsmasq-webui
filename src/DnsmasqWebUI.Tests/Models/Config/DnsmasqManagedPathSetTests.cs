using DnsmasqWebUI.Models.Config;

namespace DnsmasqWebUI.Tests.Models.Config;

public sealed class DnsmasqManagedPathSetTests
{
    [Fact]
    public void FromOptions_WithoutManagedFilesDirectory_UsesMainConfigDirectory()
    {
        var options = new DnsmasqOptions
        {
            MainConfigPath = "/etc/dnsmasq.conf",
            ManagedFileName = "zz-webui.conf",
            ManagedHostsFileName = "zz-webui.hosts"
        };

        var paths = DnsmasqManagedPathSet.FromOptions(options);

        Assert.Equal("/etc/dnsmasq.conf", paths.MainConfigPath);
        Assert.Equal("/etc", paths.ManagedFilesDirectory);
        Assert.Equal("/etc/zz-webui.conf", paths.ManagedFilePath);
        Assert.Equal("/etc/zz-webui.hosts", paths.ManagedHostsFilePath);
    }

    [Fact]
    public void FromOptions_WithManagedFilesDirectory_UsesOverrideDirectory()
    {
        var options = new DnsmasqOptions
        {
            MainConfigPath = "/etc/dnsmasq.conf",
            ManagedFilesDirectory = "/srv/repo/dnsmasq",
            ManagedFileName = "zz-webui.conf",
            ManagedHostsFileName = "zz-webui.hosts"
        };

        var paths = DnsmasqManagedPathSet.FromOptions(options);

        Assert.Equal("/etc/dnsmasq.conf", paths.MainConfigPath);
        Assert.Equal("/srv/repo/dnsmasq", paths.ManagedFilesDirectory);
        Assert.Equal("/srv/repo/dnsmasq/zz-webui.conf", paths.ManagedFilePath);
        Assert.Equal("/srv/repo/dnsmasq/zz-webui.hosts", paths.ManagedHostsFilePath);
    }
}
