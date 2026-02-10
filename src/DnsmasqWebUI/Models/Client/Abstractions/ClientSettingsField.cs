namespace DnsmasqWebUI.Models.Client.Abstractions;

/// <summary>
/// Generic base for client settings fields. Holds the value and provides coercion on set.
/// </summary>
public abstract class ClientSettingsField<T> : IClientSettingsField
{
    public string DisplayName { get; }
    private T _value = default!;

    public virtual T Value
    {
        get => _value;
        set => _value = CoerceValue(value);
    }

    object? IClientSettingsField.Value
    {
        get => Value;
        set => Value = (T)value!;
    }

    public abstract string? Validate(T value);

    string? IClientSettingsField.Validate() => Validate(Value);

    protected virtual T CoerceValue(T value) => value;

    protected ClientSettingsField(string displayName)
    {
        DisplayName = displayName;
    }
}
