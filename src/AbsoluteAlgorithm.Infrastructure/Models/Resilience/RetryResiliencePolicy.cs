using System.Text.Json.Serialization;
using AbsoluteAlgorithm.Infrastructure.Enums;

namespace AbsoluteAlgorithm.Infrastructure.Models.Resilience;

/// <summary>
/// Represents retry settings for a resilience policy.
/// </summary>
public class RetryResiliencePolicy
{
    /// <summary>
    /// Gets the maximum retry attempts.
    /// </summary>
    [JsonPropertyName("maxRetryAttempts")]
    public int MaxRetryAttempts { get; init; } = 3;

    /// <summary>
    /// Gets the delay strategy used to calculate retry wait durations.
    /// </summary>
    /// <remarks>
    /// When <see langword="null"/>, the legacy <see cref="UseExponentialBackoff"/> flag is used for backward compatibility.
    /// </remarks>
    [JsonPropertyName("delayStrategy")]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public RetryDelayStrategy? DelayStrategy { get; init; }

    /// <summary>
    /// Gets the base delay in milliseconds between retries.
    /// </summary>
    [JsonPropertyName("delayMilliseconds")]
    public int DelayMilliseconds { get; init; } = 200;

    /// <summary>
    /// Gets the per-attempt increment in milliseconds used when <see cref="DelayStrategy"/> is <see cref="RetryDelayStrategy.Linear"/>.
    /// </summary>
    [JsonPropertyName("delayIncrementMilliseconds")]
    public int DelayIncrementMilliseconds { get; init; } = 200;

    /// <summary>
    /// Gets the multiplier applied to subsequent attempts when <see cref="DelayStrategy"/> is <see cref="RetryDelayStrategy.Exponential"/>.
    /// </summary>
    [JsonPropertyName("backoffMultiplier")]
    public double BackoffMultiplier { get; init; } = 2d;

    /// <summary>
    /// Gets the explicit per-attempt delay schedule, in milliseconds.
    /// </summary>
    /// <remarks>
    /// When supplied, this schedule takes precedence and can represent exact timings such as 2000, 3000, and 4000 milliseconds.
    /// If the retry count exceeds the schedule length, the final configured delay is reused for the remaining attempts.
    /// </remarks>
    [JsonPropertyName("delayScheduleMilliseconds")]
    public List<int>? DelayScheduleMilliseconds { get; init; }

    /// <summary>
    /// Gets a value indicating whether exponential backoff is used.
    /// </summary>
    /// <remarks>
    /// This property is retained for backward compatibility. Prefer <see cref="DelayStrategy"/> for new configurations.
    /// </remarks>
    [JsonPropertyName("useExponentialBackoff")]
    public bool UseExponentialBackoff { get; init; } = true;
}