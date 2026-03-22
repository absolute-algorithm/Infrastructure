using System.Text.Json.Serialization;
using FileFormula.Api.Infrastructure.Enums;
using FileFormula.Api.Infrastructure.Models.Resilience;

namespace FileFormula.Api.Infrastructure.Models.Database;

/// <summary>
/// Represents the configuration for a named relational database registration.
/// </summary>
public class DatabasePolicy
{
    /// <summary>
    /// Gets the name used to register and resolve the repository for the database.
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; init; } = null!;

    /// <summary>
    /// Gets the database provider.
    /// </summary>
    [JsonPropertyName("databaseProvider")]
    public DatabaseProvider DatabaseProvider { get; init; }

    /// <summary>
    /// Gets the name of the environment variable that contains the connection string.
    /// </summary>
    [JsonPropertyName("connectionStringName")]
    public string ConnectionStringName { get; init; } = null!;

    /// <summary>
    /// Gets a value indicating whether the database should be created and initialized during application startup.
    /// </summary>
    [JsonPropertyName("initializeDatabase")]
    public bool InitializeDatabase { get; init; } = false;

    /// <summary>
    /// Gets a value indicating whether the built-in audit table and triggers should be created during startup.
    /// </summary>
    [JsonPropertyName("initializeAuditTable")]
    public bool InitializeAuditTable { get; init; } = false;

    /// <summary>
    /// Gets the optional initialization script executed after database creation.
    /// </summary>
    [JsonPropertyName("initializationScript")]
    public string? InitializationScript { get; init; }

    /// <summary>
    /// Gets the maximum number of pooled connections.
    /// </summary>
    [JsonPropertyName("maxPoolSize")]
    public int MaxPoolSize { get; init; } = 100;

    /// <summary>
    /// Gets the minimum number of pooled connections.
    /// </summary>
    [JsonPropertyName("minPoolSize")]
    public int MinPoolSize { get; init; } = 10;

    /// <summary>
    /// Gets the timeout value, in seconds, applied by the provider-specific connection configuration.
    /// </summary>
    [JsonPropertyName("commandTimeoutSeconds")]
    public int CommandTimeoutSeconds { get; init; } = 30;

    /// <summary>
    /// Gets the resilience configuration applied to repository operations for this database.
    /// </summary>
    [JsonPropertyName("resiliencePolicy")]
    public ResiliencePolicy? ResiliencePolicy { get; init; }
}