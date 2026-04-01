using DnsmasqWebUI.Models.Hosts;

namespace DnsmasqWebUI.Tests.Models.Hosts;

public class HostsEntryEditValidatorTests
{
    [Fact]
    public void Empty_is_invalid()
    {
        var e = HostsEntryEditValidator.GetErrors("", Array.Empty<string>());
        Assert.NotEmpty(e);
    }

    [Fact]
    public void Valid_ipv4_and_name_ok()
    {
        Assert.True(HostsEntryEditValidator.IsValid("192.168.1.1", new[] { "router" }));
    }

    [Fact]
    public void Missing_names_invalid()
    {
        var e = HostsEntryEditValidator.GetErrors("10.0.0.1", Array.Empty<string>());
        Assert.Contains(e, x => x.Contains("hostname", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Field_errors_split_address_and_names()
    {
        Assert.NotEmpty(HostsEntryEditValidator.GetAddressFieldErrors(""));
        Assert.Empty(HostsEntryEditValidator.GetHostnameFieldErrors(new[] { "h" }));
        Assert.NotEmpty(HostsEntryEditValidator.GetHostnameFieldErrors(Array.Empty<string>()));
    }
}
