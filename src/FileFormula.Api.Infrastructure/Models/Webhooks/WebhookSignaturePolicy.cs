using System.Text.Json.Serialization;
using FileFormula.Api.Infrastructure.Constraints;
using FileFormula.Api.Infrastructure.Enums;

namespace FileFormula.Api.Infrastructure.Models.Webhooks;

/// <summary>
/// Represents signature validation settings for inbound webhook requests.
/// </summary>
public class WebhookSignaturePolicy
{
    /// <summary>
    /// Gets the logical policy name.
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; init; } = string.Empty;

    /// <summary>
    /// Gets the request path prefix to validate.
    /// </summary>
    [JsonPropertyName("pathPrefix")]
    public string PathPrefix { get; init; } = "/webhooks";

    /// <summary>
    /// Gets the environment variable that contains the shared secret.
    /// </summary>
    [JsonPropertyName("secretName")]
    public string SecretName { get; init; } = string.Empty;

    /// <summary>
    /// Gets the signature header name.
    /// </summary>
    [JsonPropertyName("signatureHeaderName")]
    public string SignatureHeaderName { get; init; } = HEADER.REQUESTSIGNATURE;

    /// <summary>
    /// Gets the timestamp header name.
    /// </summary>
    [JsonPropertyName("timestampHeaderName")]
    public string TimestampHeaderName { get; init; } = HEADER.REQUESTSIGNATURETIMESTAMP;

    /// <summary>
    /// Gets the signature algorithm.
    /// </summary>
    [JsonPropertyName("algorithm")]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public RequestSignatureAlgorithm Algorithm { get; init; } = RequestSignatureAlgorithm.HmacSha256;

    /// <summary>
    /// Gets the allowed timestamp skew, in seconds.
    /// </summary>
    [JsonPropertyName("allowedClockSkewSeconds")]
    public int AllowedClockSkewSeconds { get; init; } = 300;
}