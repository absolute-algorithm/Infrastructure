using System.Text;
using AbsoluteAlgorithm.Infrastructure.Constraints;
using AbsoluteAlgorithm.Infrastructure.Exceptions;
using AbsoluteAlgorithm.Infrastructure.Models.Webhooks;
using AbsoluteAlgorithm.Infrastructure.Utilities;
using Microsoft.AspNetCore.Http;

namespace AbsoluteAlgorithm.Infrastructure.Middlewares;

/// <summary>
/// Validates signed webhook requests for configured endpoint prefixes.
/// </summary>
public class WebhookSignatureMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IReadOnlyList<WebhookSignaturePolicy> _policies;

    /// <summary>
    /// Initializes a new instance of the <see cref="WebhookSignatureMiddleware"/> class.
    /// </summary>
    /// <param name="next">The next middleware in the pipeline.</param>
    /// <param name="policies">The webhook signature policies.</param>
    public WebhookSignatureMiddleware(RequestDelegate next, IReadOnlyList<WebhookSignaturePolicy> policies)
    {
        _next = next;
        _policies = policies;
    }

    /// <summary>
    /// Processes the current request.
    /// </summary>
    /// <param name="context">The HTTP context.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public async Task InvokeAsync(HttpContext context)
    {
        var policy = _policies.FirstOrDefault(candidate =>
            !string.IsNullOrWhiteSpace(candidate.PathPrefix)
            && context.Request.Path.StartsWithSegments(candidate.PathPrefix, StringComparison.OrdinalIgnoreCase));

        if (policy is null)
        {
            await _next(context);
            return;
        }

        var signature = context.Request.Headers[policy.SignatureHeaderName].ToString();
        var timestamp = context.Request.Headers[policy.TimestampHeaderName].ToString();
        if (string.IsNullOrWhiteSpace(signature) || string.IsNullOrWhiteSpace(timestamp))
        {
            throw new ApiException(ERRORCODE.UNAUTHORIZED, "The request signature headers are missing.");
        }

        var secret = Environment.GetEnvironmentVariable(policy.SecretName);
        if (string.IsNullOrWhiteSpace(secret))
        {
            throw new InvalidOperationException($"Webhook secret '{policy.SecretName}' is missing.");
        }

        context.Request.EnableBuffering();
        string payload;
        using (var reader = new StreamReader(context.Request.Body, Encoding.UTF8, detectEncodingFromByteOrderMarks: false, leaveOpen: true))
        {
            payload = await reader.ReadToEndAsync(context.RequestAborted);
        }

        context.Request.Body.Position = 0;

        if (!RequestSignatureUtility.VerifySignature(payload, timestamp, signature, secret, policy.Algorithm, policy.AllowedClockSkewSeconds))
        {
            throw new ApiException(ERRORCODE.UNAUTHORIZED, "The request signature is invalid.");
        }

        await _next(context);
    }
}