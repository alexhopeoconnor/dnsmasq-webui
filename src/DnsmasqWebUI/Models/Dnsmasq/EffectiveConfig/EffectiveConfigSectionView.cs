namespace DnsmasqWebUI.Models.Dnsmasq.EffectiveConfig;

/// <summary>
/// One section in a context view: section id, display title, and optional allow-list of option names.
/// When AllowedOptionNames is null, all fields in that section are shown; otherwise only listed options.
/// </summary>
/// <param name="SectionId">Matches EffectiveConfigSections.SectionXxx.</param>
/// <param name="Title">Display title (e.g. "Hosts", "Resolver / DNS").</param>
/// <param name="AllowedOptionNames">Null = all fields; otherwise only descriptors whose OptionName is in this list (case-insensitive).</param>
public record EffectiveConfigSectionView(
    string SectionId,
    string Title,
    IReadOnlyList<string>? AllowedOptionNames
);
