namespace DnsmasqWebUI.Models.Dnsmasq.EffectiveConfig;

/// <summary>Kind of managed file written by the save flow.</summary>
public enum DnsmasqManagedTargetKind
{
    ManagedConfig,
    ManagedHosts
}

/// <summary>One file that may be written during save (config or managed hosts).</summary>
public sealed record DnsmasqManagedWriteTarget(
    DnsmasqManagedTargetKind Kind,
    string TargetPath);

/// <summary>One backup created before write (target path + backup path).</summary>
public sealed record DnsmasqManagedBackup(
    DnsmasqManagedTargetKind Kind,
    string TargetPath,
    string BackupPath);

/// <summary>Plan built from pending changes: which option changes, hosts change, and which targets to back up and write.</summary>
public sealed record DnsmasqSaveWritePlan(
    IReadOnlyList<PendingOptionChange> OptionChanges,
    PendingManagedHostsChange? ManagedHostsChange,
    IReadOnlyList<DnsmasqManagedWriteTarget> Targets);
