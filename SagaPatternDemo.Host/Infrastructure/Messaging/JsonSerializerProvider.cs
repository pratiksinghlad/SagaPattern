using System.Text.Json;
using System.Text.Json.Serialization;

namespace SagaPatternDemo.Host.Infrastructure.Messaging;

/// <summary>
/// Provides consistent JSON serialization options across the application
/// </summary>
public interface IJsonSerializerProvider
{
    /// <summary>
    /// Gets the configured JSON serializer options
    /// </summary>
    JsonSerializerOptions Options { get; }

    /// <summary>
    /// Serializes an object to JSON string
    /// </summary>
    /// <typeparam name="T">The type to serialize</typeparam>
    /// <param name="value">The value to serialize</param>
    /// <returns>JSON string representation</returns>
    string Serialize<T>(T value);

    /// <summary>
    /// Deserializes a JSON string to the specified type
    /// </summary>
    /// <typeparam name="T">The type to deserialize to</typeparam>
    /// <param name="json">The JSON string</param>
    /// <returns>Deserialized object</returns>
    T? Deserialize<T>(string json);
}

/// <summary>
/// Default implementation of JSON serializer provider
/// </summary>
public class JsonSerializerProvider : IJsonSerializerProvider
{
    private readonly Lazy<JsonSerializerOptions> _options;

    public JsonSerializerProvider()
    {
        _options = new Lazy<JsonSerializerOptions>(CreateOptions);
    }

    public JsonSerializerOptions Options => _options.Value;

    public string Serialize<T>(T value)
    {
        return JsonSerializer.Serialize(value, Options);
    }

    public T? Deserialize<T>(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return default;

        return JsonSerializer.Deserialize<T>(json, Options);
    }

    private static JsonSerializerOptions CreateOptions()
    {
        return new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            Converters =
            {
                new JsonStringEnumConverter(JsonNamingPolicy.CamelCase)
            }
        };
    }
}