using System.Text;
using FileFormula.Api.Infrastructure.Constraints;
using Microsoft.AspNetCore.Http;

namespace FileFormula.Api.Infrastructure.Utilities;

/// <summary>
/// Provides helper methods for generating and evaluating HTTP entity tags.
/// </summary>
public static class ETagUtility
{
    /// <summary>
    /// Creates a strong ETag for the supplied text value.
    /// </summary>
    /// <param name="value">The value to hash.</param>
    /// <param name="encoding">The text encoding. When <see langword="null"/>, UTF-8 is used.</param>
    /// <returns>The quoted strong ETag value.</returns>
    public static string CreateStrong(string value, Encoding? encoding = null)
    {
        ArgumentNullException.ThrowIfNull(value);

        return Quote(HashingUtility.ComputeSha256(value, encoding));
    }

    /// <summary>
    /// Creates a strong ETag for the supplied byte array.
    /// </summary>
    /// <param name="value">The bytes to hash.</param>
    /// <returns>The quoted strong ETag value.</returns>
    public static string CreateStrong(byte[] value)
    {
        ArgumentNullException.ThrowIfNull(value);

        return Quote(HashingUtility.ComputeSha256(value));
    }

    /// <summary>
    /// Creates a strong ETag for the supplied object by serializing it to JSON.
    /// </summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="value">The value to hash.</param>
    /// <returns>The quoted strong ETag value.</returns>
    public static string CreateStrong<T>(T value)
    {
        ArgumentNullException.ThrowIfNull(value);

        return CreateStrong(JsonUtility.Serialize(value), encoding: null);
    }

    /// <summary>
    /// Creates a weak ETag for the supplied text value.
    /// </summary>
    /// <param name="value">The value to hash.</param>
    /// <param name="encoding">The text encoding. When <see langword="null"/>, UTF-8 is used.</param>
    /// <returns>The weak ETag value.</returns>
    public static string CreateWeak(string value, Encoding? encoding = null)
    {
        return ToWeak(CreateStrong(value, encoding));
    }

    /// <summary>
    /// Creates a weak ETag for the supplied object by serializing it to JSON.
    /// </summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="value">The value to hash.</param>
    /// <returns>The weak ETag value.</returns>
    public static string CreateWeak<T>(T value)
    {
        ArgumentNullException.ThrowIfNull(value);

        return ToWeak(CreateStrong(JsonUtility.Serialize(value), encoding: null));
    }

    /// <summary>
    /// Applies an ETag value to the response.
    /// </summary>
    /// <param name="response">The HTTP response.</param>
    /// <param name="etag">The ETag value.</param>
    public static void Apply(HttpResponse response, string etag)
    {
        ArgumentNullException.ThrowIfNull(response);
        ArgumentException.ThrowIfNullOrWhiteSpace(etag);

        response.Headers[HEADER.ETAG] = Normalize(etag);
    }

    /// <summary>
    /// Gets the If-Match header value for the current request.
    /// </summary>
    /// <param name="request">The HTTP request.</param>
    /// <returns>The raw If-Match header value when present; otherwise, <see langword="null"/>.</returns>
    public static string? GetIfMatch(HttpRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        return HttpUtility.GetHeaderValue(request, HEADER.IFMATCH);
    }

    /// <summary>
    /// Gets the If-None-Match header value for the current request.
    /// </summary>
    /// <param name="request">The HTTP request.</param>
    /// <returns>The raw If-None-Match header value when present; otherwise, <see langword="null"/>.</returns>
    public static string? GetIfNoneMatch(HttpRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        return HttpUtility.GetHeaderValue(request, HEADER.IFNONEMATCH);
    }

    /// <summary>
    /// Determines whether an If-Match or If-None-Match style header contains the wildcard token.
    /// </summary>
    /// <param name="headerValue">The raw header value.</param>
    /// <returns><see langword="true"/> when the wildcard token is present; otherwise, <see langword="false"/>.</returns>
    public static bool IsWildcard(string? headerValue)
    {
        return string.Equals(headerValue?.Trim(), "*", StringComparison.Ordinal);
    }

    /// <summary>
    /// Determines whether an ETag candidate matches the current ETag.
    /// </summary>
    /// <param name="currentEtag">The current ETag.</param>
    /// <param name="candidateEtag">The candidate ETag.</param>
    /// <param name="allowWeakComparison"><see langword="true"/> to ignore the weak/strong prefix during comparison; otherwise, <see langword="false"/>.</param>
    /// <returns><see langword="true"/> when the entity tags match; otherwise, <see langword="false"/>.</returns>
    public static bool Matches(string currentEtag, string candidateEtag, bool allowWeakComparison = false)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(currentEtag);
        ArgumentException.ThrowIfNullOrWhiteSpace(candidateEtag);

        var normalizedCurrent = Normalize(currentEtag);
        var normalizedCandidate = Normalize(candidateEtag);

        if (!allowWeakComparison)
        {
            return string.Equals(normalizedCurrent, normalizedCandidate, StringComparison.Ordinal);
        }

        return string.Equals(RemoveWeakPrefix(normalizedCurrent), RemoveWeakPrefix(normalizedCandidate), StringComparison.Ordinal);
    }

    /// <summary>
    /// Determines whether a comma-separated ETag header matches the current ETag.
    /// </summary>
    /// <param name="currentEtag">The current ETag.</param>
    /// <param name="headerValue">The raw header value.</param>
    /// <param name="allowWeakComparison"><see langword="true"/> to ignore the weak/strong prefix during comparison; otherwise, <see langword="false"/>.</param>
    /// <returns><see langword="true"/> when any candidate matches; otherwise, <see langword="false"/>.</returns>
    public static bool AnyMatch(string currentEtag, string? headerValue, bool allowWeakComparison = false)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(currentEtag);

        if (string.IsNullOrWhiteSpace(headerValue))
        {
            return false;
        }

        if (IsWildcard(headerValue))
        {
            return true;
        }

        return ParseHeaderValues(headerValue).Any(candidate => Matches(currentEtag, candidate, allowWeakComparison));
    }

    /// <summary>
    /// Normalizes an ETag value into its quoted form.
    /// </summary>
    /// <param name="etag">The ETag value.</param>
    /// <returns>The normalized ETag value.</returns>
    public static string Normalize(string etag)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(etag);

        var trimmed = etag.Trim();
        if (trimmed.StartsWith("W/\"", StringComparison.Ordinal) && trimmed.EndsWith('"'))
        {
            return trimmed;
        }

        if (trimmed.StartsWith('"') && trimmed.EndsWith('"'))
        {
            return trimmed;
        }

        if (trimmed.StartsWith("W/", StringComparison.Ordinal))
        {
            return $"W/{Quote(trimmed[2..].Trim('"'))}";
        }

        return Quote(trimmed.Trim('"'));
    }

    /// <summary>
    /// Converts a strong ETag into its weak form.
    /// </summary>
    /// <param name="etag">The ETag value.</param>
    /// <returns>The weak ETag value.</returns>
    public static string ToWeak(string etag)
    {
        var normalized = Normalize(etag);
        return normalized.StartsWith("W/", StringComparison.Ordinal) ? normalized : $"W/{normalized}";
    }

    private static IEnumerable<string> ParseHeaderValues(string headerValue)
    {
        return headerValue.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    }

    private static string RemoveWeakPrefix(string etag)
    {
        return etag.StartsWith("W/", StringComparison.Ordinal) ? etag[2..] : etag;
    }

    private static string Quote(string value)
    {
        return $"\"{value.Trim('"')}\"";
    }
}