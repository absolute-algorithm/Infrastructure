using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using AbsoluteAlgorithm.Infrastructure.Services;

namespace AbsoluteAlgorithm.Infrastructure.Health;

/// <summary>
/// Represents a health check for a Google Cloud Storage registration.
/// </summary>
public class GcpStorageHealthCheck : IHealthCheck
{
    private readonly IServiceProvider _sp;
    private readonly string _policyName;

    /// <summary>
    /// Initializes a new instance of the <see cref="GcpStorageHealthCheck"/> class.
    /// </summary>
    /// <param name="sp">The service provider used to resolve the storage service.</param>
    /// <param name="policyName">The name of the keyed storage registration.</param>
    public GcpStorageHealthCheck(IServiceProvider sp, string policyName)
    {
        _sp = sp;
        _policyName = policyName;
    }

    /// <summary>
    /// Checks the health of the configured storage registration.
    /// </summary>
    /// <param name="context">The health check context.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>A <see cref="HealthCheckResult"/> value that describes the health state.</returns>
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken ct = default)
    {
        try
        {
            var storageService = _sp.GetRequiredKeyedService<StorageService>(_policyName);
            await storageService.GetDownloadUrlAsync("health-probe", TimeSpan.FromSeconds(1));

            return HealthCheckResult.Healthy($"Bucket for {_policyName} is reachable.");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy($"GCP check failed for {_policyName}: {ex.Message}");
        }
    }
}