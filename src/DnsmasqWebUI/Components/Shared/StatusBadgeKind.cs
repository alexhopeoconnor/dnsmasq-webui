namespace DnsmasqWebUI.Components.Shared;

/// <summary>Semantic badge intent for <see cref="StatusBadge"/>; maps to consistent CSS and behavior.</summary>
public enum StatusBadgeKind
{
    Managed,
    SourceMain,
    SourceInclude,
    Editable,
    ReadOnly,
    Pending,
    ActionOk,
    ActionCancel,
    ActionRevert,
    NeutralPill
}
