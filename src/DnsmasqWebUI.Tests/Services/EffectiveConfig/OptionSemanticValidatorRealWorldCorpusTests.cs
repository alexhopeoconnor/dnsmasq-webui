using DnsmasqWebUI.Infrastructure.Services.EffectiveConfig.Metadata;
using DnsmasqWebUI.Infrastructure.Services.EffectiveConfig.Validation;
using DnsmasqWebUI.Infrastructure.Services.EffectiveConfig.Validation.Abstractions;
using DnsmasqWebUI.Infrastructure.Services.EffectiveConfig.Validation.Handlers;
using DnsmasqWebUI.Infrastructure.Serialization.Parsers.DnsmasqConfig;
using DnsmasqWebUI.Models.Dnsmasq.EffectiveConfig;
using DnsmasqWebUI.Tests.Helpers;

namespace DnsmasqWebUI.Tests.Services.EffectiveConfig;

/// <summary>Corpus-driven semantic validation: bad samples produce errors for expected option keys only.</summary>
public class OptionSemanticValidatorRealWorldCorpusTests
{
    private readonly IOptionSemanticValidator _validator = new OptionSemanticValidator([
        new DomainSemanticHandler(),
        new DnsRrSemanticHandler(),
        new SynthDomainSemanticHandler(),
        new AuthServerSemanticHandler(),
        new SrvSemanticHandler(),
    ]);

    public static IEnumerable<object[]> GetCasesWithInvalidOptions()
    {
        foreach (var c in RealWorldCasesHelper.LoadCases())
        {
            if (c.SemanticInvalidOptions.Count > 0)
                yield return new object[] { c };
        }
    }

    [Theory]
    [MemberData(nameof(GetCasesWithInvalidOptions))]
    public void SemanticInvalidOptions_FromCorpus_AreRejected(CorpusCase c)
    {
        var mainPath = RealWorldCasesHelper.Resolve(c);
        var paths = DnsmasqConfIncludeParser.GetIncludedPaths(mainPath);
        var semantics = new OptionValidationSemantics(OptionValidationKind.Complex, allowEmpty: true);

        foreach (var option in c.SemanticInvalidOptions)
        {
            var values = DnsmasqConfIncludeParser.GetMultiValueFromConfigFiles(paths, option);
            Assert.NotEmpty(values);

            var anyInvalid = values.Any(v =>
                _validator.ValidateMultiItem(option, v, semantics) is not null);

            Assert.True(anyInvalid, $"Expected at least one invalid value for option '{option}' in {c.File}");
        }
    }
}
