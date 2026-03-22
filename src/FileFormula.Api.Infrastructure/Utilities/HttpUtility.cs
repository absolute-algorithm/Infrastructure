using FileFormula.Api.Infrastructure.Constraints;
using FileFormula.Api.Infrastructure.Models.Request;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.WebUtilities;

namespace FileFormula.Api.Infrastructure.Utilities;

/// <summary>
/// Provides helper methods for common HTTP request and response operations.
/// </summary>
public static class HttpUtility
{
    /// <summary>
    /// Gets a request header value.
    /// </summary>
    /// <param name="request">The HTTP request.</param>
    /// <param name="headerName">The header name.</param>
    /// <returns>The header value when present; otherwise, <see langword="null"/>.</returns>
    public static string? GetHeaderValue(HttpRequest request, string headerName)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(headerName);

        return request.Headers.TryGetValue(headerName, out var values)
            ? values.ToString()
            : null;
    }

    /// <summary>
    /// Gets the bearer token from the Authorization header.
    /// </summary>
    /// <param name="request">The HTTP request.</param>
    /// <returns>The bearer token when present; otherwise, <see langword="null"/>.</returns>
    public static string? GetBearerToken(HttpRequest request)
    {
        var authorization = GetHeaderValue(request, "Authorization");
        if (string.IsNullOrWhiteSpace(authorization) || !authorization.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        return authorization[7..].Trim();
    }

    /// <summary>
    /// Gets the client IP address for the current request.
    /// </summary>
    /// <param name="context">The HTTP context.</param>
    /// <returns>The client IP address when available; otherwise, <see langword="null"/>.</returns>
    public static string? GetClientIpAddress(HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return GetHeaderValue(context.Request, HEADER.IPADDRESS)
            ?? context.Connection.RemoteIpAddress?.ToString();
    }

    /// <summary>
    /// Gets the correlation identifier for the current request.
    /// </summary>
    /// <param name="context">The HTTP context.</param>
    /// <returns>The correlation identifier when present; otherwise, <see langword="null"/>.</returns>
    public static string? GetCorrelationId(HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return GetHeaderValue(context.Request, HEADER.CORRELATIONID)
            ?? GetHeaderValue(context.Response.Headers, HEADER.CORRELATIONID);
    }

    /// <summary>
    /// Gets the tenant identifier for the current request.
    /// </summary>
    /// <param name="context">The HTTP context.</param>
    /// <returns>The tenant identifier when present; otherwise, <see langword="null"/>.</returns>
    public static string? GetTenantId(HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return GetHeaderValue(context.Request, HEADER.TENANTID);
    }

    /// <summary>
    /// Gets the idempotency key for the current request.
    /// </summary>
    /// <param name="context">The HTTP context.</param>
    /// <returns>The idempotency key when present; otherwise, <see langword="null"/>.</returns>
    public static string? GetIdempotencyKey(HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return GetHeaderValue(context.Request, HEADER.IDEMPOTENCYKEY);
    }

    /// <summary>
    /// Builds normalized request metadata for the current HTTP context.
    /// </summary>
    /// <param name="context">The HTTP context.</param>
    /// <returns>The normalized request metadata.</returns>
    public static RequestMetadata GetRequestMetadata(HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return new RequestMetadata
        {
            CorrelationId = GetCorrelationId(context),
            ClientIpAddress = GetClientIpAddress(context),
            IdempotencyKey = GetIdempotencyKey(context),
            Method = context.Request.Method,
            Path = context.Request.Path.Value,
            TenantId = GetTenantId(context),
            UserAgent = GetHeaderValue(context.Request, HEADER.USERAGENT) ?? GetHeaderValue(context.Request, HEADER.REQUESTUSERAGENT),
            UserId = context.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? context.User.Identity?.Name,
            IsAuthenticated = context.User.Identity?.IsAuthenticated ?? false
        };
    }

    /// <summary>
    /// Builds a query string from the supplied name-value pairs.
    /// </summary>
    /// <param name="parameters">The query string parameters.</param>
    /// <returns>The generated query string, including the leading question mark when parameters exist.</returns>
    public static string BuildQueryString(IEnumerable<KeyValuePair<string, string?>> parameters)
    {
        ArgumentNullException.ThrowIfNull(parameters);

        var dictionary = parameters
            .Where(parameter => !string.IsNullOrWhiteSpace(parameter.Key) && parameter.Value is not null)
            .ToDictionary<KeyValuePair<string, string?>, string, string?>(parameter => parameter.Key, parameter => parameter.Value);

        return QueryHelpers.AddQueryString(string.Empty, dictionary);
    }

    /// <summary>
    /// Appends query string parameters to a URL.
    /// </summary>
    /// <param name="url">The base URL.</param>
    /// <param name="parameters">The query string parameters.</param>
    /// <returns>The URL with appended query string parameters.</returns>
    public static string AppendQueryString(string url, IEnumerable<KeyValuePair<string, string?>> parameters)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(url);
        ArgumentNullException.ThrowIfNull(parameters);

        var dictionary = parameters
            .Where(parameter => !string.IsNullOrWhiteSpace(parameter.Key) && parameter.Value is not null)
            .ToDictionary<KeyValuePair<string, string?>, string, string?>(parameter => parameter.Key, parameter => parameter.Value);

        return QueryHelpers.AddQueryString(url, dictionary);
    }

    private static string? GetHeaderValue(IHeaderDictionary headers, string headerName)
    {
        return headers.TryGetValue(headerName, out var values)
            ? values.ToString()
            : null;
    }
}