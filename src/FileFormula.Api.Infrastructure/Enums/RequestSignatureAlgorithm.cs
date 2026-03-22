namespace FileFormula.Api.Infrastructure.Enums;

/// <summary>
/// Specifies the keyed hashing algorithm used for request signatures.
/// </summary>
public enum RequestSignatureAlgorithm : byte
{
    /// <summary>
    /// Uses HMAC-SHA256.
    /// </summary>
    HmacSha256 = 1,

    /// <summary>
    /// Uses HMAC-SHA512.
    /// </summary>
    HmacSha512 = 2
}