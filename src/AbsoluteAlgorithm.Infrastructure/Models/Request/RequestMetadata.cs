namespace AbsoluteAlgorithm.Infrastructure.Models.Request;

/// <summary>
/// Represents normalized metadata for the current HTTP request.
/// </summary>
public class RequestMetadata
{
    /// <summary>
    /// Gets the correlation identifier.
    /// </summary>
    public string? CorrelationId { get; init; }

    /// <summary>
    /// Gets the authenticated user identifier.
    /// </summary>
    public string? UserId { get; init; }

    /// <summary>
    /// Gets the tenant identifier.
    /// </summary>
    public string? TenantId { get; init; }

    /// <summary>
    /// Gets the idempotency key.
    /// </summary>
    public string? IdempotencyKey { get; init; }

    /// <summary>
    /// Gets the client IP address.
    /// </summary>
    public string? ClientIpAddress { get; init; }

    /// <summary>
    /// Gets the request user-agent value.
    /// </summary>
    public string? UserAgent { get; init; }

    /// <summary>
    /// Gets the request path.
    /// </summary>
    public string? Path { get; init; }

    /// <summary>
    /// Gets the request method.
    /// </summary>
    public string? Method { get; init; }

    /// <summary>
    /// Gets a value indicating whether the caller is authenticated.
    /// </summary>
    public bool IsAuthenticated { get; init; }
}