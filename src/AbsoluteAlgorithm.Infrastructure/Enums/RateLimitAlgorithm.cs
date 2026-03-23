namespace AbsoluteAlgorithm.Infrastructure.Enums;

/// <summary>
/// Specifies the rate-limiting algorithm.
/// </summary>
public enum RateLimitAlgorithm : byte
{
    /// <summary>
    /// Uses a fixed-window rate limiter.
    /// </summary>
    FixedWindow = 1,

    /// <summary>
    /// Uses a sliding-window rate limiter.
    /// </summary>
    SlidingWindow,

    /// <summary>
    /// Uses a token-bucket rate limiter.
    /// </summary>
    TokenBucket,

    /// <summary>
    /// Uses a concurrency limiter.
    /// </summary>
    Concurrency
}