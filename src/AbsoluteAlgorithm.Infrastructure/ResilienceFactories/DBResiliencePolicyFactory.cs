using System.Data.Common;
using AbsoluteAlgorithm.Core.Enums;
using AbsoluteAlgorithm.Core.Models.Resilience;
using AbsoluteAlgorithm.Core.Resilience;
using Microsoft.Data.SqlClient;
using Npgsql;
using Polly;

namespace AbsoluteAlgorithm.Infrastructure.ResilienceFactories;

/// <summary>
/// Specialized factory for Database resilience, leveraging the AbsoluteAlgorithm.Core engine.
/// </summary>
public static class DBResiliencePolicyFactory
{
    /// <summary>
    /// Creates a database-aware resilience policy.
    /// </summary>
    /// <typeparam name="T">The return type of the database operation.</typeparam>
    public static IAsyncPolicy<T> CreateDbPolicy<T>(DatabaseProvider provider, ResiliencePolicy? policy)
    {
        return ResiliencePolicyFactory.CreatePolicy<T>(
            policy,
            shouldHandleResult: _ => false, // Database results usually don't trigger retries; exceptions do.
            shouldHandleException: ex => IsTransientException(provider, ex)
        );
    }

    /// <summary>
    /// Checks if an exception is considered transient for the specific database provider.
    /// This keeps the "Expert Knowledge" in Infrastructure while the "Logic" stays in Core.
    /// </summary>
    private static bool IsTransientException(DatabaseProvider provider, Exception ex)
    {
        // Handle common base exceptions
        if (ex is DbException || ex is TimeoutException) return true;

        // Handle provider-specific "Inner" exceptions
        return provider switch
        {
            DatabaseProvider.PostgreSQL =>
                ex is NpgsqlException nex && nex.IsTransient,

            DatabaseProvider.MSSQL =>
                ex is SqlException sex && sex.Number == 1205, // 1205 = Deadlock

            _ => false
        };
    }

    /// <summary>
    /// Helper for database commands that do not return a value (e.g., ExecuteNonQuery).
    /// </summary>
    public static IAsyncPolicy CreateDbCommandPolicy(DatabaseProvider provider, ResiliencePolicy? policy)
    {
        return ResiliencePolicyFactory.CreatePolicy(
            policy, shouldHandleException: ex => IsTransientException(provider, ex)
        );
    }
}