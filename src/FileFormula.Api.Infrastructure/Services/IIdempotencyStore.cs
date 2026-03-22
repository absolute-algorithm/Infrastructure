namespace FileFormula.Api.Infrastructure.Services;

/// <summary>
/// Stores completed and in-flight idempotent request responses.
/// </summary>
public interface IIdempotencyStore
{
    /// <summary>
    /// Looks up the state for the specified idempotency key.
    /// </summary>
    /// <param name="key">The normalized idempotency key.</param>
    /// <returns>The lookup result.</returns>
    IdempotencyLookupResult Get(string key);

    /// <summary>
    /// Attempts to reserve the specified key while a request is in flight.
    /// </summary>
    /// <param name="key">The normalized idempotency key.</param>
    /// <returns><see langword="true"/> when the key was reserved; otherwise, <see langword="false"/>.</returns>
    bool TryBegin(string key);

    /// <summary>
    /// Stores a completed idempotent response.
    /// </summary>
    /// <param name="key">The normalized idempotency key.</param>
    /// <param name="response">The cached response.</param>
    /// <param name="expiration">The cache lifetime.</param>
    void Complete(string key, IdempotencyStoredResponse response, TimeSpan expiration);

    /// <summary>
    /// Removes the in-flight reservation without storing a cached response.
    /// </summary>
    /// <param name="key">The normalized idempotency key.</param>
    void Abandon(string key);
}

/// <summary>
/// Represents the outcome of an idempotency lookup.
/// </summary>
public class IdempotencyLookupResult
{
    /// <summary>
    /// Gets a value indicating whether a completed response exists.
    /// </summary>
    public bool HasStoredResponse { get; init; }

    /// <summary>
    /// Gets a value indicating whether a matching request is currently in flight.
    /// </summary>
    public bool IsInProgress { get; init; }

    /// <summary>
    /// Gets the stored response when one is available.
    /// </summary>
    public IdempotencyStoredResponse? Response { get; init; }
}

/// <summary>
/// Represents a cached idempotent HTTP response.
/// </summary>
public class IdempotencyStoredResponse
{
    /// <summary>
    /// Gets the HTTP status code.
    /// </summary>
    public int StatusCode { get; init; }

    /// <summary>
    /// Gets the response content type.
    /// </summary>
    public string? ContentType { get; init; }

    /// <summary>
    /// Gets the serialized response body.
    /// </summary>
    public byte[] Body { get; init; } = Array.Empty<byte>();
}