using System;
using Amazon.S3;
using Azure.Storage.Blobs;
using FileFormula.Api.Infrastructure.Enums;
using FileFormula.Api.Infrastructure.Models.Storage;
using Google.Cloud.Storage.V1;
using Minio;

namespace FileFormula.Api.Infrastructure.Services;

/// <summary>
/// Creates storage clients for supported providers.
/// </summary>
public static class StorageFactory
{
    internal static object Create(StoragePolicy storagePolicy)
    {
        string connectionString = Environment.GetEnvironmentVariable(storagePolicy.ConnectionStringName!) ?? throw new InvalidOperationException($"Storage secret '{storagePolicy.ConnectionStringName}' is missing.");

        return storagePolicy.StorageProvider switch
        {
            StorageProvider.Minio => CreateMinioClient(connectionString),
            StorageProvider.AzureBlob => CreateBlobServiceClient(connectionString),
            StorageProvider.S3 => CreateS3Client(connectionString),
            StorageProvider.GoogleCloud => CreateGcpClient(connectionString),
            _ => throw new NotSupportedException($"Provider {storagePolicy.StorageProvider} not implemented.")
        };
    }

    private static IMinioClient CreateMinioClient(string connectionString)
    {
        var parts = ParseQueryString(connectionString);

        string rawEndpoint = parts["Endpoint"];

        var uri = new Uri(rawEndpoint);
        string sanitizedEndpoint = uri.Authority;

        string minioAccessKey = parts["AccessKey"];
        string minioSecretKey = parts["SecretKey"];

        bool minioSsl = parts.ContainsKey("Secure")
            ? bool.Parse(parts["Secure"])
            : uri.Scheme == Uri.UriSchemeHttps;

        return new MinioClient()
            .WithEndpoint(sanitizedEndpoint)
            .WithCredentials(minioAccessKey, minioSecretKey)
            .WithSSL(minioSsl)
            .Build();
    }

    private static BlobServiceClient CreateBlobServiceClient(string connectionString) => new BlobServiceClient(connectionString);

    private static IAmazonS3 CreateS3Client(string connectionString)
    {
        var parts = connectionString.Split(';', StringSplitOptions.RemoveEmptyEntries).Select(x => x.Split('=', 2)).ToDictionary(kv => kv[0].Trim(), kv => kv[1].Trim(), StringComparer.OrdinalIgnoreCase);

        // var parts = ParseQueryString(connectionString);

        var config = new AmazonS3Config
        {
            ServiceURL = parts["Endpoint"],
            AuthenticationRegion = parts.ContainsKey("Region") ? parts["Region"] : "us-east-1",
            ForcePathStyle = true,
            UseHttp = true
        };

        return new AmazonS3Client(
            parts["AccessKey"],
            parts["SecretKey"],
            config
        );
    }

    /// <summary>
    /// Ensures the bucket specified by the storage policy exists, creating it if necessary.
    /// </summary>
    /// <param name="policy">The storage policy.</param>
    internal static async Task EnsureBucketExistsAsync(StoragePolicy policy)
    {
        object client = Create(policy);
        string bucket = policy.BucketName;

        switch (client)
        {
            case IMinioClient minio:
                if (!await minio.BucketExistsAsync(new Minio.DataModel.Args.BucketExistsArgs().WithBucket(bucket)))
                    await minio.MakeBucketAsync(new Minio.DataModel.Args.MakeBucketArgs().WithBucket(bucket));
                break;

            case BlobServiceClient azure:
                await azure.GetBlobContainerClient(bucket).CreateIfNotExistsAsync();
                break;

            case IAmazonS3 s3:
                if (!await Amazon.S3.Util.AmazonS3Util.DoesS3BucketExistV2Async(s3, bucket))
                    await s3.PutBucketAsync(bucket);
                break;

            case StorageClient gcp:
                try { await gcp.GetBucketAsync(bucket); }
                catch (Google.GoogleApiException ex) when (ex.Error.Code == 404)
                {
                    await gcp.CreateBucketAsync(policy.GcpProjectId, bucket);
                }
                break;
        }
    }

    private static StorageClient CreateGcpClient(string connectionString)
    {
        // For GCP, the "connectionString" should be the PATH to your service-account.json
        // Or the raw JSON content itself.
        if (connectionString.StartsWith("{"))
            return StorageClient.Create(Google.Apis.Auth.OAuth2.GoogleCredential.FromJson(connectionString));

        return StorageClient.Create(Google.Apis.Auth.OAuth2.GoogleCredential.FromFile(connectionString));
    }

    private static Dictionary<string, string> ParseQueryString(string input) => input.Split(';', StringSplitOptions.RemoveEmptyEntries).Select(x => x.Split('=')).ToDictionary(kv => kv[0], kv => kv[1], StringComparer.OrdinalIgnoreCase);
}
