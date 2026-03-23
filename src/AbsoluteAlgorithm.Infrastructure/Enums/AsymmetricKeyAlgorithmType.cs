namespace AbsoluteAlgorithm.Infrastructure.Enums;

/// <summary>
/// Defines the supported asymmetric key algorithms exposed by the library.
/// </summary>
public enum AsymmetricKeyAlgorithmType
{
    /// <summary>
    /// RSA public and private key pairs.
    /// </summary>
    Rsa,

    /// <summary>
    /// Elliptic Curve Digital Signature Algorithm public and private key pairs.
    /// </summary>
    ECDsa
}