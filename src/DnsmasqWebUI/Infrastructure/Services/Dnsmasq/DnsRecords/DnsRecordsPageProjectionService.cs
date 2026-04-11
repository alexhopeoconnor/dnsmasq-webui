using System.Linq;
using DnsmasqWebUI.Infrastructure.Services.Dnsmasq.DnsRecords.Abstractions;
using DnsmasqWebUI.Infrastructure.Services.EffectiveConfig.Abstractions;
using DnsmasqWebUI.Infrastructure.Services.EffectiveConfig.Metadata;
using DnsmasqWebUI.Infrastructure.Services.EffectiveConfig.Validation.Abstractions;
using DnsmasqWebUI.Models.Dnsmasq;
using DnsmasqWebUI.Models.Dnsmasq.EffectiveConfig;
using DnsmasqWebUI.Models.DnsRecords;

namespace DnsmasqWebUI.Infrastructure.Services.Dnsmasq.DnsRecords;

public sealed class DnsRecordsPageProjectionService : IDnsRecordsPageProjectionService
{
    private readonly IDnsRecordDirectiveCodecProvider _codecs;
    private readonly IOptionSemanticValidator _semanticValidator;
    private readonly IEffectiveMultiValueProjectionService _multiValueProjection;
    private readonly DnsRecordConflictAnalyzer _conflicts = new();

    public DnsRecordsPageProjectionService(
        IDnsRecordDirectiveCodecProvider codecs,
        IOptionSemanticValidator semanticValidator,
        IEffectiveMultiValueProjectionService multiValueProjection)
    {
        _codecs = codecs;
        _semanticValidator = semanticValidator;
        _multiValueProjection = multiValueProjection;
    }

    /// <inheritdoc />
    public IReadOnlyList<DnsRecordRow> BuildRows(
        DnsmasqServiceStatus status,
        Func<string, IReadOnlyList<string>>? currentValuesAccessor = null)
    {
        if (status.EffectiveConfig == null)
            return [];

        var list = new List<DnsRecordRow>();
        foreach (var optionName in _codecs.DnsRecordsSectionOptionNames)
        {
            if (!_codecs.TryGet(optionName, out var codec) || codec == null)
                continue;

            var currentValues = currentValuesAccessor?.Invoke(optionName) ?? GetPlainValues(status.EffectiveConfig, optionName);
            var items = ProjectOccurrences(status, optionName, currentValues);
            for (var i = 0; i < items.Count; i++)
            {
                var item = items[i];
                var row = codec.Parse(new ValueWithSource(item.Value, item.Source), item.EffectiveIndex);
                var issues = new List<DnsRecordIssue>();
                var semantics = EffectiveConfigSpecialOptionSemantics.TryGetSemantics(optionName);
                if (semantics != null)
                {
                    var err = _semanticValidator.ValidateMultiItem(optionName, item.Value, semantics.Validation);
                    if (err != null)
                        issues.Add(new DnsRecordIssue(err, semantics.Validation.Severity));
                }

                row = row with
                {
                    OccurrenceId = item.OccurrenceId,
                    Issues = issues,
                    SourcePath = item.DisplaySourcePath,
                    SourceLabel = item.DisplaySourceLabel,
                    IsDraftOnly = item.IsDraftOnly,
                    IsEditable = item.IsEditable
                };
                list.Add(row);
            }
        }

        var conflictMap = _conflicts.Analyze(list);
        return list.Select(r =>
        {
            if (!conflictMap.TryGetValue(r.Id, out var extra) || extra.Count == 0)
                return r;
            var merged = r.Issues.Concat(extra).ToList();
            return r with { Issues = merged };
        }).ToList();
    }

    /// <inheritdoc />
    public IReadOnlyList<DnsRecordRow> FilterRows(IReadOnlyList<DnsRecordRow> rows, DnsRecordsQueryState query)
    {
        IEnumerable<DnsRecordRow> q = rows;

        if (!query.ShowReadOnly)
            q = q.Where(r => r.IsEditable);

        if (query.OnlyWithIssues)
            q = q.Where(r => r.Issues.Count > 0);

        if (!string.IsNullOrWhiteSpace(query.SourcePathFilter))
        {
            var path = query.SourcePathFilter.Trim();
            q = q.Where(r => string.Equals(r.SourcePath, path, StringComparison.OrdinalIgnoreCase));
        }

        if (query.UiFamily != DnsRecordsUiFamily.All)
        {
            q = q.Where(r => MatchesUiFamily(r, query.UiFamily));
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var s = query.Search.Trim();
            q = q.Where(r => RowMatchesSearch(r, s));
        }

        return q.ToList();
    }

