using System.Text.Json.Serialization;

namespace FileFormula.Api.Infrastructure.Models.Resilience;

/// <summary>
/// Represents the resilience configuration applied to a database or HTTP client registration.
/// </summary>
public class ResiliencePolicy
{
    /// <summary>
    /// Gets the retry configuration.
    /// </summary>
    [JsonPropertyName("retry")]
    public RetryResiliencePolicy? Retry { get; init; }

    /// <summary>
    /// Gets the timeout configuration.
    /// </summary>
    [JsonPropertyName("timeout")]
    public TimeoutResiliencePolicy? Timeout { get; init; }

    /// <summary>
    /// Gets the circuit-breaker configuration.
    /// </summary>
    [JsonPropertyName("circuitBreaker")]
    public CircuitBreakerResiliencePolicy? CircuitBreaker { get; init; }
}