using System.Security.Cryptography;
using System.Text;
using FileFormula.Api.Infrastructure.Enums;

namespace FileFormula.Api.Infrastructure.Utilities;

/// <summary>
/// Provides helper methods for generating and verifying signed webhook requests.
/// </summary>
public static class RequestSignatureUtility
{
    /// <summary>
    /// Generates a Unix timestamp string for request-signing flows.
    /// </summary>
    /// <param name="utcNow">The current UTC time. When <see langword="null"/>, the current system time is used.</param>
    /// <returns>The Unix timestamp string.</returns>
    public static string GenerateTimestamp(DateTimeOffset? utcNow = null)
    {
        return (utcNow ?? DateTimeOffset.UtcNow).ToUnixTimeSeconds().ToString();
    }

    /// <summary>
    /// Computes a keyed request signature.
    /// </summary>
    /// <param name="payload">The request body payload.</param>
    /// <param name="timestamp">The associated request timestamp.</param>
    /// <param name="secret">The shared secret.</param>
    /// <param name="algorithm">The keyed hashing algorithm.</param>
    /// <param name="encoding">The text encoding. When <see langword="null"/>, UTF-8 is used.</param>
    /// <returns>The lowercase hexadecimal signature.</returns>
    public static string ComputeSignature(
        string payload,
        string timestamp,
        string secret,
        RequestSignatureAlgorithm algorithm = RequestSignatureAlgorithm.HmacSha256,
        Encoding? encoding = null)
    {
        ArgumentNullException.ThrowIfNull(payload);
        ArgumentException.ThrowIfNullOrWhiteSpace(timestamp);
        ArgumentException.ThrowIfNullOrWhiteSpace(secret);

        var textEncoding = encoding ?? Encoding.UTF8;
        var secretBytes = textEncoding.GetBytes(secret);
        var payloadBytes = textEncoding.GetBytes($"{timestamp}.{payload}");
        var hash = algorithm switch
        {
            RequestSignatureAlgorithm.HmacSha256 => HMACSHA256.HashData(secretBytes, payloadBytes),
            RequestSignatureAlgorithm.HmacSha512 => HMACSHA512.HashData(secretBytes, payloadBytes),
            _ => throw new ArgumentOutOfRangeException(nameof(algorithm), algorithm, "Unsupported request signature algorithm.")
        };

        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    /// <summary>
    /// Verifies a signed request and checks the timestamp freshness.
    /// </summary>
    /// <param name="payload">The request body payload.</param>
    /// <param name="timestamp">The associated request timestamp.</param>
    /// <param name="providedSignature">The received signature.</param>
    /// <param name="secret">The shared secret.</param>
    /// <param name="algorithm">The keyed hashing algorithm.</param>
    /// <param name="allowedClockSkewSeconds">The allowed timestamp skew, in seconds.</param>
    /// <param name="utcNow">The current UTC time. When <see langword="null"/>, the current system time is used.</param>
    /// <param name="encoding">The text encoding. When <see langword="null"/>, UTF-8 is used.</param>
    /// <returns><see langword="true"/> when the signature is valid and within the allowed time window; otherwise, <see langword="false"/>.</returns>
    public static bool VerifySignature(
        string payload,
        string timestamp,
        string providedSignature,
        string secret,
        RequestSignatureAlgorithm algorithm = RequestSignatureAlgorithm.HmacSha256,
        int allowedClockSkewSeconds = 300,
        DateTimeOffset? utcNow = null,
        Encoding? encoding = null)
    {
        ArgumentNullException.ThrowIfNull(payload);
        ArgumentException.ThrowIfNullOrWhiteSpace(timestamp);
        ArgumentException.ThrowIfNullOrWhiteSpace(providedSignature);
        ArgumentException.ThrowIfNullOrWhiteSpace(secret);

        if (allowedClockSkewSeconds < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(allowedClockSkewSeconds), allowedClockSkewSeconds, "Allowed clock skew must be zero or greater.");
        }

        if (!long.TryParse(timestamp, out var unixTimeSeconds))
        {
            return false;
        }

        var currentUtc = utcNow ?? DateTimeOffset.UtcNow;
        var signedAt = DateTimeOffset.FromUnixTimeSeconds(unixTimeSeconds);
        if (Math.Abs((currentUtc - signedAt).TotalSeconds) > allowedClockSkewSeconds)
        {
            return false;
        }

        var expectedSignature = ComputeSignature(payload, timestamp, secret, algorithm, encoding);
        return TokenUtility.FixedTimeEquals(expectedSignature, providedSignature);
    }
}