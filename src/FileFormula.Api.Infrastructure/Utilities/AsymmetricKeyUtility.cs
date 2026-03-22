using System.Security.Cryptography;
using System.Text;
using FileFormula.Api.Infrastructure.Enums;

namespace FileFormula.Api.Infrastructure.Utilities;

/// <summary>
/// Provides helper methods for generating asymmetric keys and creating digital signatures.
/// </summary>
/// <remarks>
/// The private key remains on the server and is used to sign data. The public key can be shared with external callers and is used to verify signatures.
/// </remarks>
public static class AsymmetricKeyUtility
{
    /// <summary>
    /// Generates a private key in PEM format.
    /// </summary>
    /// <param name="algorithm">The asymmetric key algorithm to use.</param>
    /// <param name="rsaKeySize">The RSA key size in bits when <paramref name="algorithm"/> is <see cref="AsymmetricKeyAlgorithmType.Rsa"/>.</param>
    /// <returns>The generated private key in PEM format.</returns>
    public static string GeneratePrivateKey(AsymmetricKeyAlgorithmType algorithm = AsymmetricKeyAlgorithmType.ECDsa, int rsaKeySize = 2048)
    {
        return algorithm switch
        {
            AsymmetricKeyAlgorithmType.Rsa => GenerateRsaPrivateKey(rsaKeySize),
            AsymmetricKeyAlgorithmType.ECDsa => GenerateEcdsaPrivateKey(),
            _ => throw new ArgumentOutOfRangeException(nameof(algorithm), algorithm, "Unsupported asymmetric key algorithm.")
        };
    }

    /// <summary>
    /// Derives the public key in PEM format from the supplied private key.
    /// </summary>
    /// <param name="privateKeyPem">The private key in PEM format.</param>
    /// <param name="algorithm">The asymmetric key algorithm used by the private key.</param>
    /// <returns>The derived public key in PEM format.</returns>
    public static string GetPublicKey(string privateKeyPem, AsymmetricKeyAlgorithmType algorithm = AsymmetricKeyAlgorithmType.ECDsa)
    {
        ArgumentNullException.ThrowIfNull(privateKeyPem);

        return algorithm switch
        {
            AsymmetricKeyAlgorithmType.Rsa => GetRsaPublicKey(privateKeyPem),
            AsymmetricKeyAlgorithmType.ECDsa => GetEcdsaPublicKey(privateKeyPem),
            _ => throw new ArgumentOutOfRangeException(nameof(algorithm), algorithm, "Unsupported asymmetric key algorithm.")
        };
    }

    /// <summary>
    /// Generates a public and private key pair in PEM format.
    /// </summary>
    /// <param name="algorithm">The asymmetric key algorithm to use.</param>
    /// <param name="rsaKeySize">The RSA key size in bits when <paramref name="algorithm"/> is <see cref="AsymmetricKeyAlgorithmType.Rsa"/>.</param>
    /// <returns>The generated key pair.</returns>
    public static AsymmetricKeyPair GenerateKeyPair(AsymmetricKeyAlgorithmType algorithm = AsymmetricKeyAlgorithmType.ECDsa, int rsaKeySize = 2048)
    {
        var privateKeyPem = GeneratePrivateKey(algorithm, rsaKeySize);
        var publicKeyPem = GetPublicKey(privateKeyPem, algorithm);

        return new AsymmetricKeyPair(publicKeyPem, privateKeyPem);
    }

    /// <summary>
    /// Signs the specified text and returns the signature as a base64 string.
    /// </summary>
    /// <param name="value">The text to sign.</param>
    /// <param name="privateKeyPem">The private key in PEM format.</param>
    /// <param name="algorithm">The asymmetric key algorithm used by the private key.</param>
    /// <param name="hashAlgorithm">The hash algorithm applied before signing.</param>
    /// <param name="encoding">The text encoding to use. When <see langword="null"/>, UTF-8 is used.</param>
    /// <returns>The signature as a base64 string.</returns>
    public static string SignData(
        string value,
        string privateKeyPem,
        AsymmetricKeyAlgorithmType algorithm = AsymmetricKeyAlgorithmType.ECDsa,
        HashAlgorithmType hashAlgorithm = HashAlgorithmType.Sha256,
        Encoding? encoding = null)
    {
        ArgumentNullException.ThrowIfNull(value);
        ArgumentNullException.ThrowIfNull(privateKeyPem);

        var data = (encoding ?? Encoding.UTF8).GetBytes(value);
        var signature = SignData(data, privateKeyPem, algorithm, hashAlgorithm);

        return Convert.ToBase64String(signature);
    }

