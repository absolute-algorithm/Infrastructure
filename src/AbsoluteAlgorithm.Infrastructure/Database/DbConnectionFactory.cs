using System.Data;
using AbsoluteAlgorithm.Core.Enums;
using AbsoluteAlgorithm.Core.Models.Database;
using Microsoft.Data.SqlClient;
using Npgsql;

namespace AbsoluteAlgorithm.Infrastructure.Database;

/// <summary>
/// Creates database connections for supported providers.
/// </summary>
public static class DbConnectionFactory
{
    internal static IDbConnection Create(DatabasePolicy policy, string connectionString)
    {
        var formattedString = GetFormattedString(policy, connectionString);

        return policy.DatabaseProvider switch
        {
            RelationalDatabaseProvider.PostgreSQL => new NpgsqlConnection(formattedString),
            RelationalDatabaseProvider.MSSQL => new SqlConnection(formattedString),
            _ => throw new NotSupportedException($"Database type {policy.DatabaseProvider} is not supported.")
        };
    }

    private static string GetFormattedString(DatabasePolicy policy, string rawString)
    {
        return policy.DatabaseProvider switch
        {
            RelationalDatabaseProvider.PostgreSQL => new NpgsqlConnectionStringBuilder(rawString)
            {
                MaxPoolSize = policy.MaxPoolSize,
                MinPoolSize = policy.MinPoolSize,
                CommandTimeout = policy.CommandTimeoutSeconds,
                Pooling = true
            }.ToString(),

            RelationalDatabaseProvider.MSSQL => new SqlConnectionStringBuilder(rawString)
            {
                MaxPoolSize = policy.MaxPoolSize,
                MinPoolSize = policy.MinPoolSize,
                ConnectTimeout = policy.CommandTimeoutSeconds
            }.ToString(),

            _ => rawString
        };
    }
}