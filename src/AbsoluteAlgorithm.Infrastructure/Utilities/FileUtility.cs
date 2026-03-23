using System.Text;
using Microsoft.AspNetCore.StaticFiles;

namespace AbsoluteAlgorithm.Infrastructure.Utilities;

/// <summary>
/// Provides helper methods for file names, content types, sizes, and stream reads.
/// </summary>
public static class FileUtility
{
    private static readonly FileExtensionContentTypeProvider ContentTypeProvider = new();

    /// <summary>
    /// Gets the file extension from the specified file name.
    /// </summary>
    /// <param name="fileName">The file name.</param>
    /// <returns>The file extension, or an empty string when no extension exists.</returns>
    public static string GetExtension(string? fileName)
    {
        return string.IsNullOrWhiteSpace(fileName) ? string.Empty : Path.GetExtension(fileName);
    }

    /// <summary>
    /// Gets the file name without its extension.
    /// </summary>
    /// <param name="fileName">The file name.</param>
    /// <returns>The file name without its extension.</returns>
    public static string GetFileNameWithoutExtension(string? fileName)
    {
        return string.IsNullOrWhiteSpace(fileName) ? string.Empty : Path.GetFileNameWithoutExtension(fileName);
    }

    /// <summary>
    /// Gets the content type for the specified file name.
    /// </summary>
    /// <param name="fileName">The file name.</param>
    /// <param name="defaultContentType">The fallback content type.</param>
    /// <returns>The detected content type, or the fallback value when detection fails.</returns>
    public static string GetContentType(string fileName, string defaultContentType = "application/octet-stream")
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fileName);

        return ContentTypeProvider.TryGetContentType(fileName, out var contentType)
            ? contentType
            : defaultContentType;
    }

    /// <summary>
    /// Formats a byte count as a human-readable size string.
    /// </summary>
    /// <param name="byteCount">The size in bytes.</param>
    /// <returns>The formatted size string.</returns>
    public static string FormatSize(long byteCount)
    {
        if (byteCount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(byteCount), byteCount, "Byte count cannot be negative.");
        }

        string[] units = ["B", "KB", "MB", "GB", "TB", "PB"];
        double size = byteCount;
        var unitIndex = 0;

        while (size >= 1024 && unitIndex < units.Length - 1)
        {
            size /= 1024;
            unitIndex++;
        }

        return $"{size:0.##} {units[unitIndex]}";
    }

    /// <summary>
    /// Reads all bytes from a stream.
    /// </summary>
    /// <param name="stream">The source stream.</param>
    /// <param name="leaveOpen"><see langword="true"/> to leave the stream open after reading; otherwise, <see langword="false"/>.</param>
    /// <returns>The stream contents as a byte array.</returns>
    public static byte[] ReadAllBytes(Stream stream, bool leaveOpen = false)
    {
        ArgumentNullException.ThrowIfNull(stream);

        try
        {
            if (stream.CanSeek)
            {
                stream.Seek(0, SeekOrigin.Begin);
            }

            using var output = new MemoryStream();
            stream.CopyTo(output);
            return output.ToArray();
        }
        finally
        {
            if (!leaveOpen)
            {
                stream.Dispose();
            }
        }
    }

    /// <summary>
    /// Reads all text from a stream.
    /// </summary>
    /// <param name="stream">The source stream.</param>
    /// <param name="encoding">The text encoding to use. When <see langword="null"/>, UTF-8 is used.</param>
    /// <param name="leaveOpen"><see langword="true"/> to leave the stream open after reading; otherwise, <see langword="false"/>.</param>
    /// <returns>The stream contents as a string.</returns>
    public static string ReadAllText(Stream stream, Encoding? encoding = null, bool leaveOpen = false)
    {
        return (encoding ?? Encoding.UTF8).GetString(ReadAllBytes(stream, leaveOpen));
    }
}