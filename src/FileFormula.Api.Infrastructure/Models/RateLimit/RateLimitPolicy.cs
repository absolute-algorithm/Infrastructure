using System.Text.Json.Serialization;
using FileFormula.Api.Infrastructure.Enums;

namespace FileFormula.Api.Infrastructure.Models.RateLimit;


/// <summary>
/// Represents the configuration for a named rate-limit policy.
/// </summary>
public class RateLimitPolicy
{
    /// <summary>
    /// Gets the name of the policy.
    /// </summary>
    [JsonPropertyName("policyName")]
    public string PolicyName { get; init; } = null!;

    /// <summary>
    /// Gets the rate-limiting algorithm.
    /// </summary>
    [JsonPropertyName("algorithm")]
    public RateLimitAlgorithm Algorithm { get; init; }

    /// <summary>
    /// Gets the partitioning scope for the policy.
    /// </summary>
    [JsonPropertyName("scope")]
    public RateLimitScope Scope { get; init; }

    /// <summary>
    /// Gets the maximum number of permits allowed by applicable limiter types.
    /// </summary>
    [JsonPropertyName("permitLimit")]
    public int PermitLimit { get; init; }

    /// <summary>
    /// Gets the time window associated with the policy.
    /// </summary>
    [JsonPropertyName("window")]
    public TimeSpan Window { get; init; }

    /// <summary>
    /// Gets the number of segments used by the sliding-window algorithm.
    /// </summary>
    [JsonPropertyName("segmentsPerWindow")]
    public int SegmentsPerWindow { get; init; } = 1;

    /// <summary>
    /// Gets the maximum number of tokens that can accumulate in a token-bucket policy.
    /// </summary>
    [JsonPropertyName("tokenLimit")]
    public int TokenLimit { get; init; }

    /// <summary>
    /// Gets the number of tokens replenished during each replenishment period.
    /// </summary>
    [JsonPropertyName("tokensPerPeriod")]
    public int TokensPerPeriod { get; init; }
}
