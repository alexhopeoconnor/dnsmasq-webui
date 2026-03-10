using DnsmasqWebUI.Infrastructure.Helpers.Config;
using DnsmasqWebUI.Infrastructure.Services.EffectiveConfig.Validation;
using DnsmasqWebUI.Models.Dnsmasq.EffectiveConfig;

namespace DnsmasqWebUI.Tests;

/// <summary>
/// Tests for the central semantic validator (handler dispatch and generic kind validation).
/// </summary>
public class OptionSemanticValidatorTests
{
    private readonly IOptionSemanticValidator _validator = new OptionSemanticValidator([
        new LeasequerySemanticHandler(),
        new ServerSemanticHandler(),
        new RevServerSemanticHandler(),
        new AddressSemanticHandler(),
    ]);

    [Fact]
    public void ValidateMultiItem_LeasequeryHandler_InvalidIp_ReturnsError()
    {
        var semantics = new OptionValidationSemantics(OptionValidationKind.Complex);
        Assert.NotNull(_validator.ValidateMultiItem(DnsmasqConfKeys.Leasequery, "not-an-ip", semantics));
        Assert.Null(_validator.ValidateMultiItem(DnsmasqConfKeys.Leasequery, "10.0.0.0/24", semantics));
        Assert.Null(_validator.ValidateMultiItem(DnsmasqConfKeys.Leasequery, "", semantics));
    }

    [Theory]
    [InlineData("1.2.3.4", true)]
    [InlineData("::1", true)]
    [InlineData("", true)]
    [InlineData("x.y.z", false)]
    public void ValidateMultiItem_IpAddressKind_AcceptsValid_RejectsInvalid(string value, bool valid)
    {
        var semantics = new OptionValidationSemantics(OptionValidationKind.IpAddress, allowEmpty: true);
        var err = _validator.ValidateMultiItem("option", value, semantics);
        Assert.Equal(valid, err is null);
    }

    [Theory]
    [InlineData("", true, true)]
    [InlineData("x", true, true)]
    [InlineData("", false, false)]
    public void ValidateMultiItem_StringKind_RespectsAllowEmpty(string value, bool allowEmpty, bool expectValid)
    {
        var semantics = new OptionValidationSemantics(OptionValidationKind.String, allowEmpty: allowEmpty);
        var err = _validator.ValidateMultiItem("option", value, semantics);
        Assert.Equal(expectValid, err is null);
    }

    [Theory]
    [InlineData("8.8.8.8", true)]
    [InlineData("dns.example.com", true)]
    [InlineData("/internal.lan/192.168.2.1", true)]
    [InlineData("/google.com/#", true)]
    [InlineData("//", true)]
    [InlineData("", false)]
    [InlineData("http://bad", false)]
    [InlineData("/internal$lan/192.168.2.1", false)]
    [InlineData("/google.com/192.168.2.1#70000", false)]
    [InlineData("/google.com/@eth0", false)]
    public void ValidateMultiItem_Server_UsesHandler(string value, bool valid)
    {
        var semantics = new OptionValidationSemantics(OptionValidationKind.Complex, allowEmpty: true);
        var err = _validator.ValidateMultiItem(DnsmasqConfKeys.Server, value, semantics);
        Assert.Equal(valid, err is null);
    }

    [Theory]
    [InlineData("192.168.1.1", true)]
    [InlineData("::1", true)]
    [InlineData("", false)]
    [InlineData("dns.example.com", false)]
    public void ValidateMultiItem_ListenAddress_UsesIpAddressSemantics(string value, bool valid)
    {
        var semantics = new OptionValidationSemantics(OptionValidationKind.IpAddress, allowEmpty: false);
        var err = _validator.ValidateMultiItem(DnsmasqConfKeys.ListenAddress, value, semantics);
        Assert.Equal(valid, err is null);
    }

