using System.Text.Json;
using System.Text.Json.Serialization;

namespace DnsmasqWebUI.Infrastructure.Helpers.Http;

/// <summary>
/// Shared JSON options for API serialization (Controllers) and client deserialization (HTTP clients).
/// Keeps camelCase and enum-as-string in one place so server and client stay in sync.
/// </summary>
public static class ApiJsonOptions
{
    /// <summary>Options for Controllers (AddJsonOptions). Apply via ConfigureServer(JsonSerializerOptions).</summary>
    public static void ConfigureServer(JsonSerializerOptions options)
    {
        options.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        options.Converters.Add(new JsonStringEnumConverter());
    }

    /// <summary>Options for HTTP clients when deserializing API responses (camelCase, case-insensitive, enum as string).</summary>
    public static JsonSerializerOptions ClientOptions { get; } = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };
}
