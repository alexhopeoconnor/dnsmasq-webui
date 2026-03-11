using DnsmasqWebUI.Infrastructure.Services.EffectiveConfig.Metadata;
using DnsmasqWebUI.Infrastructure.Serialization.Parsers.DnsmasqConfig;

namespace DnsmasqWebUI.Tests.Helpers;

/// <summary>Ensures corpus index and files stay in sync; fails fast when testdata drifts.</summary>
public class RealWorldCorpusIntegrityTests
{
    [Fact]
    public void EveryCorpusCase_PointsToExistingFile()
    {
        var cases = RealWorldCasesHelper.LoadCases();
        Assert.NotEmpty(cases);
        foreach (var c in cases)
        {
            var path = RealWorldCasesHelper.Resolve(c);
            Assert.True(File.Exists(path), $"Corpus file missing: {c.File}");
        }
    }

    [Fact]
    public void EveryBadCase_DeclaresSemanticInvalidOptions()
    {
        var cases = RealWorldCasesHelper.LoadCases();
        var badFiles = new[] { "bad/", "real-world/bad/" };
        foreach (var c in cases)
        {
            var isBad = badFiles.Any(p => c.File.Contains(p, StringComparison.Ordinal));
            if (isBad)
                Assert.NotEmpty(c.SemanticInvalidOptions);
        }
    }

    [Fact]
    public void SemanticInvalidOptions_ArePresentInParsedValues()
    {
        foreach (var c in RealWorldCasesHelper.LoadCases())
        {
            if (c.SemanticInvalidOptions.Count == 0)
                continue;
            var mainPath = RealWorldCasesHelper.Resolve(c);
            var paths = DnsmasqConfIncludeParser.GetIncludedPaths(mainPath);
            foreach (var option in c.SemanticInvalidOptions)
            {
                var values = DnsmasqConfIncludeParser.GetMultiValueFromConfigFiles(paths, option);
                Assert.NotEmpty(values);
            }
        }
    }
}
