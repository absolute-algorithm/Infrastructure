using System.Data.Common;
using Microsoft.Data.SqlClient;
using Npgsql;
using Dapper;
using AbsoluteAlgorithm.Core.Enums;
using AbsoluteAlgorithm.Core.Diagnostics;

namespace AbsoluteAlgorithm.Infrastructure.Database;

internal static class DatabaseInitializer
{
    internal static void Initialize(string connectionString, DatabaseProvider provider, string databaseScript)
    {
        try
        {
            // 1. Ensure the DB is physically created (Sync)
            EnsureDatabaseExists(connectionString, provider);

            // 2. Open a connection to the target DB
            using var connection = CreateConnection(connectionString, provider);
            connection.Open();

            // 3. Execute the Init Script inside a standard transaction
            using var tx = connection.BeginTransaction();
            try
            {
                connection.Execute(databaseScript, transaction: tx);
                tx.Commit();
                Logger.Info($"{provider} database initialized successfully.");
            }
            catch (Exception ex)
            {
                tx.Rollback();
                Logger.Error("Transaction failed during database initialization script.", ex);
                throw;
            }
        }
        catch (Exception ex)
        {
            Logger.Error($"Absolute System Failure: Database could not be initialized for {provider}.", ex);
            throw;
        }
    }

    private static void EnsureDatabaseExists(string connectionString, DatabaseProvider provider)
    {
        string masterConnString;
        string dbName;
        string checkSql;
        string createSql;

        switch (provider)
        {
            case DatabaseProvider.PostgreSQL:
                var pg = new NpgsqlConnectionStringBuilder(connectionString);
                dbName = pg.Database!;
                pg.Database = "postgres";
                masterConnString = pg.ConnectionString;
                checkSql = "SELECT 1 FROM pg_database WHERE datname = @name";
                createSql = $"CREATE DATABASE \"{dbName}\" ENCODING 'UTF8'";
                break;

            case DatabaseProvider.MSSQL:
                var ms = new Microsoft.Data.SqlClient.SqlConnectionStringBuilder(connectionString);
                dbName = ms.InitialCatalog;
                ms.InitialCatalog = "master";
                masterConnString = ms.ConnectionString;
                checkSql = "SELECT 1 FROM sys.databases WHERE name = @name";
                createSql = $"CREATE DATABASE [{dbName}]";
                break;

            default:
                throw new ArgumentOutOfRangeException(nameof(provider), provider, "Provider not implemented.");
        }

        using var adminConn = CreateConnection(masterConnString, provider);
        adminConn.Open();

        var exists = adminConn.ExecuteScalar<int>(checkSql, new { name = dbName });

        if (exists != 1)
        {
            adminConn.Execute(createSql);
            Logger.Info($"Created {provider} database: {dbName}");
        }
    }

    private static DbConnection CreateConnection(string connString, DatabaseProvider provider)
    {
        return provider switch
        {
            DatabaseProvider.PostgreSQL => new NpgsqlConnection(connString),
            DatabaseProvider.MSSQL => new SqlConnection(connString),
            _ => throw new NotSupportedException($"Provider {provider} is not supported.")
        };
    }
}