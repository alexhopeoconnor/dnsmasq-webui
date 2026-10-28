using System.Globalization;
using DnsmasqWebUI.Infrastructure.Helpers.Config;
using DnsmasqWebUI.Infrastructure.Serialization.Parsers.DnsmasqConfig;
using DnsmasqWebUI.Infrastructure.Services.EffectiveConfig.Metadata;
using DnsmasqWebUI.Models.Dnsmasq.EffectiveConfig;
using DnsmasqWebUI.Models.Hosts;

namespace DnsmasqWebUI.Infrastructure.Services.EffectiveConfig;

/// <summary>
/// Checks that the state a pending draft was based on is still current immediately before a
/// managed-file write. The comparison is semantic: unrelated comments and unrelated directives do
/// not block a save, but a changed effective value for a field being edited does.
/// </summary>
internal static class EffectiveConfigSaveConflictDetector
{
    private static readonly HashSet<string> ResolvedPathOptions = new(StringComparer.Ordinal)
    {
        DnsmasqConfKeys.Hostsdir,
        DnsmasqConfKeys.PidFile,
        DnsmasqConfKeys.TftpRoot,
        DnsmasqConfKeys.DhcpScript,
        DnsmasqConfKeys.Dumpfile,
        DnsmasqConfKeys.DhcpLuascript,
    };

    /// <summary>
    /// Finds pending option changes whose current effective value no longer matches the value the
    /// browser was editing. The config is read directly rather than through a potentially stale UI
    /// projection, while preserving dnsmasq's in-place include order.
    /// </summary>
    public static EffectiveConfigSaveConflictCheck CheckOptions(
        DnsmasqConfigSet set,
        IReadOnlyList<PendingOptionChange> changes)
    {
        if (changes.Count == 0)
            return EffectiveConfigSaveConflictCheck.Current;

        try
        {
            if (string.IsNullOrWhiteSpace(set.MainConfigPath) || !File.Exists(set.MainConfigPath))
            {
                return new EffectiveConfigSaveConflictCheck(
                    Array.Empty<string>(),
                    "The main dnsmasq configuration file is unavailable.");
            }

            var occurrences = ReadEffectiveOccurrences(set);
            var conflicts = new List<string>();
            foreach (var change in changes)
            {
                var current = ReadOptionValue(occurrences, change.OptionName);
                if (!ValuesEqual(change.OldValue, current))
                    conflicts.Add(change.OptionName);
            }

            return conflicts.Count == 0
                ? EffectiveConfigSaveConflictCheck.Current
                : new EffectiveConfigSaveConflictCheck(conflicts, null);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException or NotSupportedException)
        {
            return new EffectiveConfigSaveConflictCheck(
                Array.Empty<string>(),
                "The current dnsmasq configuration could not be checked safely.");
        }
    }

    /// <summary>
    /// Checks the exact managed Hosts document against the draft baseline. Hosts entries are
    /// canonicalised using the same serializer used for writes, so harmless whitespace changes in
    /// ordinary records do not produce a false conflict while content/order/comment changes do.
    /// </summary>
    public static EffectiveConfigSaveConflictCheck CheckManagedHosts(PendingManagedHostsChange change)
    {
        try
        {
            var current = ReadManagedHostsEntries(change.ManagedHostsFilePath);
            return ManagedHostsEntriesEqual(change.OldEntries, current)
                ? EffectiveConfigSaveConflictCheck.Current
                : new EffectiveConfigSaveConflictCheck(["managed hosts"], null);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException or NotSupportedException)
        {
            return new EffectiveConfigSaveConflictCheck(
                Array.Empty<string>(),
                "The managed Hosts file could not be checked safely.");
        }
    }

