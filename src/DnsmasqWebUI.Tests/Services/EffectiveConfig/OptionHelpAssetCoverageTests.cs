using DnsmasqWebUI.Infrastructure.Services.EffectiveConfig.Metadata;

namespace DnsmasqWebUI.Tests.Services.EffectiveConfig;

/// <summary>
/// Guards the help popover contract: option names are normalised to lowercase for case-sensitive
/// static file servers, and every field exposed through Effective Config has an asset.
/// </summary>
public class OptionHelpAssetCoverageTests
{
    [Fact]
    public void EffectiveConfigOptions_HaveLowercaseHelpAssets()
    {
        var webRoot = FindWebRoot();
        var missing = EffectiveConfigSections.Sections
            .SelectMany(section => section.OptionNames)
            .Distinct(StringComparer.Ordinal)
            .Select(optionName => (OptionName: optionName, FileName: optionName.ToLowerInvariant() + ".html"))
            .Where(expected => !File.Exists(Path.Combine(webRoot, "option-help", expected.FileName)))
            .Select(expected => expected.OptionName)
            .ToList();

        Assert.True(
            missing.Count == 0,
            $"Missing option-help assets for: {string.Join(", ", missing)}");
    }

    [Theory]
    [InlineData(DnsmasqConfKeys.FilterA, "filter-a.html")]
    [InlineData(DnsmasqConfKeys.FilterAaaa, "filter-aaaa.html")]
    [InlineData(DnsmasqConfKeys.Slaac, "slaac.html")]
    public void HelpAssetNames_AreNormalisedForCaseSensitiveStaticHosting(string optionName, string expectedFileName)
    {
        var webRoot = FindWebRoot();
        var fileName = optionName.ToLowerInvariant() + ".html";

        Assert.Equal(expectedFileName, fileName);
        Assert.True(File.Exists(Path.Combine(webRoot, "option-help", fileName)));
    }

    private static string FindWebRoot()
    {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory);
             directory != null;
             directory = directory.Parent)
        {
            var candidate = Path.Combine(directory.FullName, "src", "DnsmasqWebUI", "wwwroot");
            if (Directory.Exists(candidate))
                return candidate;
        }

        throw new DirectoryNotFoundException("Could not locate src/DnsmasqWebUI/wwwroot from the test output directory.");
    }
}
