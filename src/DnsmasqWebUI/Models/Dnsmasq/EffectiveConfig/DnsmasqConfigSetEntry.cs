namespace DnsmasqWebUI.Models.Dnsmasq.EffectiveConfig;

/// <summary>One file in the dnsmasq config set (main or included). IsManaged is true only for the app-managed file.</summary>
/// <param name="Path">Absolute path of the config file.</param>
/// <param name="FileName">Filename only (e.g. dnsmasq.conf, 01-default.conf).</param>
/// <param name="Source">Whether the file is main, conf-file, or conf-dir.</param>
/// <param name="IsManaged">True when this file is the app-managed config file (editable from UI).</param>
public record DnsmasqConfigSetEntry(string Path, string FileName, DnsmasqConfFileSource Source, bool IsManaged);