    private static IReadOnlyList<DnsmasqConfigLineOccurrence> ReadEffectiveOccurrences(DnsmasqConfigSet set)
    {
        if (string.IsNullOrWhiteSpace(set.MainConfigPath))
            return Array.Empty<DnsmasqConfigLineOccurrence>();

        var occurrences = DnsmasqConfIncludeParser
            .GetConfigLineOccurrencesInDnsmasqOrder(set.MainConfigPath)
            .ToList();

        // During startup the managed file can exist before the main config includes it. Mirror the
        // cache's established managed-overlay contract so a draft sees the same effective state as
        // the rest of the application.
        if (string.IsNullOrWhiteSpace(set.ManagedFilePath))
            return occurrences;

        var managedPath = Path.GetFullPath(set.ManagedFilePath);
        if (occurrences.Any(o => string.Equals(o.FilePath, managedPath, StringComparison.Ordinal)))
            return occurrences;
        if (!File.Exists(managedPath))
            return occurrences;

        var lines = DnsmasqFileEncoding.ReadConfigLines(managedPath);
        for (var index = 0; index < lines.Length; index++)
        {
            var line = lines[index];
            var directive = DnsmasqConfDirectiveParser.TryParseKeyValue(line);
            if (directive == null ||
                string.Equals(directive.Value.key, DnsmasqConfKeys.ConfFile, StringComparison.Ordinal) ||
                string.Equals(directive.Value.key, DnsmasqConfKeys.ConfDir, StringComparison.Ordinal))
            {
                continue;
            }

            occurrences.Add(new DnsmasqConfigLineOccurrence(
                managedPath,
                index + 1,
                line,
                DnsmasqConfFileSource.ConfFile));
        }

        return occurrences;
    }

    private static object? ReadOptionValue(
        IReadOnlyList<DnsmasqConfigLineOccurrence> occurrences,
        string optionName)
    {
        if (EffectiveConfigWriteSemantics.GetBehavior(optionName) == EffectiveConfigWriteBehavior.InversePair)
        {
            var pair = EffectiveConfigWriteSemantics.GetInversePairKeys(optionName);
            if (pair == null)
                return ExplicitToggleState.Default;

            var enabled = DnsmasqConfIncludeParser.GetFlagFromConfigOccurrences(occurrences, pair.Value.KeyA);
            var disabled = DnsmasqConfIncludeParser.GetFlagFromConfigOccurrences(occurrences, pair.Value.KeyB);
            return disabled
                ? ExplicitToggleState.Disabled
                : enabled ? ExplicitToggleState.Enabled : ExplicitToggleState.Default;
        }

        return EffectiveConfigParserBehaviorMap.GetBehavior(optionName) switch
        {
            EffectiveConfigParserBehavior.Flag => DnsmasqConfIncludeParser.GetFlagFromConfigOccurrences(occurrences, optionName),
            EffectiveConfigParserBehavior.Multi => ReadMultiValue(occurrences, optionName),
            _ => ReadLastValue(occurrences, optionName),
        };
    }

    private static object? ReadMultiValue(
        IReadOnlyList<DnsmasqConfigLineOccurrence> occurrences,
        string optionName)
    {
        if (string.Equals(optionName, DnsmasqConfKeys.AddnHosts, StringComparison.Ordinal))
            return DnsmasqConfIncludeParser.GetAddnHostsPathsFromConfigOccurrences(occurrences);

        return DnsmasqConfIncludeParser.GetMultiValueFromConfigOccurrences(occurrences, optionName);
    }

    private static object? ReadLastValue(
        IReadOnlyList<DnsmasqConfigLineOccurrence> occurrences,
        string optionName)
    {
        if (string.Equals(optionName, DnsmasqConfKeys.DhcpLeasefile, StringComparison.Ordinal))
            return DnsmasqConfIncludeParser.GetDhcpLeaseFilePathFromConfigOccurrences(occurrences);

        var (rawValue, sourceDirectory) = DnsmasqConfIncludeParser
            .GetLastValueFromConfigOccurrences(occurrences, optionName);

        if (ResolvedPathOptions.Contains(optionName))
        {
            return string.IsNullOrWhiteSpace(rawValue)
                ? null
                : DnsmasqConfIncludeParser.ResolvePath(rawValue.Trim(), sourceDirectory);
        }

        if (IsNumericOption(optionName))
        {
            return int.TryParse(rawValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed)
                ? parsed
                : null;
        }

        if (EffectiveConfigWriteSemantics.GetBehavior(optionName) == EffectiveConfigWriteBehavior.KeyOnlyOrValue)
            return rawValue == null ? null : rawValue.Trim();

        return string.IsNullOrWhiteSpace(rawValue) ? null : rawValue.Trim();
    }

