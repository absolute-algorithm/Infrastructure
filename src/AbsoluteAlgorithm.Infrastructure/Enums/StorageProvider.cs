namespace AbsoluteAlgorithm.Infrastructure.Enums;

/// <summary>
/// Specifies the object storage provider.
/// </summary>
public enum StorageProvider : byte
{
    /// <summary>
    /// MinIO-compatible object storage.
    /// </summary>
    Minio = 1,

    /// <summary>
    /// Azure Blob Storage.
    /// </summary>
    AzureBlob,

    /// <summary>
    /// Google Cloud Storage.
    /// </summary>
    GoogleCloud,

    /// <summary>
    /// Amazon S3.
    /// </summary>
    S3
}
