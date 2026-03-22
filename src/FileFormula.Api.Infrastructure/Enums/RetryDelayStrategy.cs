namespace FileFormula.Api.Infrastructure.Enums;

/// <summary>
/// Defines how retry delays are calculated between attempts.
/// </summary>
public enum RetryDelayStrategy
{
    /// <summary>
    /// Uses the same delay for every retry attempt.
    /// </summary>
    Fixed,

    /// <summary>
    /// Increases the delay by a constant step on each retry attempt.
    /// </summary>
    Linear,

    /// <summary>
    /// Multiplies the delay on each retry attempt.
    /// </summary>
    Exponential,

    /// <summary>
    /// Uses explicit delay values supplied in the retry schedule.
    /// </summary>
    CustomSchedule
}