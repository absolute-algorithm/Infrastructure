using System.Text.Json.Serialization;

namespace AbsoluteAlgorithm.Infrastructure.Models.Resilience;

/// <summary>
/// Represents circuit-breaker settings for a resilience policy.
/// </summary>
public class CircuitBreakerResiliencePolicy
{
    /// <summary>
    /// Gets the number of handled failures required to break the circuit.
    /// </summary>
    [JsonPropertyName("handledEventsAllowedBeforeBreaking")]
    public int HandledEventsAllowedBeforeBreaking { get; init; } = 5;

    /// <summary>
    /// Gets the break duration, in seconds.
    /// </summary>
    [JsonPropertyName("durationOfBreakSeconds")]
    public int DurationOfBreakSeconds { get; init; } = 30;
}