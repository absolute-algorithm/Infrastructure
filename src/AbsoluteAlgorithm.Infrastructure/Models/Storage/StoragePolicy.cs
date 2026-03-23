using System;
using System.Text.Json.Serialization;
using AbsoluteAlgorithm.Infrastructure.Enums;

namespace AbsoluteAlgorithm.Infrastructure.Models.Storage;

/// <summary>
/// Represents the configuration for a named object storage registration.
/// </summary>
public class StoragePolicy
{
    /// <summary>
    /// Gets the name used to register and resolve the storage service.
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; init; } = null!;

    /// <summary>
    /// Gets the storage provider.
    /// </summary>
    [JsonPropertyName("storageProvider")]
    public StorageProvider StorageProvider { get; init; }

    /// <summary>
    /// Gets the name of the environment variable that contains the provider credentials or connection string.
    /// </summary>
    [JsonPropertyName("connectionStringName")]
    public string ConnectionStringName { get; init; } = null!;

    /// <summary>
    /// Gets the bucket or container name used by the provider.
    /// </summary>
    [JsonPropertyName("bucketName")]
    public string BucketName { get; init; } = null!;

    /// <summary>
    /// Gets the Google Cloud project identifier used when creating buckets.
    /// </summary>
    [JsonPropertyName("gcpProjectId")]
    public string? GcpProjectId { get; init; }
}
