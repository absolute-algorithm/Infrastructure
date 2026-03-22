using System.Text.Json.Serialization;

namespace FileFormula.Api.Infrastructure.Models.Documentation;

/// <summary>
/// Describes a single Swagger document registration.
/// </summary>
public class SwaggerDocumentDefinition
{
    /// <summary>
    /// Gets the unique Swagger document name.
    /// </summary>
    [JsonPropertyName("documentName")]
    public string DocumentName { get; init; } = string.Empty;

    /// <summary>
    /// Gets the API Explorer group name that should be included in the document.
    /// </summary>
    [JsonPropertyName("apiGroupName")]
    public string ApiGroupName { get; init; } = string.Empty;

    /// <summary>
    /// Gets the displayed API version.
    /// </summary>
    [JsonPropertyName("version")]
    public string Version { get; init; } = string.Empty;

    /// <summary>
    /// Gets the optional document title override.
    /// </summary>
    [JsonPropertyName("title")]
    public string? Title { get; init; }

    /// <summary>
    /// Gets the optional document description override.
    /// </summary>
    [JsonPropertyName("description")]
    public string? Description { get; init; }
}