using Polly;

namespace AbsoluteAlgorithm.Infrastructure.ResilienceFactories;

/// <summary>
/// Wraps outgoing HTTP requests in a Polly policy.
/// </summary>
public sealed class PollyHttpMessageHandler : DelegatingHandler
{
    private readonly IAsyncPolicy<HttpResponseMessage> _policy;

    /// <summary>
    /// Initializes a new instance of the <see cref="PollyHttpMessageHandler"/> class.
    /// </summary>
    /// <param name="policy">The HTTP response policy to execute for each request.</param>
    public PollyHttpMessageHandler(IAsyncPolicy<HttpResponseMessage> policy)
    {
        _policy = policy ?? throw new ArgumentNullException(nameof(policy));
    }

    /// <inheritdoc />
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        return _policy.ExecuteAsync(
            token => base.SendAsync(request, token),
            cancellationToken);
    }
}