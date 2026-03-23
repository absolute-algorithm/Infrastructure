using System.Text.Json.Serialization;

namespace AbsoluteAlgorithm.Infrastructure.Models.Resilience;

/// <summary>
/// Represents timeout settings for a resilience policy.
/// </summary>
public class TimeoutResiliencePolicy
{
    /// <summary>
    /// Gets the timeout value, in seconds.
    /// </summary>
    [JsonPropertyName("timeoutSeconds")]
    public int TimeoutSeconds { get; init; } = 30;
}