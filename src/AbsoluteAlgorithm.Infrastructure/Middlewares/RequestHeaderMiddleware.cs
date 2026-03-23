using AbsoluteAlgorithm.Infrastructure.Constraints;
using Microsoft.AspNetCore.Http;

namespace AbsoluteAlgorithm.Infrastructure.Middlewares;

/// <summary>
/// Adds normalized request metadata headers to the request and response.
/// </summary>
public class RequestHeaderMiddleware
{
    private readonly RequestDelegate _next;

    /// <summary>
    /// Initializes a new instance of the <see cref="RequestHeaderMiddleware"/> class.
    /// </summary>
    /// <param name="next">The next middleware in the pipeline.</param>
    public RequestHeaderMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    /// <summary>
    /// Processes the current request.
    /// </summary>
    /// <param name="context">The current HTTP context.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public async Task InvokeAsync(HttpContext context)
    {
        string correlationId = GetOrGenerateCorrelationId(context);

        string ipAddress = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";

        string userAgent = context.Request.Headers[HEADER.REQUESTUSERAGENT].ToString() ?? "unknown";

        context.Request.Headers[HEADER.CORRELATIONID] = correlationId;
        context.Request.Headers[HEADER.IPADDRESS] = ipAddress;
        context.Request.Headers[HEADER.USERAGENT] = userAgent;

        context.Response.Headers[HEADER.CORRELATIONID] = correlationId;
        context.Response.Headers[HEADER.IPADDRESS] = ipAddress;
        context.Response.Headers[HEADER.USERAGENT] = userAgent;

        await _next(context).ConfigureAwait(false);
    }

    private string GetOrGenerateCorrelationId(HttpContext context)
    {
        if (context.Request.Headers.TryGetValue(HEADER.CORRELATIONID, out var existingId))
        {
            return existingId.ToString();
        }
        return Guid.NewGuid().ToString("N");
    }
}