    public IReadOnlyList<ProjectedMultiValueOccurrence> ProjectOccurrences(
        DnsmasqServiceStatus status,
        string optionName,
        IReadOnlyList<string> currentValues)
    {
        return _multiValueProjection.Project(
            currentValues,
            GetBaselineValues(status, optionName),
            status.ManagedFilePath);
    }

    private static bool MatchesUiFamily(DnsRecordRow row, DnsRecordsUiFamily ui) => ui switch
    {
        DnsRecordsUiFamily.Advanced => row.Family.IsAdvancedFamily(),
        DnsRecordsUiFamily.Cname => row.Family == DnsRecordFamily.Cname,
        DnsRecordsUiFamily.HostRecord => row.Family == DnsRecordFamily.HostRecord,
        DnsRecordsUiFamily.Txt => row.Family == DnsRecordFamily.Txt,
        DnsRecordsUiFamily.Ptr => row.Family == DnsRecordFamily.Ptr,
        DnsRecordsUiFamily.Mx => row.Family == DnsRecordFamily.Mx,
        DnsRecordsUiFamily.Srv => row.Family == DnsRecordFamily.Srv,
        _ => true
    };

    private static bool RowMatchesSearch(DnsRecordRow row, string needle)
    {
        var haystack = string.Join('\u001f',
            row.Family.ToString(),
            row.OptionName,
            row.RawValue,
            row.Summary,
            row.SourcePath ?? "",
            row.SourceLabel ?? "");
        return haystack.Contains(needle, StringComparison.OrdinalIgnoreCase);
    }

    private static IReadOnlyList<ValueWithSource>? GetBaselineValues(
        DnsmasqServiceStatus status,
        string optionName)
    {
        var sources = status.EffectiveConfigSources;
        return optionName switch
        {
            DnsmasqConfKeys.Cname => sources?.CnameValues,
            DnsmasqConfKeys.MxHost => sources?.MxHostValues,
            DnsmasqConfKeys.Srv => sources?.SrvValues,
            DnsmasqConfKeys.PtrRecord => sources?.PtrRecordValues,
            DnsmasqConfKeys.TxtRecord => sources?.TxtRecordValues,
            DnsmasqConfKeys.NaptrRecord => sources?.NaptrRecordValues,
            DnsmasqConfKeys.HostRecord => sources?.HostRecordValues,
            DnsmasqConfKeys.DynamicHost => sources?.DynamicHostValues,
            DnsmasqConfKeys.InterfaceName => sources?.InterfaceNameValues,
            DnsmasqConfKeys.CaaRecord => sources?.CaaRecordValues,
            DnsmasqConfKeys.DnsRr => sources?.DnsRrValues,
            DnsmasqConfKeys.SynthDomain => sources?.SynthDomainValues,
            DnsmasqConfKeys.AuthZone => sources?.AuthZoneValues,
            DnsmasqConfKeys.AuthSoa => sources?.AuthSoaValues,
            DnsmasqConfKeys.AuthSecServers => sources?.AuthSecServersValues,
            DnsmasqConfKeys.AuthPeer => sources?.AuthPeerValues,
            _ => null
        };
    }

    private static IReadOnlyList<string> GetPlainValues(
        EffectiveDnsmasqConfig ec,
        string optionName)
    {
        IReadOnlyList<string>? plain = optionName switch
        {
            DnsmasqConfKeys.Cname => ec.CnameValues,
            DnsmasqConfKeys.MxHost => ec.MxHostValues,
            DnsmasqConfKeys.Srv => ec.SrvValues,
            DnsmasqConfKeys.PtrRecord => ec.PtrRecordValues,
            DnsmasqConfKeys.TxtRecord => ec.TxtRecordValues,
            DnsmasqConfKeys.NaptrRecord => ec.NaptrRecordValues,
            DnsmasqConfKeys.HostRecord => ec.HostRecordValues,
            DnsmasqConfKeys.DynamicHost => ec.DynamicHostValues,
            DnsmasqConfKeys.InterfaceName => ec.InterfaceNameValues,
            DnsmasqConfKeys.CaaRecord => ec.CaaRecordValues,
            DnsmasqConfKeys.DnsRr => ec.DnsRrValues,
            DnsmasqConfKeys.SynthDomain => ec.SynthDomainValues,
            DnsmasqConfKeys.AuthZone => ec.AuthZoneValues,
            DnsmasqConfKeys.AuthSoa => ec.AuthSoaValues,
            DnsmasqConfKeys.AuthSecServers => ec.AuthSecServersValues,
            DnsmasqConfKeys.AuthPeer => ec.AuthPeerValues,
            _ => null
        };

        if (plain == null || plain.Count == 0)
            return [];
        return plain;
    }
}
