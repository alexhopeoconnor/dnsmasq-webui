using DnsmasqWebUI.Models.Client.Abstractions;

namespace DnsmasqWebUI.Models.Client;

/// <summary>
/// Min/max bounded integer field with optional clamping on set.
/// </summary>
public sealed class ClientSettingsMinMaxField : ClientSettingsField<int>
{
    public int Min { get; }
    public int Max { get; }
    public string Unit { get; }
    public bool AllowClamp { get; }

    public ClientSettingsMinMaxField(string displayName, int min, int max, string unit = "seconds", bool allowClamp = true)
        : base(displayName)
    {
        Min = min;
        Max = max;
        Unit = unit;
        AllowClamp = allowClamp;
    }

    public override string? Validate(int value)
    {
        if (value < Min || value > Max)
            return $"{DisplayName} must be between {Min} and {Max} {Unit}. You entered {value}.";
        return null;
    }

    protected override int CoerceValue(int value) =>
        AllowClamp ? Math.Clamp(value, Min, Max) : value;
}
