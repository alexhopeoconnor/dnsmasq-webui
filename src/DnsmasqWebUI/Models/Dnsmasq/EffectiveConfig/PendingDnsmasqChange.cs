namespace DnsmasqWebUI.Models.Dnsmasq.EffectiveConfig;

/// <summary>Base type for a pending change (config option or managed hosts). Used by the edit session and save flow.</summary>
public abstract record PendingDnsmasqChange(string ChangeKey);
