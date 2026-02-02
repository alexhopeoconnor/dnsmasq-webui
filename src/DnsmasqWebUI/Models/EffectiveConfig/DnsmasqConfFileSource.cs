namespace DnsmasqWebUI.Models.EffectiveConfig;

/// <summary>Source of a file in the dnsmasq config set: main config, conf-file=, or conf-dir=.</summary>
public enum DnsmasqConfFileSource
{
    Main,
    ConfFile,
    ConfDir
}
