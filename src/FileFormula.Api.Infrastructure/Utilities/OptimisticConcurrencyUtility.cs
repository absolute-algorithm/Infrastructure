using FileFormula.Api.Infrastructure.Constraints;
using FileFormula.Api.Infrastructure.Exceptions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.WebUtilities;

namespace FileFormula.Api.Infrastructure.Utilities;

/// <summary>
/// Provides helper methods for optimistic concurrency and conditional request checks.
/// </summary>
public static class OptimisticConcurrencyUtility
{
    /// <summary>
    /// Creates a version token from a database row-version payload.
    /// </summary>
    /// <param name="rowVersion">The row-version bytes.</param>
    /// <returns>The normalized version token.</returns>
    public static string CreateVersionToken(byte[] rowVersion)
    {
        ArgumentNullException.ThrowIfNull(rowVersion);

        return WebEncoders.Base64UrlEncode(rowVersion);
    }

    /// <summary>
    /// Creates a version token from a long version number.
    /// </summary>
    /// <param name="version">The version number.</param>
    /// <returns>The normalized version token.</returns>
    public static string CreateVersionToken(long version)
    {
        return version.ToString(System.Globalization.CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// Creates a version token from a UTC timestamp.
    /// </summary>
    /// <param name="timestampUtc">The UTC timestamp.</param>
    /// <returns>The normalized version token.</returns>
    public static string CreateVersionToken(DateTime timestampUtc)
    {
        return DateTimeUtility.EnsureUtc(timestampUtc).Ticks.ToString(System.Globalization.CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// Creates a strong ETag from the supplied version token.
    /// </summary>
    /// <param name="versionToken">The version token.</param>
    /// <returns>The strong ETag value.</returns>
    public static string CreateETag(string versionToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(versionToken);

        return ETagUtility.Normalize(versionToken);
    }

    /// <summary>
    /// Creates a strong ETag from the supplied row-version bytes.
    /// </summary>
    /// <param name="rowVersion">The row-version bytes.</param>
    /// <returns>The strong ETag value.</returns>
    public static string CreateETag(byte[] rowVersion)
    {
        return CreateETag(CreateVersionToken(rowVersion));
    }

    /// <summary>
    /// Determines whether the supplied version tokens are equal.
    /// </summary>
    /// <param name="currentToken">The current version token.</param>
    /// <param name="expectedToken">The expected version token.</param>
    /// <returns><see langword="true"/> when the versions are equal; otherwise, <see langword="false"/>.</returns>
    public static bool Matches(string currentToken, string expectedToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(currentToken);
        ArgumentException.ThrowIfNullOrWhiteSpace(expectedToken);

        return TokenUtility.FixedTimeEquals(currentToken, expectedToken);
    }

    /// <summary>
    /// Throws when the supplied version tokens do not match.
    /// </summary>
    /// <param name="currentToken">The current version token.</param>
    /// <param name="expectedToken">The expected version token.</param>
    /// <param name="resourceName">The optional resource name.</param>
    public static void EnsureMatches(string currentToken, string expectedToken, string resourceName = "resource")
    {
        if (!Matches(currentToken, expectedToken))
        {
            throw ApiExceptions.Conflict($"The {resourceName} was modified by another request.");
        }
    }

    /// <summary>
    /// Throws when the If-Match header is missing or does not match the current ETag.
    /// </summary>
    /// <param name="request">The HTTP request.</param>
    /// <param name="currentEtag">The current resource ETag.</param>
    /// <param name="resourceName">The optional resource name.</param>
    public static void RequireIfMatch(HttpRequest request, string currentEtag, string resourceName = "resource")
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(currentEtag);

        var ifMatch = ETagUtility.GetIfMatch(request);
        if (string.IsNullOrWhiteSpace(ifMatch))
        {
            throw ApiExceptions.PreconditionFailed($"The {HEADER.IFMATCH} header is required for {resourceName} updates.");
        }

        EnsureIfMatch(ifMatch, currentEtag, resourceName);
    }

    /// <summary>
    /// Throws when the supplied If-Match value does not match the current ETag.
    /// </summary>
    /// <param name="ifMatchHeader">The If-Match header value.</param>
    /// <param name="currentEtag">The current resource ETag.</param>
    /// <param name="resourceName">The optional resource name.</param>
    public static void EnsureIfMatch(string ifMatchHeader, string currentEtag, string resourceName = "resource")
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(ifMatchHeader);
        ArgumentException.ThrowIfNullOrWhiteSpace(currentEtag);

        if (!ETagUtility.AnyMatch(currentEtag, ifMatchHeader, allowWeakComparison: false))
        {
            throw ApiExceptions.PreconditionFailed($"The supplied entity tag does not match the current {resourceName} version.");
        }
    }

    /// <summary>
    /// Determines whether the request should return 304 Not Modified for the current ETag.
    /// </summary>
    /// <param name="request">The HTTP request.</param>
    /// <param name="currentEtag">The current resource ETag.</param>
    /// <returns><see langword="true"/> when the resource has not changed; otherwise, <see langword="false"/>.</returns>
    public static bool ShouldReturnNotModified(HttpRequest request, string currentEtag)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(currentEtag);

        var ifNoneMatch = ETagUtility.GetIfNoneMatch(request);
        return ETagUtility.AnyMatch(currentEtag, ifNoneMatch, allowWeakComparison: true);
    }
}