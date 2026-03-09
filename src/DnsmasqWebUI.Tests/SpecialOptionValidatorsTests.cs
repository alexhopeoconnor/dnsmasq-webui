using DnsmasqWebUI.Infrastructure.Services.EffectiveConfig.Validation;
using Xunit;

namespace DnsmasqWebUI.Tests;

/// <summary>
/// Validation tests for SpecialOptionValidators, including malformed numeric and structured values.
/// </summary>
public class SpecialOptionValidatorsTests
{
    [Theory]
    [InlineData("", true)]
    [InlineData("0xff", true)]
    [InlineData("255", true)]
    [InlineData("0", true)]
    [InlineData("0xZZ", false)]
    [InlineData("-1", false)]
    [InlineData("not-a-number", false)]
    public void ValidateConnmarkAllowlistEnable_AcceptsValid_RejectsInvalid(string input, bool valid)
    {
        var err = SpecialOptionValidators.ValidateConnmarkAllowlistEnable(input);
        Assert.Equal(valid, err is null);
    }

    [Theory]
    [InlineData("", true)]
    [InlineData("no", true)]
    [InlineData("yes", false)]
    [InlineData("0", false)]
    public void ValidateDnssecCheckUnsigned_AcceptsValid_RejectsInvalid(string input, bool valid)
    {
        var err = SpecialOptionValidators.ValidateDnssecCheckUnsigned(input);
        Assert.Equal(valid, err is null);
    }

    [Theory]
    [InlineData("", true)]
    [InlineData("10.0.0.1", true)]
    [InlineData("10.0.0.0/24", true)]
    [InlineData("::1", true)]
    [InlineData("invalid", false)]
    [InlineData("10.0.0.0/abc", false)]
    public void ValidateLeasequeryValue_AcceptsValid_RejectsInvalid(string? input, bool valid)
    {
        var err = SpecialOptionValidators.ValidateLeasequeryValue(input);
        Assert.Equal(valid, err is null);
    }
}
