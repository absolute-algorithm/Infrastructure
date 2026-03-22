using System.Text;
using FileFormula.Api.Infrastructure.Constraints;
using FileFormula.Api.Infrastructure.Exceptions;
using FileFormula.Api.Infrastructure.Models.Idempotency;
using FileFormula.Api.Infrastructure.Services;
using Microsoft.AspNetCore.Http;

namespace FileFormula.Api.Infrastructure.Middlewares;

/// <summary>
/// Replays successful responses for duplicate idempotent write requests.
/// </summary>
public class IdempotencyMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IdempotencyPolicy _policy;
    private readonly IIdempotencyStore _store;

    /// <summary>
    /// Initializes a new instance of the <see cref="IdempotencyMiddleware"/> class.
    /// </summary>
    /// <param name="next">The next middleware in the pipeline.</param>
    /// <param name="policy">The idempotency policy.</param>
    /// <param name="store">The idempotency store.</param>
    public IdempotencyMiddleware(
        RequestDelegate next,
        IdempotencyPolicy policy,
        IIdempotencyStore store)
    {
        _next = next;
        _policy = policy;
        _store = store;
    }

    /// <summary>
    /// Processes the current request.
    /// </summary>
    /// <param name="context">The HTTP context.</param>
    /// <param name="requestMetadataAccessor">The request metadata accessor.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public async Task InvokeAsync(HttpContext context, IRequestMetadataAccessor requestMetadataAccessor)
    {
        if (!ShouldHandle(context.Request.Method))
        {
            await _next(context);
            return;
        }

        var idempotencyKey = context.Request.Headers[_policy.HeaderName].ToString();
        if (string.IsNullOrWhiteSpace(idempotencyKey))
        {
            if (_policy.RequireHeader)
            {
                throw new ApiException(ERRORCODE.BADREQUEST, $"The {_policy.HeaderName} header is required for {context.Request.Method} requests.");
            }

            await _next(context);
            return;
        }

        var cacheKey = CreateCacheKey(context, idempotencyKey, requestMetadataAccessor);
        var lookup = _store.Get(cacheKey);
        if (lookup.HasStoredResponse && lookup.Response is not null)
        {
            await ReplayAsync(context, lookup.Response);
            return;
        }

        if (lookup.IsInProgress || !_store.TryBegin(cacheKey))
        {
            throw new ApiException(ERRORCODE.CONFLICT, "A request with the same idempotency key is already in progress.");
        }

        var originalBody = context.Response.Body;
        await using var responseBuffer = new MemoryStream();
        context.Response.Body = responseBuffer;

        try
        {
            await _next(context);

            responseBuffer.Position = 0;
            await responseBuffer.CopyToAsync(originalBody, context.RequestAborted);

            if (IsCacheable(context.Response.StatusCode) && responseBuffer.Length <= _policy.MaximumResponseBodyBytes)
            {
                _store.Complete(
                    cacheKey,
                    new IdempotencyStoredResponse
                    {
                        StatusCode = context.Response.StatusCode,
                        ContentType = context.Response.ContentType,
                        Body = responseBuffer.ToArray()
                    },
                    TimeSpan.FromMinutes(_policy.ExpirationMinutes));
            }
            else
            {
                _store.Abandon(cacheKey);
            }
        }
        catch
        {
            _store.Abandon(cacheKey);
            throw;
        }
        finally
        {
            context.Response.Body = originalBody;
        }
    }

    private bool ShouldHandle(string method)
    {
        return _policy.ReplayableMethods.Any(candidate => string.Equals(candidate, method, StringComparison.OrdinalIgnoreCase));
    }

    private string CreateCacheKey(HttpContext context, string idempotencyKey, IRequestMetadataAccessor requestMetadataAccessor)
    {
        var metadata = requestMetadataAccessor.Current;
        var subject = metadata?.UserId ?? metadata?.ClientIpAddress ?? "anonymous";
        var path = _policy.IncludeQueryStringInKey
            ? $"{context.Request.Path}{context.Request.QueryString}"
            : context.Request.Path.ToString();

        return $"{context.Request.Method}:{path}:{subject}:{idempotencyKey.Trim()}";
    }

    private static bool IsCacheable(int statusCode)
    {
        return statusCode is >= StatusCodes.Status200OK and < StatusCodes.Status300MultipleChoices;
    }

    private static async Task ReplayAsync(HttpContext context, IdempotencyStoredResponse response)
    {
        context.Response.StatusCode = response.StatusCode;
        context.Response.ContentType = response.ContentType ?? "application/json";
        context.Response.Headers[HEADER.IDEMPOTENCYREPLAYED] = "true";

        await context.Response.BodyWriter.WriteAsync(response.Body, context.RequestAborted);
    }
}