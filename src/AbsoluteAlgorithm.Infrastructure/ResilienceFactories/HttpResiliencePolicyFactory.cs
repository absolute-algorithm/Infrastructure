using AbsoluteAlgorithm.Core.Models.Resilience;
using AbsoluteAlgorithm.Core.Resilience; // Your new Core Engine
using Polly;
using Polly.Timeout;

namespace AbsoluteAlgorithm.Infrastructure.ResilienceFactories;

/// <summary>
/// Creates Polly policies for outbound HTTP requests leveraging the AbsoluteAlgorithm.Core engine.
/// </summary>
public static class HttpResiliencePolicyFactory
{
    /// <summary>
    /// Creates an asynchronous HTTP resilience policy for the supplied configuration.
    /// </summary>
    /// <param name="policy">The resilience policy configuration.</param>
    /// <returns>An asynchronous HTTP response policy.</returns>
    public static IAsyncPolicy<HttpResponseMessage> CreateHttpPolicy(ResiliencePolicy? policy)
    {
        return ResiliencePolicyFactory.CreatePolicy<HttpResponseMessage>(
            policy,
            shouldHandleResult: ShouldHandleResult,
            shouldHandleException: IsTransientHttpException
        );
    }

    /// <summary>
    /// Experts knowledge: Which HTTP status codes warrant a retry?
    /// </summary>
    private static bool ShouldHandleResult(HttpResponseMessage response)
    {
        var statusCode = (int)response.StatusCode;
        // 408 (Request Timeout), 429 (Too Many Requests), 5xx (Server Errors)
        return statusCode == 408 || statusCode == 429 || statusCode >= 500;
    }

    /// <summary>
    /// Experts knowledge: Which Exceptions warrant a retry?
    /// </summary>
    private static bool IsTransientHttpException(Exception ex)
    {
        return ex is HttpRequestException 
            || ex is TimeoutRejectedException 
            || ex is TaskCanceledException;
    }
}