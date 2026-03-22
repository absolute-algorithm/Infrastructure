using System;
using System.Data.Common;
using FileFormula.Api.Infrastructure.Enums;
using FileFormula.Api.Infrastructure.Models.Resilience;
using Microsoft.Data.SqlClient;
using Npgsql;
using Polly;
using Polly.CircuitBreaker;
using Polly.Retry;
using Polly.Timeout;

namespace FileFormula.Api.Infrastructure.ResilienceFactories;

/// <summary>
/// Creates retry policies for transient database failures.
/// </summary>
public static class DBResiliencePolicyFactory
{
    /// <summary>
    /// Creates an asynchronous resilience policy for the specified database provider and configuration.
    /// </summary>
    /// <param name="provider">The database provider to tailor the transient fault detection for.</param>
    /// <param name="policy">The resilience policy configuration.</param>
    /// <returns>An asynchronous resilience policy.</returns>
    public static IAsyncPolicy CreateDbPolicy(DatabaseProvider provider, ResiliencePolicy? policy)
    {
        if (policy is null)
        {
            return Policy.NoOpAsync();
        }

        return Policy.WrapAsync(
            CreateTimeoutPolicy(policy.Timeout),
            CreateCircuitBreakerPolicy(provider, policy.CircuitBreaker),
            CreateRetryPolicy(provider, policy.Retry));
    }

    /// <summary>
    /// Creates an asynchronous retry policy for the specified database provider.
    /// </summary>
    /// <param name="provider">The database provider to tailor the transient fault detection for.</param>
    /// <param name="retryCount">The number of retry attempts to perform.</param>
    /// <returns>An asynchronous retry policy.</returns>
    public static AsyncRetryPolicy CreateDbRetryPolicy(DatabaseProvider provider, int retryCount = 3)
    {
        return CreateRetryPolicy(provider, new RetryResiliencePolicy
        {
            MaxRetryAttempts = retryCount,
            DelayMilliseconds = 1000,
            DelayStrategy = RetryDelayStrategy.Exponential,
            BackoffMultiplier = 2d,
            UseExponentialBackoff = true
        });
    }

    private static AsyncRetryPolicy CreateRetryPolicy(DatabaseProvider provider, RetryResiliencePolicy? retry)
    {
        if (retry is null || retry.MaxRetryAttempts <= 0)
        {
            return Policy.Handle<Exception>(_ => false).RetryAsync(0);
        }

        var builder = Policy
            .Handle<DbException>()
            .Or<TimeoutException>()
            .Or<TimeoutRejectedException>();

        return provider switch
        {
            DatabaseProvider.MSSQL => builder
                .OrInner<SqlException>(ex => ex.Number == 1205)
                .WaitAndRetryAsync(retry.MaxRetryAttempts, retryAttempt => CreateDelay(retry, retryAttempt)),

            DatabaseProvider.PostgreSQL => builder
                .OrInner<NpgsqlException>(ex => ex.IsTransient)
                .WaitAndRetryAsync(retry.MaxRetryAttempts, retryAttempt => CreateDelay(retry, retryAttempt)),

            _ => builder.WaitAndRetryAsync(retry.MaxRetryAttempts, retryAttempt => CreateDelay(retry, retryAttempt))
        };
    }

    private static AsyncCircuitBreakerPolicy CreateCircuitBreakerPolicy(DatabaseProvider provider, CircuitBreakerResiliencePolicy? circuitBreaker)
    {
        if (circuitBreaker is null || circuitBreaker.HandledEventsAllowedBeforeBreaking <= 0)
        {
            return Policy.Handle<Exception>(_ => false).CircuitBreakerAsync(100_000, TimeSpan.FromMilliseconds(1));
        }

        var builder = Policy
            .Handle<DbException>()
            .Or<TimeoutException>()
            .Or<TimeoutRejectedException>();

        return provider switch
        {
            DatabaseProvider.MSSQL => builder
                .OrInner<SqlException>(ex => ex.Number == 1205)
                .CircuitBreakerAsync(circuitBreaker.HandledEventsAllowedBeforeBreaking, TimeSpan.FromSeconds(Math.Max(1, circuitBreaker.DurationOfBreakSeconds))),

            DatabaseProvider.PostgreSQL => builder
                .OrInner<NpgsqlException>(ex => ex.IsTransient)
                .CircuitBreakerAsync(circuitBreaker.HandledEventsAllowedBeforeBreaking, TimeSpan.FromSeconds(Math.Max(1, circuitBreaker.DurationOfBreakSeconds))),

            _ => builder.CircuitBreakerAsync(circuitBreaker.HandledEventsAllowedBeforeBreaking, TimeSpan.FromSeconds(Math.Max(1, circuitBreaker.DurationOfBreakSeconds)))
        };
    }

    private static IAsyncPolicy CreateTimeoutPolicy(TimeoutResiliencePolicy? timeout)
    {
        if (timeout is null || timeout.TimeoutSeconds <= 0)
        {
            return Policy.NoOpAsync();
        }

        return Policy.TimeoutAsync(TimeSpan.FromSeconds(timeout.TimeoutSeconds));
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