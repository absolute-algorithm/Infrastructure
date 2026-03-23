using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace AbsoluteAlgorithm.Infrastructure.Health;

/// <summary>
/// Represents a health check for an S3-compatible storage bucket.
/// </summary>
public class S3StorageHealthCheck : IHealthCheck
{
    private readonly IAmazonS3 _client;
    private readonly string _bucketName;

    /// <summary>
    /// Initializes a new instance of the <see cref="S3StorageHealthCheck"/> class.
    /// </summary>
    /// <param name="client">The S3 client.</param>
    /// <param name="bucketName">The bucket name to check.</param>
    public S3StorageHealthCheck(IAmazonS3 client, string bucketName)
    {
        _client = client;
        _bucketName = bucketName;
    }

    /// <summary>
    /// Checks the health of the configured S3 bucket.
    /// </summary>
    /// <param name="context">The health check context.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>A <see cref="HealthCheckResult"/> value that describes the health state.</returns>
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken ct = default)
    {
        try
        {
            await _client.ListObjectsV2Async(new ListObjectsV2Request
            {
                BucketName = _bucketName,
                MaxKeys = 1
            }, ct);

            return HealthCheckResult.Healthy();
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy(ex.Message, ex);
        }
    }
}
