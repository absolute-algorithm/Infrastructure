using System.Text.Json.Serialization;
using AbsoluteAlgorithm.Infrastructure.Enums;

namespace AbsoluteAlgorithm.Infrastructure.Models.Configuration;

/// <summary>
/// Represents API versioning options applied during service registration.
/// </summary>
public class ApiVersioningPolicy
{
    /// <summary>
    /// Gets the default API major version.
    /// </summary>
    [JsonPropertyName("defaultMajorVersion")]
    public int DefaultMajorVersion { get; init; } = 1;

    /// <summary>
    /// Gets the default API minor version.
    /// </summary>
    [JsonPropertyName("defaultMinorVersion")]
    public int DefaultMinorVersion { get; init; } = 0;

    /// <summary>
    /// Gets a value indicating whether requests that omit a version should use the configured default version.
    /// </summary>
    [JsonPropertyName("assumeDefaultVersionWhenUnspecified")]
    public bool AssumeDefaultVersionWhenUnspecified { get; init; } = true;

    /// <summary>
    /// Gets a value indicating whether supported and deprecated API versions are reported in response headers.
    /// </summary>
    [JsonPropertyName("reportApiVersions")]
    public bool ReportApiVersions { get; init; } = true;

    /// <summary>
    /// Gets the enabled request readers used to resolve the requested API version.
    /// </summary>
    [JsonPropertyName("readers")]
    public IReadOnlyList<ApiVersionReaderType> Readers { get; init; } =
    [
        ApiVersionReaderType.QueryString
    ];

    /// <summary>
    /// Gets the query-string parameter name used when <see cref="Readers"/> includes <see cref="ApiVersionReaderType.QueryString"/>.
    /// </summary>
    [JsonPropertyName("queryStringParameterName")]
    public string QueryStringParameterName { get; init; } = "api-version";

    /// <summary>
    /// Gets the request header names used when <see cref="Readers"/> includes <see cref="ApiVersionReaderType.Header"/>.
    /// </summary>
    [JsonPropertyName("headerNames")]
    public IReadOnlyList<string> HeaderNames { get; init; } = ["x-api-version"];

    /// <summary>
    /// Gets the media-type parameter name used when <see cref="Readers"/> includes <see cref="ApiVersionReaderType.MediaType"/>.
    /// </summary>
    [JsonPropertyName("mediaTypeParameterName")]
    public string MediaTypeParameterName { get; init; } = "ver";

    /// <summary>
    /// Gets a value indicating whether versioned API explorer metadata should be registered.
    /// </summary>
    [JsonPropertyName("enableApiExplorer")]
    public bool EnableApiExplorer { get; init; } = true;

    /// <summary>
    /// Gets the group-name format used by the versioned API explorer.
    /// </summary>
    [JsonPropertyName("groupNameFormat")]
    public string GroupNameFormat { get; init; } = "'v'VVV";

    /// <summary>
    /// Gets a value indicating whether route templates should substitute the concrete API version value when URL segment versioning is used.
    /// </summary>
    [JsonPropertyName("substituteApiVersionInUrl")]
    public bool SubstituteApiVersionInUrl { get; init; } = true;
}