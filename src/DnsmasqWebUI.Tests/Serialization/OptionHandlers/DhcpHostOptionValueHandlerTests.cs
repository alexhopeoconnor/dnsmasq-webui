using DnsmasqWebUI.Infrastructure.Serialization.OptionHandlers;
using DnsmasqWebUI.Infrastructure.Services.EffectiveConfig.Metadata;
using DnsmasqWebUI.Models.Dhcp;

namespace DnsmasqWebUI.Tests.Serialization.OptionHandlers;

public class DhcpHostOptionValueHandlerTests
{
    private readonly DhcpHostOptionValueHandler _sut = new();

    [Fact]
    public void OptionName_EqualsDnsmasqConfKeysDhcpHost()
    {
        Assert.Equal(DnsmasqConfKeys.DhcpHost, _sut.OptionName);
    }

    [Fact]
    public void SerializeValue_EmitsValueOnlyForm()
    {
        var entry = new DhcpHostEntry
        {
            MacAddresses = new List<string> { "11:22:33:44:55:66" },
            Address = "192.168.1.10"
        };
        var value = _sut.SerializeValue(entry);
        Assert.Equal("11:22:33:44:55:66, 192.168.1.10", value);
    }

    [Fact]
    public void SerializeValue_IgnoreEntry_EmitsIgnoreToken()
    {
        var entry = new DhcpHostEntry
        {
            MacAddresses = new List<string> { "aa:bb:cc:dd:ee:ff" },
            Ignore = true
        };
        var value = _sut.SerializeValue(entry);
        Assert.Contains("ignore", value);
        Assert.Contains("aa:bb:cc:dd:ee:ff", value);
    }

    [Fact]
    public void SerializeValue_EmptyEntry_ReturnsEmptyString()
    {
        var entry = new DhcpHostEntry();
        var value = _sut.SerializeValue(entry);
        Assert.Equal("", value);
    }

    [Fact]
    public void TryParseValue_RoundTripsStructuredFields()
    {
        var input = new DhcpHostEntry
        {
            MacAddresses = new List<string> { "11:22:33:44:55:66" },
            Address = "192.168.1.10",
            Extra = new List<string> { "id:01:02:03", "set:test" },
            Comment = "note",
            IsComment = true
        };
        var value = _sut.SerializeValue(input);
        var ok = _sut.TryParseValue(value, 1, out var output);
        Assert.True(ok);
        Assert.NotNull(output);
        Assert.True(output!.IsComment);
        Assert.Contains("id:01:02:03", output.Extra!);
        Assert.Equal("note", output.Comment);
    }

    [Fact]
    public void TryParseValue_IsComment_RoundTrips()
    {
        var entry = new DhcpHostEntry
        {
            MacAddresses = new List<string> { "aa:bb:cc:dd:ee:ff" },
            Address = "192.168.1.1",
            IsComment = true
        };
        var value = _sut.SerializeValue(entry);
        Assert.StartsWith("#", value);
        var ok = _sut.TryParseValue(value, 1, out var roundTripped);
        Assert.True(ok);
        Assert.NotNull(roundTripped);
        Assert.True(roundTripped!.IsComment);
        Assert.Single(roundTripped.MacAddresses!);
        Assert.Equal("aa:bb:cc:dd:ee:ff", roundTripped.MacAddresses[0]);
        Assert.Equal("192.168.1.1", roundTripped.Address);
    }

    [Fact]
    public void TryParseValue_Comment_EmitsTrailingComment_AndRoundTrips()
    {
        var entry = new DhcpHostEntry
        {
            MacAddresses = new List<string> { "aa:bb:cc:dd:ee:ff" },
            Address = "192.168.1.1",
            Comment = " my note "
        };
        var value = _sut.SerializeValue(entry);
        Assert.Contains(" # ", value);
        var ok = _sut.TryParseValue(value, 1, out var roundTripped);
        Assert.True(ok);
        Assert.NotNull(roundTripped);
        Assert.Equal("my note", roundTripped!.Comment?.Trim());
    }
}
