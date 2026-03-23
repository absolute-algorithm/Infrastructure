using System.Text.Json;
using System.Text.Json.Serialization;

namespace AbsoluteAlgorithm.Infrastructure.Utilities;

/// <summary>
/// Provides helper methods for JSON serialization and deserialization.
/// </summary>
public static class JsonUtility
{
    /// <summary>
    /// Creates the default JSON serializer options used by the library helpers.
    /// </summary>
    /// <returns>A new <see cref="JsonSerializerOptions"/> instance.</returns>
    public static JsonSerializerOptions CreateDefaultOptions()
    {
        return new JsonSerializerOptions(JsonSerializerDefaults.Web)
        {
            PropertyNameCaseInsensitive = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            WriteIndented = false
        };
    }

    /// <summary>
    /// Serializes a value to JSON.
    /// </summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="value">The value to serialize.</param>
    /// <param name="options">The serializer options. When <see langword="null"/>, the library defaults are used.</param>
    /// <returns>The serialized JSON string.</returns>
    public static string Serialize<T>(T value, JsonSerializerOptions? options = null)
    {
        return JsonSerializer.Serialize(value, options ?? CreateDefaultOptions());
    }

    /// <summary>
    /// Deserializes JSON into the specified type.
    /// </summary>
    /// <typeparam name="T">The target type.</typeparam>
    /// <param name="json">The JSON payload.</param>
    /// <param name="options">The serializer options. When <see langword="null"/>, the library defaults are used.</param>
    /// <returns>The deserialized value.</returns>
    public static T? Deserialize<T>(string json, JsonSerializerOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(json);

        return JsonSerializer.Deserialize<T>(json, options ?? CreateDefaultOptions());
    }

    /// <summary>
    /// Deserializes JSON into the specified runtime type.
    /// </summary>
    /// <param name="json">The JSON payload.</param>
    /// <param name="type">The target runtime type.</param>
    /// <param name="options">The serializer options. When <see langword="null"/>, the library defaults are used.</param>
    /// <returns>The deserialized value.</returns>
    public static object? Deserialize(string json, Type type, JsonSerializerOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(json);
        ArgumentNullException.ThrowIfNull(type);

        return JsonSerializer.Deserialize(json, type, options ?? CreateDefaultOptions());
    }
}