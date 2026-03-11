using System.Reflection;
using DnsmasqWebUI.Infrastructure.Serialization.OptionHandlers.Abstractions;
using DnsmasqWebUI.Infrastructure.Services.EffectiveConfig.Metadata;

namespace DnsmasqWebUI.Infrastructure.Serialization.OptionHandlers;

/// <summary>
/// Registry of structured option handlers. Validates handler option names and semantics metadata at construction time
/// so runtime resolution is fail-fast and consistent.
/// </summary>
public sealed class StructuredOptionValueHandlerRegistry : IStructuredOptionValueHandlerRegistry
{
    private readonly IReadOnlyDictionary<string, IStructuredOptionValueHandler> _byOption;

    public StructuredOptionValueHandlerRegistry(IEnumerable<IStructuredOptionValueHandler> handlers)
    {
        var handlerList = (handlers ?? Array.Empty<IStructuredOptionValueHandler>()).ToList();
        var validOptionNames = GetDnsmasqConfKeyValues();

        foreach (var handler in handlerList)
        {
            if (!validOptionNames.Contains(handler.OptionName))
                throw new InvalidOperationException(
                    $"Structured handler '{handler.GetType().Name}' uses unknown option name '{handler.OptionName}'. Use DnsmasqConfKeys.");
        }

        try
        {
            _byOption = handlerList.ToDictionary(h => h.OptionName, StringComparer.Ordinal);
        }
        catch (ArgumentException ex)
        {
            throw new InvalidOperationException("Duplicate structured option handlers registered for the same option name.", ex);
        }

        ValidateSemanticsAlignment(_byOption);
    }

    /// <inheritdoc />
    public bool IsStructured(string optionName) =>
        _byOption.ContainsKey(optionName);

    /// <inheritdoc />
    public IStructuredOptionValueHandler? Get(string optionName) =>
        _byOption.TryGetValue(optionName, out var handler) ? handler : null;

    /// <inheritdoc />
    public IStructuredOptionValueHandler<T>? Get<T>(string optionName) =>
        Get(optionName) as Abstractions.IStructuredOptionValueHandler<T>;

    /// <inheritdoc />
    public IStructuredOptionValueHandler<T> GetRequired<T>(string optionName)
    {
        var handler = Get<T>(optionName);
        if (handler != null)
            return handler;

        var structuredType = EffectiveConfigSpecialOptionSemantics.GetStructuredValueType(optionName);
        throw new InvalidOperationException(
            structuredType is null
                ? $"Option '{optionName}' is not declared as a structured option."
                : $"No structured handler registered for option '{optionName}' with value type '{typeof(T).Name}'. Declared type is '{structuredType.Name}'.");
    }

    private static void ValidateSemanticsAlignment(IReadOnlyDictionary<string, IStructuredOptionValueHandler> handlers)
    {
        foreach (var (optionName, handler) in handlers)
        {
            var structuredType = EffectiveConfigSpecialOptionSemantics.GetStructuredValueType(optionName);
            if (structuredType is null)
            {
                throw new InvalidOperationException(
                    $"Structured handler '{handler.GetType().Name}' is registered for '{optionName}', but semantics has no StructuredValueType.");
            }

            if (handler.ValueType != structuredType)
            {
                throw new InvalidOperationException(
                    $"Structured handler type mismatch for '{optionName}': handler type '{handler.ValueType.Name}' does not match semantics type '{structuredType.Name}'.");
            }
        }

        foreach (var optionName in EffectiveConfigSpecialOptionSemantics.GetAllOptionNames())
        {
            var structuredType = EffectiveConfigSpecialOptionSemantics.GetStructuredValueType(optionName);
            if (structuredType is null)
                continue;

            if (!handlers.ContainsKey(optionName))
            {
                throw new InvalidOperationException(
                    $"Semantics declares structured option '{optionName}' with type '{structuredType.Name}', but no structured handler is registered.");
            }
        }
    }

    private static HashSet<string> GetDnsmasqConfKeyValues()
    {
        var values = new HashSet<string>(StringComparer.Ordinal);
        foreach (var field in typeof(DnsmasqConfKeys).GetFields(BindingFlags.Public | BindingFlags.Static))
        {
            if (field.FieldType == typeof(string) && field.GetValue(null) is string value)
                values.Add(value);
        }
        return values;
    }
}
