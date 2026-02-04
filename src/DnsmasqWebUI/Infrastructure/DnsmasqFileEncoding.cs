using System.Text;

namespace DnsmasqWebUI.Infrastructure;

/// <summary>Encoding for dnsmasq config and hosts files. UTF-8 without BOM so dnsmasq does not see a BOM as "bad option" on line 1.</summary>
public static class DnsmasqFileEncoding
{
    public static readonly Encoding Utf8NoBom = new UTF8Encoding(false);
}
