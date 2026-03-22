using System.Security.Cryptography;
using System.Text;

namespace FileFormula.Api.Infrastructure.Utilities;

/// <summary>
/// Provides helper methods for symmetric encryption and decryption.
/// </summary>
/// <remarks>
/// This utility uses AES-GCM authenticated encryption. The supplied key must remain private and should be generated with <see cref="GenerateKey"/>.
/// </remarks>
public static class EncryptionUtility
{
    private const int NonceSize = 12;
    private const int TagSize = 16;

    /// <summary>
    /// Generates a base64-encoded AES key.
    /// </summary>
    /// <param name="sizeInBytes">The key size in bytes. Supported values are 16, 24, and 32.</param>
    /// <returns>A base64-encoded AES key.</returns>
    public static string GenerateKey(int sizeInBytes = 32)
    {
        if (sizeInBytes is not (16 or 24 or 32))
        {
            throw new ArgumentOutOfRangeException(nameof(sizeInBytes), sizeInBytes, "AES key sizes must be 16, 24, or 32 bytes.");
        }

        return Convert.ToBase64String(RandomNumberGenerator.GetBytes(sizeInBytes));
    }

    /// <summary>
    /// Encrypts the specified plaintext and returns a base64-encoded payload.
    /// </summary>
    /// <param name="plainText">The plaintext to encrypt.</param>
    /// <param name="base64Key">The base64-encoded AES key.</param>
    /// <param name="encoding">The text encoding to use. When <see langword="null"/>, UTF-8 is used.</param>
    /// <returns>A base64-encoded encrypted payload.</returns>
    public static string Encrypt(string plainText, string base64Key, Encoding? encoding = null)
    {
        ArgumentNullException.ThrowIfNull(plainText);

        var bytes = (encoding ?? Encoding.UTF8).GetBytes(plainText);
        return Convert.ToBase64String(Encrypt(bytes, base64Key));
    }

    /// <summary>
    /// Decrypts the specified base64-encoded payload.
    /// </summary>
    /// <param name="encryptedPayload">The base64-encoded encrypted payload.</param>
    /// <param name="base64Key">The base64-encoded AES key.</param>
    /// <param name="encoding">The text encoding to use. When <see langword="null"/>, UTF-8 is used.</param>
    /// <returns>The decrypted plaintext.</returns>
    public static string Decrypt(string encryptedPayload, string base64Key, Encoding? encoding = null)
    {
        ArgumentNullException.ThrowIfNull(encryptedPayload);

        var bytes = Convert.FromBase64String(encryptedPayload);
        return (encoding ?? Encoding.UTF8).GetString(Decrypt(bytes, base64Key));
    }

    /// <summary>
    /// Encrypts the specified bytes and returns the raw encrypted payload.
    /// </summary>
    /// <param name="plainBytes">The bytes to encrypt.</param>
    /// <param name="base64Key">The base64-encoded AES key.</param>
    /// <returns>The raw encrypted payload.</returns>
    public static byte[] Encrypt(byte[] plainBytes, string base64Key)
    {
        ArgumentNullException.ThrowIfNull(plainBytes);

        var key = DecodeKey(base64Key);
        var nonce = RandomNumberGenerator.GetBytes(NonceSize);
        var cipherText = new byte[plainBytes.Length];
        var tag = new byte[TagSize];

        using var aes = new AesGcm(key, TagSize);
        aes.Encrypt(nonce, plainBytes, cipherText, tag);

        var payload = new byte[NonceSize + TagSize + cipherText.Length];
        Buffer.BlockCopy(nonce, 0, payload, 0, NonceSize);
        Buffer.BlockCopy(tag, 0, payload, NonceSize, TagSize);
        Buffer.BlockCopy(cipherText, 0, payload, NonceSize + TagSize, cipherText.Length);

        return payload;
    }

    /// <summary>
    /// Decrypts the specified encrypted payload and returns the original bytes.
    /// </summary>
    /// <param name="encryptedPayload">The raw encrypted payload.</param>
    /// <param name="base64Key">The base64-encoded AES key.</param>
    /// <returns>The decrypted bytes.</returns>
    public static byte[] Decrypt(byte[] encryptedPayload, string base64Key)
    {
        ArgumentNullException.ThrowIfNull(encryptedPayload);

        if (encryptedPayload.Length < NonceSize + TagSize)
        {
            throw new ArgumentException("The encrypted payload is invalid.", nameof(encryptedPayload));
        }

        var key = DecodeKey(base64Key);
        var nonce = encryptedPayload[..NonceSize];
        var tag = encryptedPayload[NonceSize..(NonceSize + TagSize)];
        var cipherText = encryptedPayload[(NonceSize + TagSize)..];
        var plainBytes = new byte[cipherText.Length];

        using var aes = new AesGcm(key, TagSize);
        aes.Decrypt(nonce, cipherText, tag, plainBytes);

        return plainBytes;
    }

    private static byte[] DecodeKey(string base64Key)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(base64Key);

        var key = Convert.FromBase64String(base64Key);
        if (key.Length is not (16 or 24 or 32))
        {
            throw new ArgumentException("The AES key must decode to 16, 24, or 32 bytes.", nameof(base64Key));
        }

        return key;
    }
}