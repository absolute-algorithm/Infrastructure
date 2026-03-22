using System;

namespace FileFormula.Api.Infrastructure.Constraints;


/// <summary>
/// Defines HTTP header names used by the library.
/// </summary>
public static class HEADER
{
    /// <summary>
    /// Gets the header name used to carry the correlation identifier.
    /// </summary>
    public const string CORRELATIONID = "x-correlation-id";

    /// <summary>
    /// Gets the normalized user-agent header name written by the library.
    /// </summary>
    public const string USERAGENT = "x-user-agent";

    /// <summary>
    /// Gets the normalized client IP address header name written by the library.
    /// </summary>
    public const string IPADDRESS = "x-ip-address";

    /// <summary>
    /// Gets the incoming request header name used to read the original user-agent value.
    /// </summary>
    public const string REQUESTUSERAGENT = "User-Agent";

    /// <summary>
    /// Gets the header name used to carry the tenant identifier.
    /// </summary>
    public const string TENANTID = "x-tenant-id";

    /// <summary>
    /// Gets the header name used to carry an idempotency key.
    /// </summary>
    public const string IDEMPOTENCYKEY = "x-idempotency-key";

    /// <summary>
    /// Gets the header name used to carry an API key.
    /// </summary>
    public const string APIKEY = "x-api-key";

    /// <summary>
    /// Gets the header name used to carry an authentication key.
    /// </summary>
    public const string AUTHKEY = "x-auth-key";

    /// <summary>
    /// Gets the response header name that indicates a cached idempotent response was replayed.
    /// </summary>
    public const string IDEMPOTENCYREPLAYED = "x-idempotency-replayed";

    /// <summary>
    /// Gets the header name used to send the antiforgery request token for cookie-authenticated requests.
    /// </summary>
    public const string CSRFTOKEN = "x-csrf-token";

    /// <summary>
    /// Gets the header name used to carry a webhook or request signature.
    /// </summary>
    public const string REQUESTSIGNATURE = "x-signature";

    /// <summary>
    /// Gets the header name used to carry the request signature timestamp.
    /// </summary>
    public const string REQUESTSIGNATURETIMESTAMP = "x-signature-timestamp";

    /// <summary>
    /// Gets the ETag response header name.
    /// </summary>
    public const string ETAG = "ETag";

    /// <summary>
    /// Gets the conditional request header used to require a matching entity tag.
    /// </summary>
    public const string IFMATCH = "If-Match";

    /// <summary>
    /// Gets the conditional request header used to check whether an entity tag has changed.
    /// </summary>
    public const string IFNONEMATCH = "If-None-Match";
}
