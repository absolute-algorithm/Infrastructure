using System.Security.Cryptography;
using System.Text;
using FileFormula.Api.Infrastructure.Enums;

namespace FileFormula.Api.Infrastructure.Utilities;

/// <summary>
/// Provides helper methods for computing cryptographic hashes.
/// </summary>
public static class HashingUtility
{
    /// <summary>
    /// Computes the hash of the specified text and returns it as a lowercase hexadecimal string.
    /// </summary>
    /// <param name="value">The text to hash.</param>
    /// <param name="algorithm">The hashing algorithm to use.</param>
    /// <param name="encoding">The text encoding to use. When <see langword="null"/>, UTF-8 is used.</param>
    /// <returns>The hash as a lowercase hexadecimal string.</returns>
    /// <remarks>
    /// Prefer SHA-256 or stronger algorithms for security-sensitive workloads. MD5 and SHA-1 are exposed for interoperability only.
    /// </remarks>
    public static string ComputeHash(string value, HashAlgorithmType algorithm = HashAlgorithmType.Sha256, Encoding? encoding = null)
    {
        ArgumentNullException.ThrowIfNull(value);

        var bytes = (encoding ?? Encoding.UTF8).GetBytes(value);
        return ComputeHash(bytes, algorithm);
    }

    /// <summary>
    /// Computes the hash of the specified byte array and returns it as a lowercase hexadecimal string.
    /// </summary>
    /// <param name="value">The bytes to hash.</param>
    /// <param name="algorithm">The hashing algorithm to use.</param>
    /// <returns>The hash as a lowercase hexadecimal string.</returns>
    /// <remarks>
    /// Prefer SHA-256 or stronger algorithms for security-sensitive workloads. MD5 and SHA-1 are exposed for interoperability only.
    /// </remarks>
    public static string ComputeHash(byte[] value, HashAlgorithmType algorithm = HashAlgorithmType.Sha256)
    {
        ArgumentNullException.ThrowIfNull(value);

        var hashBytes = algorithm switch
        {
            HashAlgorithmType.Md5 => MD5.HashData(value),
            HashAlgorithmType.Sha1 => SHA1.HashData(value),
            HashAlgorithmType.Sha256 => SHA256.HashData(value),
            HashAlgorithmType.Sha384 => SHA384.HashData(value),
            HashAlgorithmType.Sha512 => SHA512.HashData(value),
            _ => throw new ArgumentOutOfRangeException(nameof(algorithm), algorithm, "Unsupported hash algorithm.")
        };

        return ConvertToHex(hashBytes);
    }

    /// <summary>
    /// Computes the hash of the specified stream and returns it as a lowercase hexadecimal string.
    /// </summary>
    /// <param name="stream">The stream to hash.</param>
    /// <param name="algorithm">The hashing algorithm to use.</param>
    /// <param name="leaveOpen"><see langword="true"/> to leave the stream open after hashing; otherwise, <see langword="false"/>.</param>
    /// <returns>The hash as a lowercase hexadecimal string.</returns>
    /// <remarks>
    /// Prefer SHA-256 or stronger algorithms for security-sensitive workloads. MD5 and SHA-1 are exposed for interoperability only.
    /// </remarks>
    public static string ComputeHash(Stream stream, HashAlgorithmType algorithm = HashAlgorithmType.Sha256, bool leaveOpen = false)
    {
        ArgumentNullException.ThrowIfNull(stream);

        var originalPosition = stream.CanSeek ? stream.Position : 0;

        try
        {
            if (stream.CanSeek)
            {
                stream.Seek(0, SeekOrigin.Begin);
            }

            using var hashAlgorithm = CreateAlgorithm(algorithm);
            var hashBytes = hashAlgorithm.ComputeHash(stream);
            return ConvertToHex(hashBytes);
        }
        finally
        {
            if (stream.CanSeek)
            {
                stream.Seek(originalPosition, SeekOrigin.Begin);
            }

            if (!leaveOpen)
            {
                stream.Dispose();
            }
        }
    }

    /// <summary>
    /// Computes the hash of a base64-encoded payload and returns it as a lowercase hexadecimal string.
    /// </summary>
    /// <param name="base64Value">The base64-encoded content to hash.</param>
    /// <param name="algorithm">The hashing algorithm to use.</param>
    /// <returns>The hash as a lowercase hexadecimal string.</returns>
    /// <remarks>
    /// Prefer SHA-256 or stronger algorithms for security-sensitive workloads. MD5 and SHA-1 are exposed for interoperability only.
    /// </remarks>
    public static string ComputeHashFromBase64(string base64Value, HashAlgorithmType algorithm = HashAlgorithmType.Sha256)
    {
        ArgumentNullException.ThrowIfNull(base64Value);

        var bytes = Convert.FromBase64String(base64Value);
        return ComputeHash(bytes, algorithm);
    }

