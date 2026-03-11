using System.Collections.Generic;
using DnsmasqWebUI.Infrastructure.Services.EffectiveConfig.Abstractions;
using DnsmasqWebUI.Infrastructure.Services.EffectiveConfig.Metadata;
using DnsmasqWebUI.Infrastructure.Services.EffectiveConfig.Validation;
using DnsmasqWebUI.Infrastructure.Services.EffectiveConfig.Validation.Abstractions;
using DnsmasqWebUI.Models.Dnsmasq.EffectiveConfig;

namespace DnsmasqWebUI.Infrastructure.Services.EffectiveConfig;

/// <summary>
/// Pre-save semantic validation: runs option semantics validation on pending changes.
/// </summary>
public sealed class EffectiveConfigSemanticValidationService : IEffectiveConfigSemanticValidationService
{
    private readonly IOptionSemanticValidator _validator;

    public EffectiveConfigSemanticValidationService(IOptionSemanticValidator validator)
    {
        _validator = validator;
    }

    /// <inheritdoc />
    public IReadOnlyList<FieldIssue> Validate(IReadOnlyList<PendingEffectiveConfigChange> changes)
    {
        var issues = new List<FieldIssue>();
        foreach (var change in changes)
        {
            var semantics = EffectiveConfigSpecialOptionSemantics.TryGetSemantics(change.OptionName);
            if (semantics is null) continue;

            var fieldKey = $"{change.SectionId}:{change.OptionName}";
            var severity = semantics.Validation.Severity;

            if (change.NewValue is IReadOnlyList<string> list)
            {
                for (var i = 0; i < list.Count; i++)
                {
                    var err = _validator.ValidateMultiItem(change.OptionName, list[i], semantics.Validation);
                    if (err is not null)
                        issues.Add(new FieldIssue(fieldKey, err, severity, i));
                }
            }
            else
            {
                var err = _validator.ValidateSingle(change.OptionName, change.NewValue, semantics.Validation);
                if (err is not null)
                    issues.Add(new FieldIssue(fieldKey, err, severity, null));
            }
        }
        return issues;
    }
}