    [Theory]
    [InlineData("1.2.3.0/24,192.168.1.1", true)]
    [InlineData("2001:db8::/64,2001:4860:4860::8888", true)]
    [InlineData("1.2.3.0/33,192.168.1.1", false)]
    [InlineData("not-an-ip/24,192.168.1.1", false)]
    [InlineData("1.2.3.0/x,192.168.1.1", false)]
    [InlineData("1.2.3.0/24,", false)]
    public void ValidateMultiItem_RevServer_UsesHandler(string value, bool valid)
    {
        var semantics = new OptionValidationSemantics(OptionValidationKind.Complex, allowEmpty: true);
        var err = _validator.ValidateMultiItem(DnsmasqConfKeys.RevServer, value, semantics);
        Assert.Equal(valid, err is null);
    }

    [Theory]
    [InlineData("/example.local/192.168.1.10", true)]
    [InlineData("/#/1.2.3.4", true)]
    [InlineData("/example.local/#", true)]
    [InlineData("/example.local/", true)]
    [InlineData("example.local/192.168.1.10", false)]
    [InlineData("//192.168.1.10", false)]
    [InlineData("/example.local/not-an-ip", false)]
    public void ValidateMultiItem_Address_UsesHandler(string value, bool valid)
    {
        var semantics = new OptionValidationSemantics(OptionValidationKind.Complex, allowEmpty: true);
        var err = _validator.ValidateMultiItem(DnsmasqConfKeys.Address, value, semantics);
        Assert.Equal(valid, err is null);
    }

    [Fact]
    public void ValidateSingle_UseStaleCache_UsesEngineRule()
    {
        var semantics = new OptionValidationSemantics(OptionValidationKind.KeyOnlyOrValue, allowEmpty: true);
        Assert.Null(_validator.ValidateSingle(DnsmasqConfKeys.UseStaleCache, "", semantics));
        Assert.Null(_validator.ValidateSingle(DnsmasqConfKeys.UseStaleCache, "0", semantics));
        Assert.NotNull(_validator.ValidateSingle(DnsmasqConfKeys.UseStaleCache, "x", semantics));
    }

    [Fact]
    public void ValidateSingle_AddMac_UsesEngineRule()
    {
        var semantics = new OptionValidationSemantics(OptionValidationKind.KeyOnlyOrValue, allowEmpty: true);
        Assert.Null(_validator.ValidateSingle(DnsmasqConfKeys.AddMac, "", semantics));
        Assert.Null(_validator.ValidateSingle(DnsmasqConfKeys.AddMac, "base64", semantics));
        Assert.NotNull(_validator.ValidateSingle(DnsmasqConfKeys.AddMac, "bogus", semantics));
    }

    [Theory]
    [InlineData("")]
    [InlineData("24,96")]
    [InlineData("0/0")]
    public void ValidateSingle_AddSubnet_RemainsIntentionallyPermissive(string input)
    {
        var semantics = new OptionValidationSemantics(OptionValidationKind.KeyOnlyOrValue, allowEmpty: true);
        Assert.Null(_validator.ValidateSingle(DnsmasqConfKeys.AddSubnet, input, semantics));
    }

    [Theory]
    [InlineData("")]
    [InlineData("org-id,asset-id")]
    [InlineData("device-123")]
    public void ValidateSingle_Umbrella_RemainsIntentionallyPermissive(string input)
    {
        var semantics = new OptionValidationSemantics(OptionValidationKind.KeyOnlyOrValue, allowEmpty: true);
        Assert.Null(_validator.ValidateSingle(DnsmasqConfKeys.Umbrella, input, semantics));
    }

    [Theory]
    [InlineData("", true)]
    [InlineData("0xff", true)]
    [InlineData("255", true)]
    [InlineData("0xZZ", false)]
    [InlineData("-1", false)]
    public void ValidateSingle_ConnmarkAllowlistEnable_UsesEngineRule(string input, bool valid)
    {
        var semantics = new OptionValidationSemantics(OptionValidationKind.KeyOnlyOrValue, allowEmpty: true);
        var err = _validator.ValidateSingle(DnsmasqConfKeys.ConnmarkAllowlistEnable, input, semantics);
        Assert.Equal(valid, err is null);
    }

