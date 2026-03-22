using System.Text.Json.Serialization;
using FileFormula.Api.Infrastructure.Constraints;

namespace FileFormula.Api.Infrastructure.Models.Idempotency;

/// <summary>
/// Represents request idempotency settings for write operations.
/// </summary>
public class IdempotencyPolicy
{
    /// <summary>
    /// Gets the header name used to carry the idempotency key.
    /// </summary>
    [JsonPropertyName("headerName")]
    public string HeaderName { get; init; } = HEADER.IDEMPOTENCYKEY;

    /// <summary>
    /// Gets a value indicating whether the idempotency header is required for configured methods.
    /// </summary>
    [JsonPropertyName("requireHeader")]
    public bool RequireHeader { get; init; } = true;

    /// <summary>
    /// Gets the methods that participate in idempotency caching.
    /// </summary>
    [JsonPropertyName("replayableMethods")]
    public IReadOnlyList<string> ReplayableMethods { get; init; } = ["POST", "PUT", "PATCH"];

    /// <summary>
    /// Gets the cached response lifetime, in minutes.
    /// </summary>
    [JsonPropertyName("expirationMinutes")]
    public int ExpirationMinutes { get; init; } = 60;

    /// <summary>
    /// Gets the maximum response body size, in bytes, eligible for caching.
    /// </summary>
    [JsonPropertyName("maximumResponseBodyBytes")]
    public int MaximumResponseBodyBytes { get; init; } = 64 * 1024;

    /// <summary>
    /// Gets a value indicating whether successful responses are replayed only for identical request paths.
    /// </summary>
    [JsonPropertyName("includeQueryStringInKey")]
    public bool IncludeQueryStringInKey { get; init; } = false;
}