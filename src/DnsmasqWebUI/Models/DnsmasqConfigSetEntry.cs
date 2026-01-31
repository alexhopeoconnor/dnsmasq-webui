namespace DnsmasqWebUI.Models;

/// <summary>One file in the dnsmasq config set (main or included). IsManaged is true only for the app-managed file.</summary>
public record DnsmasqConfigSetEntry(string Path, string FileName, DnsmasqConfFileSource Source, bool IsManaged);