    private static bool IsNumericOption(string optionName) => optionName is
        DnsmasqConfKeys.CacheSize or
        DnsmasqConfKeys.Port or
        DnsmasqConfKeys.LocalTtl or
        DnsmasqConfKeys.AuthTtl or
        DnsmasqConfKeys.EdnsPacketMax or
        DnsmasqConfKeys.QueryPort or
        DnsmasqConfKeys.PortLimit or
        DnsmasqConfKeys.MinPort or
        DnsmasqConfKeys.MaxPort or
        DnsmasqConfKeys.DhcpLeaseMax or
        DnsmasqConfKeys.NegTtl or
        DnsmasqConfKeys.MaxTtl or
        DnsmasqConfKeys.MaxCacheTtl or
        DnsmasqConfKeys.MinCacheTtl or
        DnsmasqConfKeys.DhcpTtl or
        DnsmasqConfKeys.DnsForwardMax;

    private static bool ValuesEqual(object? baseline, object? current)
    {
        if (baseline is IReadOnlyList<string> || current is IReadOnlyList<string>)
        {
            var expected = baseline as IReadOnlyList<string> ?? Array.Empty<string>();
            var actual = current as IReadOnlyList<string> ?? Array.Empty<string>();
            return expected.SequenceEqual(actual, StringComparer.Ordinal);
        }

        if (baseline is IEnumerable<string> baselineEnumerable && baseline is not string)
        {
            var expected = baselineEnumerable.ToList();
            var actual = current as IEnumerable<string>;
            return actual != null && expected.SequenceEqual(actual, StringComparer.Ordinal);
        }

        if (baseline is int expectedInt)
            return current is int currentInt && expectedInt == currentInt;

        // Draft values normally come from typed field descriptors, but older browser sessions and
        // callers can carry numeric values as strings. Treat equivalent numeric representations as
        // the same effective value rather than rejecting a perfectly current draft.
        if (baseline is string expectedText && current is int currentNumber)
            return int.TryParse(expectedText, NumberStyles.Integer, CultureInfo.InvariantCulture, out var expectedNumber)
                   && expectedNumber == currentNumber;

        return Equals(baseline, current);
    }

    private static IReadOnlyList<HostEntry> ReadManagedHostsEntries(string path)
    {
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
            return Array.Empty<HostEntry>();

        var lines = File.ReadAllLines(path, DnsmasqFileEncoding.Utf8NoBom);
        var entries = new List<HostEntry>(lines.Length);
        for (var index = 0; index < lines.Length; index++)
        {
            var entry = HostsFileLineParser.ParseLine(lines[index], index + 1);
            if (entry != null)
                entries.Add(entry);
        }
        return entries;
    }

    private static bool ManagedHostsEntriesEqual(
        IReadOnlyList<HostEntry> baseline,
        IReadOnlyList<HostEntry> current)
    {
        if (baseline.Count != current.Count)
            return false;

        for (var index = 0; index < baseline.Count; index++)
        {
            if (!string.Equals(
                    HostsFileLineParser.ToLine(baseline[index]),
                    HostsFileLineParser.ToLine(current[index]),
                    StringComparison.Ordinal))
            {
                return false;
            }
        }
        return true;
    }
}

/// <summary>Result of an optimistic save freshness check.</summary>
internal sealed record EffectiveConfigSaveConflictCheck(
    IReadOnlyList<string> ConflictingFields,
    string? VerificationFailure)
{
    public static EffectiveConfigSaveConflictCheck Current { get; } = new(Array.Empty<string>(), null);

    public bool IsCurrent => ConflictingFields.Count == 0 && VerificationFailure == null;
}