    /// <summary>
    /// Signs the specified bytes and returns the raw signature bytes.
    /// </summary>
    /// <param name="data">The bytes to sign.</param>
    /// <param name="privateKeyPem">The private key in PEM format.</param>
    /// <param name="algorithm">The asymmetric key algorithm used by the private key.</param>
    /// <param name="hashAlgorithm">The hash algorithm applied before signing.</param>
    /// <returns>The signature bytes.</returns>
    public static byte[] SignData(
        byte[] data,
        string privateKeyPem,
        AsymmetricKeyAlgorithmType algorithm = AsymmetricKeyAlgorithmType.ECDsa,
        HashAlgorithmType hashAlgorithm = HashAlgorithmType.Sha256)
    {
        ArgumentNullException.ThrowIfNull(data);
        ArgumentNullException.ThrowIfNull(privateKeyPem);

        var hashName = MapHashAlgorithm(hashAlgorithm);

        return algorithm switch
        {
            AsymmetricKeyAlgorithmType.Rsa => SignRsa(data, privateKeyPem, hashName),
            AsymmetricKeyAlgorithmType.ECDsa => SignEcdsa(data, privateKeyPem, hashName),
            _ => throw new ArgumentOutOfRangeException(nameof(algorithm), algorithm, "Unsupported asymmetric key algorithm.")
        };
    }

    /// <summary>
    /// Verifies a base64 signature against the specified text using the public key.
    /// </summary>
    /// <param name="value">The original text.</param>
    /// <param name="signature">The base64-encoded signature.</param>
    /// <param name="publicKeyPem">The public key in PEM format.</param>
    /// <param name="algorithm">The asymmetric key algorithm used by the public key.</param>
    /// <param name="hashAlgorithm">The hash algorithm applied before verification.</param>
    /// <param name="encoding">The text encoding to use. When <see langword="null"/>, UTF-8 is used.</param>
    /// <returns><see langword="true"/> when the signature is valid; otherwise, <see langword="false"/>.</returns>
    public static bool VerifySignature(
        string value,
        string signature,
        string publicKeyPem,
        AsymmetricKeyAlgorithmType algorithm = AsymmetricKeyAlgorithmType.ECDsa,
        HashAlgorithmType hashAlgorithm = HashAlgorithmType.Sha256,
        Encoding? encoding = null)
    {
        ArgumentNullException.ThrowIfNull(value);
        ArgumentNullException.ThrowIfNull(signature);
        ArgumentNullException.ThrowIfNull(publicKeyPem);

        var data = (encoding ?? Encoding.UTF8).GetBytes(value);
        var signatureBytes = Convert.FromBase64String(signature);

        return VerifySignature(data, signatureBytes, publicKeyPem, algorithm, hashAlgorithm);
    }

    /// <summary>
    /// Verifies a signature against the specified bytes using the public key.
    /// </summary>
    /// <param name="data">The original bytes.</param>
    /// <param name="signature">The signature bytes.</param>
    /// <param name="publicKeyPem">The public key in PEM format.</param>
    /// <param name="algorithm">The asymmetric key algorithm used by the public key.</param>
    /// <param name="hashAlgorithm">The hash algorithm applied before verification.</param>
    /// <returns><see langword="true"/> when the signature is valid; otherwise, <see langword="false"/>.</returns>
    public static bool VerifySignature(
        byte[] data,
        byte[] signature,
        string publicKeyPem,
        AsymmetricKeyAlgorithmType algorithm = AsymmetricKeyAlgorithmType.ECDsa,
        HashAlgorithmType hashAlgorithm = HashAlgorithmType.Sha256)
    {
        ArgumentNullException.ThrowIfNull(data);
        ArgumentNullException.ThrowIfNull(signature);
        ArgumentNullException.ThrowIfNull(publicKeyPem);

        var hashName = MapHashAlgorithm(hashAlgorithm);

        return algorithm switch
        {
            AsymmetricKeyAlgorithmType.Rsa => VerifyRsa(data, signature, publicKeyPem, hashName),
            AsymmetricKeyAlgorithmType.ECDsa => VerifyEcdsa(data, signature, publicKeyPem, hashName),
            _ => throw new ArgumentOutOfRangeException(nameof(algorithm), algorithm, "Unsupported asymmetric key algorithm.")
        };
    }