    [Theory]
    [InlineData("", true)]
    [InlineData("no", true)]
    [InlineData("yes", false)]
    public void ValidateSingle_DnssecCheckUnsigned_UsesEngineRule(string input, bool valid)
    {
        var semantics = new OptionValidationSemantics(OptionValidationKind.KeyOnlyOrValue, allowEmpty: true);
        var err = _validator.ValidateSingle(DnsmasqConfKeys.DnssecCheckUnsigned, input, semantics);
        Assert.Equal(valid, err is null);
    }

    [Fact]
    public void ValidateSingle_IntKind_AcceptsIntOrNumericString()
    {
        var semantics = new OptionValidationSemantics(OptionValidationKind.Int);
        Assert.Null(_validator.ValidateSingle("option", 42, semantics));
        Assert.Null(_validator.ValidateSingle("option", "99", semantics));
        Assert.NotNull(_validator.ValidateSingle("option", "abc", semantics));
    }

    [Fact]
    public void OptionValidationSemantics_PathPolicy_OnNonPathKind_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            new OptionValidationSemantics(
                OptionValidationKind.Int,
                pathPolicy: PathExistencePolicy.MustExist));
    }

    [Fact]
    public void ValidateMultiItem_PathFile_RejectsDirectoryPath()
    {
        var validator = _validator;
        var semantics = new OptionValidationSemantics(
            OptionValidationKind.PathFile,
            allowEmpty: true,
            pathPolicy: PathExistencePolicy.MustExist);

        var dir = Directory.CreateTempSubdirectory("dnsmasq-webui-pathfile-");
        try
        {
            var err = validator.ValidateMultiItem(DnsmasqConfKeys.DhcpHostsfile, dir.FullName, semantics);
            Assert.Equal("File does not exist.", err);
        }
        finally
        {
            dir.Delete(recursive: true);
        }
    }

    [Fact]
    public void ValidateMultiItem_PathDirectory_RejectsFilePath()
    {
        var validator = _validator;
        var semantics = new OptionValidationSemantics(
            OptionValidationKind.PathDirectory,
            allowEmpty: true,
            pathPolicy: PathExistencePolicy.MustExist);

        var filePath = Path.Combine(Path.GetTempPath(), $"dnsmasq-webui-pathdir-{Guid.NewGuid():N}.txt");
        File.WriteAllText(filePath, "test");
        try
        {
            var err = validator.ValidateMultiItem(DnsmasqConfKeys.DhcpHostsdir, filePath, semantics);
            Assert.Equal("Directory does not exist.", err);
        }
        finally
        {
            File.Delete(filePath);
        }
    }

    [Fact]
    public void ValidateMultiItem_PathFile_AcceptsExistingFile()
    {
        var validator = _validator;
        var semantics = new OptionValidationSemantics(
            OptionValidationKind.PathFile,
            allowEmpty: true,
            pathPolicy: PathExistencePolicy.MustExist);

        var filePath = Path.Combine(Path.GetTempPath(), $"dnsmasq-webui-pathfile-{Guid.NewGuid():N}.txt");
        File.WriteAllText(filePath, "test");
        try
        {
            var err = validator.ValidateMultiItem(DnsmasqConfKeys.DhcpHostsfile, filePath, semantics);
            Assert.Null(err);
        }
        finally
        {
            File.Delete(filePath);
        }
    }

    [Fact]
    public void ValidateMultiItem_PathDirectory_AcceptsExistingDirectory()
    {
        var validator = _validator;
        var semantics = new OptionValidationSemantics(
            OptionValidationKind.PathDirectory,
            allowEmpty: true,
            pathPolicy: PathExistencePolicy.MustExist);

        var dir = Directory.CreateTempSubdirectory("dnsmasq-webui-pathdir-");
        try
        {
            var err = validator.ValidateMultiItem(DnsmasqConfKeys.DhcpHostsdir, dir.FullName, semantics);
            Assert.Null(err);
        }
        finally
        {
            dir.Delete(recursive: true);
        }
    }
}
