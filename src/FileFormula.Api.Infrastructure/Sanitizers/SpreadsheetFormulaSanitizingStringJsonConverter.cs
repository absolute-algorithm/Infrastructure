using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace FileFormula.Api.Infrastructure.Sanitizers;

/// <summary>
/// Sanitizes JSON string values during controller request-body deserialization.
/// </summary>
public sealed class SpreadsheetFormulaSanitizingStringJsonConverter : JsonConverter<string>
{
    /// <summary>
    /// Reads and sanitizes a JSON string value.
    /// </summary>
    /// <param name="reader">The JSON reader.</param>
    /// <param name="typeToConvert">The target type.</param>
    /// <param name="options">The serializer options.</param>
    /// <returns>The sanitized string value.</returns>
    public override string? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
        {
            return null;
        }

        return SpreadsheetFormulaSanitizer.SanitizeCell(reader.GetString());
    }

    /// <summary>
    /// Writes a JSON string value.
    /// </summary>
    /// <param name="writer">The JSON writer.</param>
    /// <param name="value">The value to write.</param>
    /// <param name="options">The serializer options.</param>
    public override void Write(Utf8JsonWriter writer, string value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value);
    }
}