using DnsmasqWebUI.Models.Dnsmasq.EffectiveConfig;

namespace DnsmasqWebUI.Models.Dnsmasq.EffectiveConfig;

/// <summary>
/// Raised when the user clicks the readonly badge in edit mode. Section shows ReadonlyEditPopover under the badge.
/// </summary>
public record ReadonlyBadgeClickedEventArgs(
    string AnchorId,
    ConfigValueSource Source,
    string OptionName,
    object? Value);
