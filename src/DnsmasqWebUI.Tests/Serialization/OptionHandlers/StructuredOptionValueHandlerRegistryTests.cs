using System.Reflection;
using DnsmasqWebUI.Infrastructure.Serialization.OptionHandlers;
using DnsmasqWebUI.Infrastructure.Serialization.OptionHandlers.Abstractions;
using DnsmasqWebUI.Infrastructure.Services.EffectiveConfig.Metadata;
using DnsmasqWebUI.Models.Dhcp;

namespace DnsmasqWebUI.Tests.Serialization.OptionHandlers;

/// <summary>Ensures structured handlers use DnsmasqConfKeys and registry has no duplicate option names.</summary>
public class StructuredOptionValueHandlerRegistryTests
{
    [Fact]
    public void DhcpHostOptionValueHandler_OptionName_EqualsDnsmasqConfKeysDhcpHost()
    {
        var handler = new DhcpHostOptionValueHandler();
        Assert.Equal(DnsmasqConfKeys.DhcpHost, handler.OptionName);
    }

    [Fact]
    public void DhcpHostOptionValueHandler_OptionName_IsFromDnsmasqConfKeys()
    {
        var handler = new DhcpHostOptionValueHandler();
        var validKeys = GetDnsmasqConfKeysValues();
        Assert.Contains(handler.OptionName, validKeys);
    }

    [Fact]
    public void Registry_WithDhcpHostHandler_ReturnsHandlerForDhcpHost()
    {
        var handler = new DhcpHostOptionValueHandler();
        var registry = new StructuredOptionValueHandlerRegistry(new[] { handler });
        var resolved = registry.Get<DhcpHostEntry>(DnsmasqConfKeys.DhcpHost);
        Assert.NotNull(resolved);
        Assert.Equal(DnsmasqConfKeys.DhcpHost, resolved.OptionName);
    }

    [Fact]
    public void Registry_IsStructured_WhenHandlerExists_ReturnsTrue()
    {
        var registry = new StructuredOptionValueHandlerRegistry([new DhcpHostOptionValueHandler()]);
        Assert.True(registry.IsStructured(DnsmasqConfKeys.DhcpHost));
        Assert.False(registry.IsStructured(DnsmasqConfKeys.Server));
    }

    [Fact]
    public void Registry_GetRequired_WhenHandlerExists_ReturnsTypedHandler()
    {
        var registry = new StructuredOptionValueHandlerRegistry([new DhcpHostOptionValueHandler()]);
        var handler = registry.GetRequired<DhcpHostEntry>(DnsmasqConfKeys.DhcpHost);
        Assert.Equal(DnsmasqConfKeys.DhcpHost, handler.OptionName);
    }

    [Fact]
    public void Registry_GetRequired_WhenOptionIsNotStructured_Throws()
    {
        var registry = new StructuredOptionValueHandlerRegistry([new DhcpHostOptionValueHandler()]);
        var ex = Assert.Throws<InvalidOperationException>(() => registry.GetRequired<DhcpHostEntry>(DnsmasqConfKeys.Server));
        Assert.Contains("not declared as a structured option", ex.Message);
    }

    [Fact]
    public void Registry_DuplicateOptionNames_Throws()
    {
        var ex = Assert.Throws<InvalidOperationException>(() =>
            new StructuredOptionValueHandlerRegistry([
                new DhcpHostOptionValueHandler(),
                new DuplicateDhcpHostHandler()
            ]));
        Assert.Contains("Duplicate structured option handlers", ex.Message);
    }

    [Fact]
    public void Semantics_DhcpHost_HasStructuredValueTypeMatchingHandler()
    {
        var structuredType = EffectiveConfigSpecialOptionSemantics.GetStructuredValueType(DnsmasqConfKeys.DhcpHost);
        Assert.NotNull(structuredType);
        Assert.Equal(typeof(DhcpHostEntry), structuredType);
        var handler = new DhcpHostOptionValueHandler();
        Assert.Equal(structuredType, handler.ValueType);
    }

    [Fact]
    public void Registry_HandlerWithoutStructuredMetadata_Throws()
    {
        var ex = Assert.Throws<InvalidOperationException>(() =>
            new StructuredOptionValueHandlerRegistry([new InvalidServerHandler()]));
        Assert.Contains("no StructuredValueType", ex.Message);
    }

    private static HashSet<string> GetDnsmasqConfKeysValues()
    {
        var set = new HashSet<string>(StringComparer.Ordinal);
        foreach (var field in typeof(DnsmasqConfKeys).GetFields(BindingFlags.Public | BindingFlags.Static))
        {
            if (field.FieldType == typeof(string) && field.GetValue(null) is string value)
                set.Add(value);
        }
        return set;
    }

    private sealed class DuplicateDhcpHostHandler : IStructuredOptionValueHandler<DhcpHostEntry>
    {
        public string OptionName => DnsmasqConfKeys.DhcpHost;
        public Type ValueType => typeof(DhcpHostEntry);
        public string SerializeLine(DhcpHostEntry value) => "";
        public string SerializeValue(DhcpHostEntry value) => "";
        public bool TryParseValue(string text, int lineNumber, out DhcpHostEntry? value)
        {
            value = null;
            return false;
        }
    }

    private sealed class InvalidServerHandler : IStructuredOptionValueHandler<string>
    {
        public string OptionName => DnsmasqConfKeys.Server;
        public Type ValueType => typeof(string);
        public string SerializeLine(string value) => "";
        public string SerializeValue(string value) => value;
        public bool TryParseValue(string text, int lineNumber, out string? value)
        {
            value = text;
            return true;
        }
    }
}
