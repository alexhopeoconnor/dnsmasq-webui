using DnsmasqWebUI.Infrastructure.Services.EffectiveConfig.Abstractions;
using DnsmasqWebUI.Models.Dnsmasq.EffectiveConfig;

namespace DnsmasqWebUI.Infrastructure.Services.EffectiveConfig;

public sealed class EffectiveMultiValueProjectionService : IEffectiveMultiValueProjectionService
{
    public IReadOnlyList<ProjectedMultiValueOccurrence> Project(
        IReadOnlyList<string> currentValues,
        IReadOnlyList<ValueWithSource>? baselineValues,
        string? managedFilePath)
    {
        if (currentValues.Count == 0)
            return [];

        var managedPath = string.IsNullOrWhiteSpace(managedFilePath) ? null : managedFilePath.Trim();
        var managedLabel = string.IsNullOrWhiteSpace(managedPath) ? null : Path.GetFileName(managedPath);
        var usedBaseline = baselineValues != null ? new bool[baselineValues.Count] : Array.Empty<bool>();
        var draftOrdinals = new Dictionary<string, int>(StringComparer.Ordinal);
        var projected = new List<ProjectedMultiValueOccurrence>(currentValues.Count);

        for (var i = 0; i < currentValues.Count; i++)
        {
            var value = currentValues[i];
            ValueWithSource? matched = null;
            var matchedBaselineIndex = -1;
            if (baselineValues != null)
            {
                for (var j = 0; j < baselineValues.Count; j++)
                {
                    if (usedBaseline[j])
                        continue;
                    if (!string.Equals(baselineValues[j].Value, value, StringComparison.Ordinal))
                        continue;

                    matched = baselineValues[j];
                    usedBaseline[j] = true;
                    matchedBaselineIndex = j;
                    break;
                }
            }

            if (matched != null)
            {
                projected.Add(new ProjectedMultiValueOccurrence(
                    OccurrenceId: $"baseline:{matchedBaselineIndex}",
                    Value: value,
                    EffectiveIndex: i,
                    Source: matched.Source,
                    IsDraftOnly: false,
                    IsEditable: matched.Source?.IsReadOnly != true,
                    DisplaySourcePath: matched.Source?.FilePath,
                    DisplaySourceLabel: matched.Source?.FileName));
                continue;
            }

            draftOrdinals.TryGetValue(value, out var draftOrdinal);
            draftOrdinals[value] = draftOrdinal + 1;
            projected.Add(new ProjectedMultiValueOccurrence(
                OccurrenceId: $"draft:{Fnv1aHash(value):X8}:{draftOrdinal}",
                Value: value,
                EffectiveIndex: i,
                Source: null,
                IsDraftOnly: true,
                IsEditable: true,
                DisplaySourcePath: managedPath,
                DisplaySourceLabel: managedLabel));
        }

        return projected;
    }

    private static uint Fnv1aHash(string s)
    {
        unchecked
        {
            const uint offset = 2166136261;
            const uint prime = 16777619;
            var h = offset;
            foreach (var c in s)
            {
                h ^= c;
                h *= prime;
            }

            return h;
        }
    }
}
