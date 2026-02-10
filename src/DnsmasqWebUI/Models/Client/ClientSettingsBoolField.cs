using DnsmasqWebUI.Models.Client.Abstractions;

namespace DnsmasqWebUI.Models.Client;

/// <summary>
/// Boolean field. No validation or coercion.
/// </summary>
public sealed class ClientSettingsBoolField : ClientSettingsField<bool>
{
    public ClientSettingsBoolField(string displayName) : base(displayName) { }

    public override string? Validate(bool value) => null;
}
