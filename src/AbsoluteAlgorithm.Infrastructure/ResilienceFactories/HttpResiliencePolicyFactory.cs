using Polly;
using Polly.CircuitBreaker;
using Polly.Retry;
using Polly.Timeout;
using AbsoluteAlgorithm.Core.Enums;
using AbsoluteAlgorithm.Core.Models.Resilience;

namespace AbsoluteAlgorithm.Infrastructure.ResilienceFactories;

/// <summary>
/// Creates Polly policies for outbound HTTP requests.
/// </summary>
public static class HttpResiliencePolicyFactory
{
    /// <summary>
    /// Creates an asynchronous HTTP resilience policy for the supplied configuration.
    /// </summary>
    /// <param name="policy">The resilience policy configuration.</param>
    /// <returns>An asynchronous HTTP response policy.</returns>
    public static IAsyncPolicy<HttpResponseMessage> CreateHttpPolicy(ResiliencePolicy? policy)
    {
        if (policy is null)
        {
            return Policy.NoOpAsync<HttpResponseMessage>();
        }

        var policyWrap = Policy.WrapAsync<HttpResponseMessage>(
            CreateTimeoutPolicy(policy.Timeout),
            CreateCircuitBreakerPolicy(policy.CircuitBreaker),
            CreateRetryPolicy(policy.Retry));

        return policyWrap;
    }

    private static AsyncRetryPolicy<HttpResponseMessage> CreateRetryPolicy(RetryResiliencePolicy? retry)
    {
        if (retry is null || retry.MaxRetryAttempts <= 0)
        {
            return Policy.HandleResult<HttpResponseMessage>(_ => false).RetryAsync(0);
        }

        return Policy<HttpResponseMessage>
            .Handle<HttpRequestException>()
            .Or<TimeoutRejectedException>()
            .Or<TaskCanceledException>()
            .OrResult(ShouldHandleResult)
            .WaitAndRetryAsync(
                retry.MaxRetryAttempts,
                retryAttempt => CreateDelay(retry, retryAttempt));
    }

    private static AsyncCircuitBreakerPolicy<HttpResponseMessage> CreateCircuitBreakerPolicy(CircuitBreakerResiliencePolicy? circuitBreaker)
    {
        if (circuitBreaker is null || circuitBreaker.HandledEventsAllowedBeforeBreaking <= 0)
        {
            return Policy<HttpResponseMessage>
                .HandleResult(_ => false)
                .CircuitBreakerAsync(100_000, TimeSpan.FromMilliseconds(1));
        }

        return Policy<HttpResponseMessage>
            .Handle<HttpRequestException>()
            .Or<TimeoutRejectedException>()
            .Or<TaskCanceledException>()
            .OrResult(ShouldHandleResult)
            .CircuitBreakerAsync(
                circuitBreaker.HandledEventsAllowedBeforeBreaking,
                TimeSpan.FromSeconds(Math.Max(1, circuitBreaker.DurationOfBreakSeconds)));
    }

    private static IAsyncPolicy<HttpResponseMessage> CreateTimeoutPolicy(TimeoutResiliencePolicy? timeout)
    {
        if (timeout is null || timeout.TimeoutSeconds <= 0)
        {
            return Policy.NoOpAsync<HttpResponseMessage>();
        }

        return Policy.TimeoutAsync<HttpResponseMessage>(TimeSpan.FromSeconds(timeout.TimeoutSeconds));
    }

    private static bool ShouldHandleResult(HttpResponseMessage response)
    {
        var statusCode = (int)response.StatusCode;
        return statusCode == 408 || statusCode == 429 || statusCode >= 500;
    }

    private static TimeSpan CreateDelay(RetryResiliencePolicy retry, int retryAttempt)
    {
        var schedule = retry.DelayScheduleMilliseconds;
        if (schedule is { Count: > 0 })
        {
            var scheduleIndex = Math.Min(Math.Max(0, retryAttempt - 1), schedule.Count - 1);
            return TimeSpan.FromMilliseconds(Math.Max(1, schedule[scheduleIndex]));
        }

        var strategy = retry.DelayStrategy ?? (retry.UseExponentialBackoff ? RetryDelayStrategy.Exponential : RetryDelayStrategy.Fixed);
        var baseDelay = Math.Max(1, retry.DelayMilliseconds);

        return strategy switch
        {
            RetryDelayStrategy.Fixed => TimeSpan.FromMilliseconds(baseDelay),
            RetryDelayStrategy.Linear => TimeSpan.FromMilliseconds(baseDelay + (Math.Max(1, retryAttempt) - 1) * Math.Max(1, retry.DelayIncrementMilliseconds)),
            RetryDelayStrategy.Exponential => TimeSpan.FromMilliseconds(baseDelay * Math.Pow(retry.BackoffMultiplier > 1d ? retry.BackoffMultiplier : 2d, retryAttempt - 1)),
            RetryDelayStrategy.CustomSchedule => TimeSpan.FromMilliseconds(baseDelay),
            _ => TimeSpan.FromMilliseconds(baseDelay)
        };
    }
}