    /// <summary>
    /// Computes the SHA-256 hash of the specified text and returns it as a lowercase hexadecimal string.
    /// </summary>
    /// <param name="value">The text to hash.</param>
    /// <param name="encoding">The text encoding to use. When <see langword="null"/>, UTF-8 is used.</param>
    /// <returns>The SHA-256 hash as a lowercase hexadecimal string.</returns>
    public static string ComputeSha256(string value, Encoding? encoding = null)
    {
        return ComputeHash(value, HashAlgorithmType.Sha256, encoding);
    }

    /// <summary>
    /// Computes the SHA-512 hash of the specified text and returns it as a lowercase hexadecimal string.
    /// </summary>
    /// <param name="value">The text to hash.</param>
    /// <param name="encoding">The text encoding to use. When <see langword="null"/>, UTF-8 is used.</param>
    /// <returns>The SHA-512 hash as a lowercase hexadecimal string.</returns>
    public static string ComputeSha512(string value, Encoding? encoding = null)
    {
        return ComputeHash(value, HashAlgorithmType.Sha512, encoding);
    }

    /// <summary>
    /// Computes the SHA-256 hash of the specified byte array and returns it as a lowercase hexadecimal string.
    /// </summary>
    /// <param name="value">The bytes to hash.</param>
    /// <returns>The SHA-256 hash as a lowercase hexadecimal string.</returns>
    public static string ComputeSha256(byte[] value)
    {
        return ComputeHash(value, HashAlgorithmType.Sha256);
    }

    /// <summary>
    /// Computes the SHA-512 hash of the specified byte array and returns it as a lowercase hexadecimal string.
    /// </summary>
    /// <param name="value">The bytes to hash.</param>
    /// <returns>The SHA-512 hash as a lowercase hexadecimal string.</returns>
    public static string ComputeSha512(byte[] value)
    {
        return ComputeHash(value, HashAlgorithmType.Sha512);
    }

    /// <summary>
    /// Computes the SHA-256 hash of the specified stream and returns it as a lowercase hexadecimal string.
    /// </summary>
    /// <param name="stream">The stream to hash.</param>
    /// <param name="leaveOpen"><see langword="true"/> to leave the stream open after hashing; otherwise, <see langword="false"/>.</param>
    /// <returns>The SHA-256 hash as a lowercase hexadecimal string.</returns>
    public static string ComputeSha256(Stream stream, bool leaveOpen = false)
    {
        return ComputeHash(stream, HashAlgorithmType.Sha256, leaveOpen);
    }

    /// <summary>
    /// Computes the SHA-512 hash of the specified stream and returns it as a lowercase hexadecimal string.
    /// </summary>
    /// <param name="stream">The stream to hash.</param>
    /// <param name="leaveOpen"><see langword="true"/> to leave the stream open after hashing; otherwise, <see langword="false"/>.</param>
    /// <returns>The SHA-512 hash as a lowercase hexadecimal string.</returns>
    public static string ComputeSha512(Stream stream, bool leaveOpen = false)
    {
        return ComputeHash(stream, HashAlgorithmType.Sha512, leaveOpen);
    }

    /// <summary>
    /// Computes the SHA-256 hash of a base64-encoded payload and returns it as a lowercase hexadecimal string.
    /// </summary>
    /// <param name="base64Value">The base64-encoded content to hash.</param>
    /// <returns>The SHA-256 hash as a lowercase hexadecimal string.</returns>
    public static string ComputeSha256FromBase64(string base64Value)
    {
        return ComputeHashFromBase64(base64Value, HashAlgorithmType.Sha256);
    }

    /// <summary>
    /// Computes the SHA-512 hash of a base64-encoded payload and returns it as a lowercase hexadecimal string.
    /// </summary>
    /// <param name="base64Value">The base64-encoded content to hash.</param>
    /// <returns>The SHA-512 hash as a lowercase hexadecimal string.</returns>
    public static string ComputeSha512FromBase64(string base64Value)
    {
        return ComputeHashFromBase64(base64Value, HashAlgorithmType.Sha512);
    }

    private static HashAlgorithm CreateAlgorithm(HashAlgorithmType algorithm)
    {
        return algorithm switch
        {
            HashAlgorithmType.Md5 => MD5.Create(),
            HashAlgorithmType.Sha1 => SHA1.Create(),
            HashAlgorithmType.Sha256 => SHA256.Create(),
            HashAlgorithmType.Sha384 => SHA384.Create(),
            HashAlgorithmType.Sha512 => SHA512.Create(),
            _ => throw new ArgumentOutOfRangeException(nameof(algorithm), algorithm, "Unsupported hash algorithm.")
        };
    }

    private static string ConvertToHex(byte[] hashBytes)
    {
        return Convert.ToHexString(hashBytes).ToLowerInvariant();
    }
}