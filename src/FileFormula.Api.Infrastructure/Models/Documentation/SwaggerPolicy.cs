using System.Text.Json.Serialization;
using FileFormula.Api.Infrastructure.Enums;

namespace FileFormula.Api.Infrastructure.Models.Documentation;

/// <summary>
/// Represents the Swagger and OpenAPI documentation configuration.
/// </summary>
public class SwaggerPolicy
{
    /// <summary>
    /// Gets the default document title.
    /// </summary>
    [JsonPropertyName("title")]
    public string Title { get; init; } = "API Documentation";

    /// <summary>
    /// Gets the optional default document description.
    /// </summary>
    [JsonPropertyName("description")]
    public string? Description { get; init; }

    /// <summary>
    /// Gets a value indicating whether Swagger UI is exposed.
    /// </summary>
    [JsonPropertyName("useSwaggerUi")]
    public bool UseSwaggerUi { get; init; } = true;

    /// <summary>
    /// Gets the OpenAPI JSON path.
    /// </summary>
    /// <remarks>
    /// Use the <c>{documentName}</c> placeholder when multiple documents are registered.
    /// </remarks>
    [JsonPropertyName("openApiPath")]
    public string OpenApiPath { get; init; } = "/swagger/{documentName}/swagger.json";

    /// <summary>
    /// Gets the Swagger UI path.
    /// </summary>
    [JsonPropertyName("swaggerUiPath")]
    public string SwaggerUiPath { get; init; } = "/swagger";

    /// <summary>
    /// Gets a value indicating whether authorizations entered in Swagger UI are persisted.
    /// </summary>
    [JsonPropertyName("persistAuthorization")]
    public bool PersistAuthorization { get; init; } = true;

    /// <summary>
    /// Gets a value indicating whether Swagger UI enables Try it out.
    /// </summary>
    [JsonPropertyName("enableTryItOut")]
    public bool EnableTryItOut { get; init; } = true;

    /// <summary>
    /// Gets the Swagger document generation mode.
    /// </summary>
    [JsonPropertyName("documentMode")]
    public SwaggerDocumentMode DocumentMode { get; init; } = SwaggerDocumentMode.Single;

    /// <summary>
    /// Gets the single-document name used when <see cref="DocumentMode"/> is <see cref="SwaggerDocumentMode.Single"/>.
    /// </summary>
    [JsonPropertyName("singleDocumentName")]
    public string SingleDocumentName { get; init; } = "v1";

    /// <summary>
    /// Gets the single-document version label used when <see cref="DocumentMode"/> is <see cref="SwaggerDocumentMode.Single"/>.
    /// </summary>
    [JsonPropertyName("singleDocumentVersion")]
    public string SingleDocumentVersion { get; init; } = "1.0";

    /// <summary>
    /// Gets the versioned document registrations used when <see cref="DocumentMode"/> is <see cref="SwaggerDocumentMode.PerApiVersion"/>.
    /// </summary>
    [JsonPropertyName("documents")]
    public IReadOnlyList<SwaggerDocumentDefinition> Documents { get; init; } = Array.Empty<SwaggerDocumentDefinition>();

    /// <summary>
    /// Gets the additional request headers to show in generated Swagger documents.
    /// </summary>
    [JsonPropertyName("headers")]
    public IReadOnlyList<SwaggerHeaderDefinition> Headers { get; init; } = Array.Empty<SwaggerHeaderDefinition>();
}