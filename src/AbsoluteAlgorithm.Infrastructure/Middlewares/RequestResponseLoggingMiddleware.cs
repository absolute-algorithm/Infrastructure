using System;
using System.Text;
using AbsoluteAlgorithm.Core.Diagnostics;
using Microsoft.AspNetCore.Http;

namespace AbsoluteAlgorithm.Infrastructure.Middlewares;

/// <summary>
/// Adds normalized request metadata headers to the request and response.
/// </summary>
public class RequestResponseLoggingMiddleware
{
    private readonly RequestDelegate _next;

    /// <summary>
    /// Initializes a new instance of the <see cref="RequestResponseLoggingMiddleware"/> class.
    /// </summary>
    /// <param name="next">The next middleware in the pipeline.</param>
    public RequestResponseLoggingMiddleware(RequestDelegate next)
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
        context.Request.EnableBuffering();

        var requestBody = await ReadRequestBody(context.Request);
        Logger.Info($"[HTTP REQUEST] | {context.Request.Method} | {context.Request.Path} | Body: {requestBody}");

        var originalBodyStream = context.Response.Body;
        using var responseBody = new MemoryStream();
        context.Response.Body = responseBody;

        await _next(context);

        var responseBodyContent = await ReadResponseBody(context.Response);
        Logger.Info($"[HTTP RESPONSE] | {context.Response.StatusCode} | Body: {responseBodyContent}");

        await responseBody.CopyToAsync(originalBodyStream);
    }

    private static async Task<string> ReadRequestBody(HttpRequest request)
    {
        request.Body.Position = 0; // Reset to start
        using var reader = new StreamReader(request.Body, Encoding.UTF8, leaveOpen: true);
        var body = await reader.ReadToEndAsync();
        request.Body.Position = 0; // Reset for downstream logic
        return string.IsNullOrWhiteSpace(body) ? "[Empty]" : body;
    }

    private static async Task<string> ReadResponseBody(HttpResponse response)
    {
        response.Body.Seek(0, SeekOrigin.Begin);
        using var reader = new StreamReader(response.Body, Encoding.UTF8, leaveOpen: true);
        var body = await reader.ReadToEndAsync();
        response.Body.Seek(0, SeekOrigin.Begin);
        return string.IsNullOrWhiteSpace(body) ? "[Empty]" : body;
    }
}