    private static string GenerateRsaPrivateKey(int keySize)
    {
        if (keySize < 2048)
        {
            throw new ArgumentOutOfRangeException(nameof(keySize), keySize, "RSA key size must be at least 2048 bits.");
        }

        using var rsa = RSA.Create(keySize);

        return rsa.ExportPkcs8PrivateKeyPem();
    }

    private static string GenerateEcdsaPrivateKey()
    {
        using var ecdsa = ECDsa.Create(ECCurve.NamedCurves.nistP256);

        return ecdsa.ExportPkcs8PrivateKeyPem();
    }

    private static string GetRsaPublicKey(string privateKeyPem)
    {
        using var rsa = RSA.Create();
        rsa.ImportFromPem(privateKeyPem);

        return rsa.ExportSubjectPublicKeyInfoPem();
    }

    private static string GetEcdsaPublicKey(string privateKeyPem)
    {
        using var ecdsa = ECDsa.Create();
        ecdsa.ImportFromPem(privateKeyPem);

        return ecdsa.ExportSubjectPublicKeyInfoPem();
    }

    private static byte[] SignRsa(byte[] data, string privateKeyPem, HashAlgorithmName hashAlgorithm)
    {
        using var rsa = RSA.Create();
        rsa.ImportFromPem(privateKeyPem);

        return rsa.SignData(data, hashAlgorithm, RSASignaturePadding.Pkcs1);
    }

    private static byte[] SignEcdsa(byte[] data, string privateKeyPem, HashAlgorithmName hashAlgorithm)
    {
        using var ecdsa = ECDsa.Create();
        ecdsa.ImportFromPem(privateKeyPem);

        return ecdsa.SignData(data, hashAlgorithm);
    }

    private static bool VerifyRsa(byte[] data, byte[] signature, string publicKeyPem, HashAlgorithmName hashAlgorithm)
    {
        using var rsa = RSA.Create();
        rsa.ImportFromPem(publicKeyPem);

        return rsa.VerifyData(data, signature, hashAlgorithm, RSASignaturePadding.Pkcs1);
    }

    private static bool VerifyEcdsa(byte[] data, byte[] signature, string publicKeyPem, HashAlgorithmName hashAlgorithm)
    {
        using var ecdsa = ECDsa.Create();
        ecdsa.ImportFromPem(publicKeyPem);

        return ecdsa.VerifyData(data, signature, hashAlgorithm);
    }

    private static HashAlgorithmName MapHashAlgorithm(HashAlgorithmType algorithm)
    {
        return algorithm switch
        {
            HashAlgorithmType.Sha256 => HashAlgorithmName.SHA256,
            HashAlgorithmType.Sha384 => HashAlgorithmName.SHA384,
            HashAlgorithmType.Sha512 => HashAlgorithmName.SHA512,
            HashAlgorithmType.Md5 => HashAlgorithmName.MD5,
            HashAlgorithmType.Sha1 => HashAlgorithmName.SHA1,
            _ => throw new ArgumentOutOfRangeException(nameof(algorithm), algorithm, "Unsupported signature hash algorithm.")
        };
    }
}

/// <summary>
/// Represents an asymmetric public and private key pair in PEM format.
/// </summary>
/// <param name="PublicKeyPem">The public key in PEM format.</param>
/// <param name="PrivateKeyPem">The private key in PEM format.</param>
public sealed record AsymmetricKeyPair(string PublicKeyPem, string PrivateKeyPem);