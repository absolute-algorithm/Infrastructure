using System;
using System.Linq;

namespace AbsoluteAlgorithm.Infrastructure.Sanitizers;

/// <summary>
/// Provides helpers for normalizing file names supplied by external input.
/// </summary>
public static class FileNameSanitizer
{
    /// <summary>
    /// Returns a normalized file name that removes path segments, invalid file name characters, and control characters.
    /// </summary>
    /// <param name="fileName">The source file name.</param>
    /// <returns>A sanitized file name.</returns>
    public static string SanitizeFileName(string? fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
            return "file";
        }

        var leafName = Path.GetFileName(fileName.Trim());
        var invalidCharacters = Path.GetInvalidFileNameChars();

        var sanitizedCharacters = leafName
            .Select(character =>
                char.IsControl(character) || invalidCharacters.Contains(character)
                    ? '_'
                    : character)
            .ToArray();

        var sanitized = new string(sanitizedCharacters)
            .Trim(' ', '.');

        return string.IsNullOrWhiteSpace(sanitized) ? "file" : sanitized;
    }

    /// <summary>
    /// Returns a normalized file extension for the specified file name.
    /// </summary>
    /// <param name="fileName">The source file name.</param>
    /// <returns>A sanitized extension, or an empty string when no extension is present.</returns>
    public static string GetSafeExtension(string? fileName)
    {
        var sanitizedFileName = SanitizeFileName(fileName);
        var extension = Path.GetExtension(sanitizedFileName);

        if (string.IsNullOrEmpty(extension))
        {
            return string.Empty;
        }

        var safeExtensionCharacters = extension
            .Where(character => character == '.' || char.IsLetterOrDigit(character) || character == '_' || character == '-')
            .ToArray();

        var safeExtension = new string(safeExtensionCharacters);
        return safeExtension == "." ? string.Empty : safeExtension;
    }
}