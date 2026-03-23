namespace AbsoluteAlgorithm.Infrastructure.Enums;

/// <summary>
/// Specifies the partitioning scope for a rate-limit policy.
/// </summary>
public enum RateLimitScope : byte
{
    /// <summary>
    /// Applies a shared limit to all requests.
    /// </summary>
    Global = 0,

    /// <summary>
    /// Partitions limits by client IP address.
    /// </summary>
    IpAddress,

    /// <summary>
    /// Partitions limits by authenticated user.
    /// </summary>
    User,

    /// <summary>
    /// Partitions limits by request path.
    /// </summary>
    Endpoint,

    /// <summary>
    /// Partitions limits by API key.
    /// </summary>
    ApiKey
}