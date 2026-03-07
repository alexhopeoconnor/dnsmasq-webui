namespace DnsmasqWebUI.Infrastructure.Services.EffectiveConfig.Validation;

/// <summary>
/// Edit behavior for the server multi-value option: placeholder, no duplicates, normalize (trim).
/// </summary>
public sealed class ServerMultiBehavior : IMultiValueEditBehavior
{
    public string Placeholder => "IP or hostname (e.g. 8.8.8.8 or dns.example.com)";
    public bool AllowDuplicates => false;

    public string Normalize(string input) => (input ?? "").Trim();
}
