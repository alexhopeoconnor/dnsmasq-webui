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

    [Theory]
    [InlineData("two words")]
    [InlineData("name#comment")]
    [InlineData("name\talias")]
    public void Hostname_token_characters_that_change_hosts_syntax_are_invalid(string name)
    {
        var errors = HostsEntryEditValidator.GetHostnameFieldErrors(new[] { name });

        Assert.NotEmpty(errors);
    }

    [Fact]
    public void Raw_passthrough_line_cannot_inject_another_record()
    {
        var entries = new[]
        {
            new HostEntry
            {
                IsPassthrough = true,
                RawLine = "192.0.2.1 one\n192.0.2.2 two"
            }
        };

        var errors = HostsEntryEditValidator.GetEntryErrors(entries);

        Assert.Contains(errors, error => error.Contains("line break", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Existing_single_line_passthrough_is_preserved()
    {
        var errors = HostsEntryEditValidator.GetEntryErrors(new[]
        {
            new HostEntry { IsPassthrough = true, RawLine = "# retained legacy comment" }
        });

        Assert.Empty(errors);
    }
}
