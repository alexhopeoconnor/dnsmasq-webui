namespace DnsmasqWebUI.Models.Client.Abstractions;

/// <summary>
/// Non-generic interface for heterogeneous iteration over client settings fields.
/// </summary>
public interface IClientSettingsField
{
    string DisplayName { get; }
    object? Value { get; set; }
    string? Validate();
}
