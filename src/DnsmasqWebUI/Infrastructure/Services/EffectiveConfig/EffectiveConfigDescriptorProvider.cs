using DnsmasqWebUI.Infrastructure.Services.EffectiveConfig.Abstractions;
using DnsmasqWebUI.Infrastructure.Services.EffectiveConfig.Metadata;
using DnsmasqWebUI.Models.Dnsmasq;
using DnsmasqWebUI.Models.Dnsmasq.EffectiveConfig;

namespace DnsmasqWebUI.Infrastructure.Services.EffectiveConfig;

/// <summary>
/// Canonical descriptor lookup/filter; uses the DI-managed field builder and caches per status reference
/// so UI and PageEditor share the same graph.
/// </summary>
public sealed class EffectiveConfigDescriptorProvider : IEffectiveConfigDescriptorProvider
{
    private readonly IEffectiveConfigFieldBuilder _fieldBuilder;
    private DnsmasqServiceStatus? _cachedStatus;
    private IReadOnlyList<EffectiveConfigFieldDescriptor>? _cachedList;

    public EffectiveConfigDescriptorProvider(IEffectiveConfigFieldBuilder fieldBuilder)
    {
        _fieldBuilder = fieldBuilder ?? throw new ArgumentNullException(nameof(fieldBuilder));
    }

    /// <inheritdoc />
    public IReadOnlyList<EffectiveConfigFieldDescriptor> GetAll(DnsmasqServiceStatus status)
    {
        if (status == null)
            throw new ArgumentNullException(nameof(status));
        if (ReferenceEquals(_cachedStatus, status) && _cachedList != null)
            return _cachedList;
        _cachedStatus = status;
        _cachedList = _fieldBuilder.BuildFieldDescriptors(status);
        return _cachedList;
    }

    /// <inheritdoc />
    public IReadOnlyDictionary<string, IReadOnlyList<EffectiveConfigFieldDescriptor>> GetBySection(
        DnsmasqServiceStatus status,
        IReadOnlyList<EffectiveConfigSectionView> views)
    {
        if (status == null)
            throw new ArgumentNullException(nameof(status));
        if (views == null || views.Count == 0)
            return new Dictionary<string, IReadOnlyList<EffectiveConfigFieldDescriptor>>(StringComparer.OrdinalIgnoreCase);

        var all = GetAll(status);
        var result = new Dictionary<string, List<EffectiveConfigFieldDescriptor>>(StringComparer.OrdinalIgnoreCase);

        foreach (var view in views)
        {
            var list = all
                .Where(d => string.Equals(d.SectionId, view.SectionId, StringComparison.OrdinalIgnoreCase))
                .Where(d => view.AllowedOptionNames == null
                    || view.AllowedOptionNames.Contains(d.OptionName, StringComparer.OrdinalIgnoreCase))
                .ToList();
            if (list.Count > 0)
                result[view.SectionId] = list;
        }

        return result.ToDictionary(kv => kv.Key, kv => (IReadOnlyList<EffectiveConfigFieldDescriptor>)kv.Value, StringComparer.OrdinalIgnoreCase);
    }

    /// <inheritdoc />
    public EffectiveConfigFieldDescriptor? Resolve(DnsmasqServiceStatus status, EffectiveConfigFieldRef field)
    {
        if (status == null)
            throw new ArgumentNullException(nameof(status));
        var all = GetAll(status);
        return all.FirstOrDefault(d =>
            string.Equals(d.SectionId, field.SectionId, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(d.OptionName, field.OptionName, StringComparison.OrdinalIgnoreCase));
    }
}
