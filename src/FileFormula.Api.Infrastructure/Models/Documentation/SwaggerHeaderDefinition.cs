using System.Text.Json.Serialization;

namespace FileFormula.Api.Infrastructure.Models.Documentation;

/// <summary>
/// Describes a request header that should appear in generated Swagger documents.
/// </summary>
public class SwaggerHeaderDefinition
{
    /// <summary>
    /// Gets the header name.
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; init; } = string.Empty;

    /// <summary>
    /// Gets the header description.
    /// </summary>
    [JsonPropertyName("description")]
    public string? Description { get; init; }

    /// <summary>
    /// Gets a value indicating whether the header is required.
    /// </summary>
    [JsonPropertyName("required")]
    public bool Required { get; init; }

    /// <summary>
    /// Gets a value indicating whether the header should only be applied to authorized operations.
    /// </summary>
    [JsonPropertyName("authorizedOnly")]
    public bool AuthorizedOnly { get; init; }
}