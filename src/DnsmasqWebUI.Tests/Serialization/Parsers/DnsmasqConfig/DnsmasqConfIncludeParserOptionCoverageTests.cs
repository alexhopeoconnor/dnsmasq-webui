using DnsmasqWebUI.Infrastructure.Services.EffectiveConfig.Metadata;
using DnsmasqWebUI.Infrastructure.Serialization.Parsers.DnsmasqConfig;
using DnsmasqWebUI.Tests.Helpers;

namespace DnsmasqWebUI.Tests.Serialization.Parsers.DnsmasqConfig;

/// <summary>
/// Ensures every option in the coverage list is parsed consistently: one config line per option,
/// dispatch by parser behavior (Flag / LastWins / Multi), assert we get a value back.
/// Add new options to <see cref="OptionCoverageData.GetParserCoverageEntries"/> when adding to the app.
/// </summary>
public class DnsmasqConfIncludeParserOptionCoverageTests
{
    public static IEnumerable<object[]> GetCoverageEntries()
    {
        foreach (var (optionKey, configLine) in OptionCoverageData.GetParserCoverageEntries())
            yield return new object[] { optionKey, configLine };
    }

    [Theory]
    [MemberData(nameof(GetCoverageEntries))]
    public void EveryOption_ParseSingleLine_ReturnsExpectedResult(string optionKey, string configLine)
    {
        var dir = Path.Combine(Path.GetTempPath(), "dnsmasq-opt-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        var conf = Path.Combine(dir, "dnsmasq.conf");
        try
        {
            File.WriteAllText(conf, configLine + "\n");
            var paths = new[] { conf };

            if (optionKey == DnsmasqConfKeys.AddnHosts)
            {
                var addn = DnsmasqConfIncludeParser.GetAddnHostsPathsFromConfigFiles(paths);
                Assert.True(addn.Count >= 1, $"addn-hosts should parse from: {configLine}");
                return;
            }

            var behavior = EffectiveConfigParserBehaviorMap.GetBehavior(optionKey);

            switch (behavior)
            {
                case EffectiveConfigParserBehavior.Flag:
                {
                    var flag = DnsmasqConfIncludeParser.GetFlagFromConfigFiles(paths, optionKey);
                    Assert.True(flag, $"Flag option {optionKey} should be true when set.");
                    break;
                }
                case EffectiveConfigParserBehavior.LastWins:
                {
                    var (value, _) = DnsmasqConfIncludeParser.GetLastValueFromConfigFiles(paths, optionKey);
                    Assert.NotNull(value);
                    Assert.True(value!.Length > 0, $"LastWins option {optionKey} should return non-empty value.");
                    break;
                }
                case EffectiveConfigParserBehavior.Multi:
                {
                    var list = DnsmasqConfIncludeParser.GetMultiValueFromConfigFiles(paths, optionKey);
                    Assert.True(list.Count >= 1, $"Multi option {optionKey} should return at least one value.");
                    break;
                }
                default:
                    Assert.Fail($"Unknown behavior for {optionKey}");
                    break;
            }
        }
        finally
        {
            if (Directory.Exists(dir))
                Directory.Delete(dir, recursive: true);
        }
    }
}
