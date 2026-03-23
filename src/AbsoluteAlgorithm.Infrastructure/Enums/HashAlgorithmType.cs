namespace AbsoluteAlgorithm.Infrastructure.Enums;

/// <summary>
/// Defines the supported hashing algorithms exposed by the library.
/// </summary>
public enum HashAlgorithmType
{
    /// <summary>
    /// Computes an MD5 hash. This algorithm is retained for compatibility scenarios and should not be used for security-sensitive workloads.
    /// </summary>
    Md5,

    /// <summary>
    /// Computes a SHA-1 hash. This algorithm is retained for compatibility scenarios and should not be used for security-sensitive workloads.
    /// </summary>
    Sha1,

    /// <summary>
    /// Computes a SHA-256 hash.
    /// </summary>
    Sha256,

    /// <summary>
    /// Computes a SHA-384 hash.
    /// </summary>
    Sha384,

    /// <summary>
    /// Computes a SHA-512 hash.
    /// </summary>
    Sha512
}