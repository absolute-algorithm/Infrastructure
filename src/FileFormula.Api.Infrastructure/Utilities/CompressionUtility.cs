using System.IO.Compression;
using System.Text;

namespace FileFormula.Api.Infrastructure.Utilities;

/// <summary>
/// Provides helper methods for compressing and decompressing data.
/// </summary>
public static class CompressionUtility
{
    /// <summary>
    /// Compresses a string using GZip and returns the compressed bytes.
    /// </summary>
    /// <param name="value">The text to compress.</param>
    /// <param name="encoding">The text encoding to use. When <see langword="null"/>, UTF-8 is used.</param>
    /// <param name="compressionLevel">The compression level.</param>
    /// <returns>The compressed bytes.</returns>
    public static byte[] CompressGzip(string value, Encoding? encoding = null, CompressionLevel compressionLevel = CompressionLevel.Optimal)
    {
        ArgumentNullException.ThrowIfNull(value);

        return CompressGzip((encoding ?? Encoding.UTF8).GetBytes(value), compressionLevel);
    }

    /// <summary>
    /// Compresses bytes using GZip.
    /// </summary>
    /// <param name="value">The bytes to compress.</param>
    /// <param name="compressionLevel">The compression level.</param>
    /// <returns>The compressed bytes.</returns>
    public static byte[] CompressGzip(byte[] value, CompressionLevel compressionLevel = CompressionLevel.Optimal)
    {
        ArgumentNullException.ThrowIfNull(value);

        using var output = new MemoryStream();
        using (var gzip = new GZipStream(output, compressionLevel, leaveOpen: true))
        {
            gzip.Write(value, 0, value.Length);
        }

        return output.ToArray();
    }

    /// <summary>
    /// Decompresses GZip bytes into a string.
    /// </summary>
    /// <param name="compressedValue">The compressed bytes.</param>
    /// <param name="encoding">The text encoding to use. When <see langword="null"/>, UTF-8 is used.</param>
    /// <returns>The decompressed string.</returns>
    public static string DecompressGzipToString(byte[] compressedValue, Encoding? encoding = null)
    {
        return (encoding ?? Encoding.UTF8).GetString(DecompressGzip(compressedValue));
    }

    /// <summary>
    /// Decompresses GZip bytes.
    /// </summary>
    /// <param name="compressedValue">The compressed bytes.</param>
    /// <returns>The decompressed bytes.</returns>
    public static byte[] DecompressGzip(byte[] compressedValue)
    {
        ArgumentNullException.ThrowIfNull(compressedValue);

        using var input = new MemoryStream(compressedValue);
        using var gzip = new GZipStream(input, CompressionMode.Decompress);
        using var output = new MemoryStream();
        gzip.CopyTo(output);

        return output.ToArray();
    }

    /// <summary>
    /// Compresses a string using Brotli and returns the compressed bytes.
    /// </summary>
    /// <param name="value">The text to compress.</param>
    /// <param name="encoding">The text encoding to use. When <see langword="null"/>, UTF-8 is used.</param>
    /// <param name="compressionLevel">The compression level.</param>
    /// <returns>The compressed bytes.</returns>
    public static byte[] CompressBrotli(string value, Encoding? encoding = null, CompressionLevel compressionLevel = CompressionLevel.Optimal)
    {
        ArgumentNullException.ThrowIfNull(value);

        return CompressBrotli((encoding ?? Encoding.UTF8).GetBytes(value), compressionLevel);
    }

    /// <summary>
    /// Compresses bytes using Brotli.
    /// </summary>
    /// <param name="value">The bytes to compress.</param>
    /// <param name="compressionLevel">The compression level.</param>
    /// <returns>The compressed bytes.</returns>
    public static byte[] CompressBrotli(byte[] value, CompressionLevel compressionLevel = CompressionLevel.Optimal)
    {
        ArgumentNullException.ThrowIfNull(value);

        using var output = new MemoryStream();
        using (var brotli = new BrotliStream(output, compressionLevel, leaveOpen: true))
        {
            brotli.Write(value, 0, value.Length);
        }

        return output.ToArray();
    }

    /// <summary>
    /// Decompresses Brotli bytes into a string.
    /// </summary>
    /// <param name="compressedValue">The compressed bytes.</param>
    /// <param name="encoding">The text encoding to use. When <see langword="null"/>, UTF-8 is used.</param>
    /// <returns>The decompressed string.</returns>
    public static string DecompressBrotliToString(byte[] compressedValue, Encoding? encoding = null)
    {
        return (encoding ?? Encoding.UTF8).GetString(DecompressBrotli(compressedValue));
    }

    /// <summary>
    /// Decompresses Brotli bytes.
    /// </summary>
    /// <param name="compressedValue">The compressed bytes.</param>
    /// <returns>The decompressed bytes.</returns>
    public static byte[] DecompressBrotli(byte[] compressedValue)
    {
        ArgumentNullException.ThrowIfNull(compressedValue);

        using var input = new MemoryStream(compressedValue);
        using var brotli = new BrotliStream(input, CompressionMode.Decompress);
        using var output = new MemoryStream();
        brotli.CopyTo(output);

        return output.ToArray();
    }
}