namespace FileFormula.Api.Infrastructure.Enums;

/// <summary>
/// Specifies the supported resilience categories exposed by the library.
/// </summary>
public enum ResilienceType : byte
{
    /// <summary>
    /// Retry handling for transient failures.
    /// </summary>
    Retry = 1,

    /// <summary>
    /// Timeout handling for long-running operations.
    /// </summary>
    Timeout,

    /// <summary>
    /// Circuit-breaker handling for repeated failures.
    /// </summary>
    CircuitBreaker
}