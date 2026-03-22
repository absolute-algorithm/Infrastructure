using Amazon.S3;
using Amazon.S3.Model;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Sas;
using FileFormula.Api.Infrastructure.Models.Storage;
using FileFormula.Api.Infrastructure.Sanitizers;
using Google.Apis.Auth.OAuth2;
using Google.Cloud.Storage.V1;
using Minio;
using Minio.DataModel.Args;

namespace FileFormula.Api.Infrastructure.Services;

/// <summary>
/// Provides object storage operations for a configured provider.
/// </summary>
public class StorageService
{
    private readonly StoragePolicy _policy;

    /// <summary>
    /// Initializes a new instance of the <see cref="StorageService"/> class.
    /// </summary>
    /// <param name="policy">The storage policy.</param>
    public StorageService(StoragePolicy policy)
    {
        _policy = policy;
    }

    /// <summary>
    /// Uploads content to the configured storage provider.
    /// </summary>
    /// <param name="fileContent">The file content to upload.</param>
    /// <returns>The generated object name.</returns>
    public async Task<string> UploadAsync(FileContent fileContent)
    {
        object client = StorageFactory.Create(_policy);
        string bucket = _policy.BucketName ?? "default-bucket";

        string safeExtension = FileNameSanitizer.GetSafeExtension(fileContent.FileName);
        string fileName = $"{Convert.ToBase64String(Guid.NewGuid().ToByteArray()).Replace("/", "_").Replace("+", "-")[..22]}{safeExtension}";

        using Stream stream = fileContent.GetContentStream();
        if (stream.CanSeek) stream.Seek(0, SeekOrigin.Begin);

        return client switch
        {
            IMinioClient minio => await UploadToMinio(minio, bucket, fileName, fileContent.ContentType, stream),
            BlobServiceClient azure => await UploadToAzure(azure, bucket, fileName, fileContent.ContentType, stream),
            IAmazonS3 s3 => await UploadToS3(s3, bucket, fileName, fileContent.ContentType, stream),
            StorageClient gcp => await UploadToGcp(gcp, bucket, fileName, fileContent.ContentType, stream),
            _ => throw new NotSupportedException($"Provider {_policy.StorageProvider} is not supported.")
        };
    }

    /// <summary>
    /// Creates a temporary download URL for an object.
    /// </summary>
    /// <param name="fileName">The stored object name.</param>
    /// <param name="expiry">The lifetime of the generated URL.</param>
    /// <returns>The generated download URL.</returns>
    public async Task<string> GetDownloadUrlAsync(string fileName, TimeSpan expiry)
    {
        object client = StorageFactory.Create(_policy);
        string bucket = _policy.BucketName ?? "default-bucket";

        return client switch
        {
            IMinioClient minio => await GetMinioUrl(minio, bucket, fileName, expiry),
            BlobServiceClient azure => GetAzureUrl(azure, bucket, fileName, expiry),
            IAmazonS3 s3 => GetS3Url(s3, bucket, fileName, expiry),
            StorageClient gcp => await GetGcpUrl(gcp, bucket, fileName, expiry),
            _ => throw new NotSupportedException("Provider not supported.")
        };
    }


    #region Helpers
    private async Task<string> UploadToMinio(IMinioClient client, string bucket, string fileName, string contentType, Stream stream)
    {
        bool exists = await client.BucketExistsAsync(new BucketExistsArgs().WithBucket(bucket));
        if (!exists)
        {
            await client.MakeBucketAsync(new MakeBucketArgs().WithBucket(bucket));
        }

        var args = new PutObjectArgs()
            .WithBucket(bucket)
            .WithObject(fileName)
            .WithStreamData(stream)
            .WithObjectSize(stream.Length)
            .WithContentType(contentType);

        await client.PutObjectAsync(args);
        return fileName;
    }

    private async Task<string> UploadToAzure(BlobServiceClient client, string bucket, string fileName, string contentType, Stream stream)
    {
        var container = client.GetBlobContainerClient(bucket);

        // Built-in convenience method
        await container.CreateIfNotExistsAsync(PublicAccessType.None);

        var blob = container.GetBlobClient(fileName);
        await blob.UploadAsync(stream, new BlobHttpHeaders { ContentType = contentType });
        return fileName;
    }

    private async Task<string> UploadToS3(IAmazonS3 client, string bucket, string fileName, string contentType, Stream stream)
    {
        if (!await Amazon.S3.Util.AmazonS3Util.DoesS3BucketExistV2Async(client, bucket))
        {
            await client.PutBucketAsync(bucket);
        }

        var request = new PutObjectRequest
        {
            BucketName = bucket,
            Key = fileName,
            InputStream = stream,
            ContentType = contentType
        };

        await client.PutObjectAsync(request);
        return fileName;
    }

    private async Task<string> UploadToGcp(StorageClient client, string bucket, string fileName, string contentType, Stream stream)
    {
        try
        {
            await client.GetBucketAsync(bucket);
        }
        catch (Google.GoogleApiException ex) when (ex.Error.Code == 404)
        {
            await client.CreateBucketAsync(_policy.GcpProjectId, bucket);
        }

        await client.UploadObjectAsync(bucket, fileName, contentType, stream);
        return fileName;
    }

    private async Task<string> GetMinioUrl(IMinioClient client, string bucket, string fileName, TimeSpan expiry)
    {
        var args = new PresignedGetObjectArgs()
            .WithBucket(bucket)
            .WithObject(fileName)
            .WithExpiry((int)expiry.TotalSeconds);

        return await client.PresignedGetObjectAsync(args);
    }

    private string GetAzureUrl(BlobServiceClient client, string bucket, string fileName, TimeSpan expiry)
    {
        var blobClient = client.GetBlobContainerClient(bucket).GetBlobClient(fileName);

        // Check if we can actually sign (requires Account Key or User Delegation)
        if (!blobClient.CanGenerateSasUri)
            throw new InvalidOperationException("Client lacks permissions to generate SAS.");

        var sasUri = blobClient.GenerateSasUri(BlobSasPermissions.Read, DateTimeOffset.UtcNow.Add(expiry));
        return sasUri.ToString();
    }

    private string GetS3Url(IAmazonS3 client, string bucket, string fileName, TimeSpan expiry)
    {
        var request = new GetPreSignedUrlRequest
        {
            BucketName = bucket,
            Key = fileName,
            Expires = DateTime.UtcNow.Add(expiry),
            Verb = HttpVerb.GET
        };

        return client.GetPreSignedURL(request);
    }

    private async Task<string> GetGcpUrl(StorageClient client, string bucket, string fileName, TimeSpan expiry)
    {
        string secret = Environment.GetEnvironmentVariable(_policy.ConnectionStringName!) ?? throw new InvalidOperationException("GCP Credentials missing.");

        GoogleCredential credential;
        if (secret.StartsWith("{"))
        {
            credential = GoogleCredential.FromJson(secret);
        }
        else
        {
            credential = GoogleCredential.FromFile(secret);
        }

        UrlSigner signer = UrlSigner.FromCredential(credential);

        return await signer.SignAsync(bucket, fileName, expiry, HttpMethod.Get);
    }
    #endregion
}