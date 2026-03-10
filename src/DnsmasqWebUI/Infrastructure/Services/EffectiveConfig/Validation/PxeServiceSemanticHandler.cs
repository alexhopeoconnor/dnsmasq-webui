using DnsmasqWebUI.Infrastructure.Helpers.Config;

namespace DnsmasqWebUI.Infrastructure.Services.EffectiveConfig.Validation;

/// <summary>
/// Conservative semantic behavior for <c>pxe-service</c> values.
/// Validates optional leading tag and the required CSA + menu text fields.
/// </summary>
public sealed class PxeServiceSemanticHandler : IOptionSemanticHandler
{
    public bool CanHandle(string optionName) =>
        optionName == DnsmasqConfKeys.PxeService;

    public string? ValidateSingle(object? value) => null;

    public string? ValidateMultiItem(string? value)
    {
        var s = value?.Trim() ?? "";
        if (s.Length == 0)
            return "Value cannot be empty.";

        var tokens = s.Split(',', StringSplitOptions.TrimEntries);
        if (tokens.Length < 2 || tokens.Any(t => t.Length == 0))
            return "pxe-service must include a CSA and menu text.";

        var index = 0;
        if (tokens[0].StartsWith("tag:", StringComparison.OrdinalIgnoreCase))
        {
            if (tokens[0].Length <= 4)
                return "pxe-service tag: value cannot be empty.";
            index++;
        }

        if (index >= tokens.Length)
            return "pxe-service must include a client system architecture value.";

        if (!IsCsa(tokens[index]))
            return $"Invalid pxe-service CSA '{tokens[index]}'.";

        index++;
        if (index >= tokens.Length)
            return "pxe-service must include menu text.";

        return null;
    }

    private static bool IsCsa(string value) =>
        int.TryParse(value, out _) ||
        value is
            "x86PC" or
            "PC98" or
            "IA64_EFI" or
            "Alpha" or
            "Arc_x86" or
            "Intel_Lean_Client" or
            "IA32_EFI" or
            "x86-64_EFI" or
            "Xscale_EFI" or
            "BC_EFI" or
            "ARM32_EFI" or
            "ARM64_EFI";
}
