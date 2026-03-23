using System;
using System.Text.Json.Serialization;
using Newtonsoft.Json;

namespace AbsoluteAlgorithm.Infrastructure.Models.Storage;

/// <summary>
/// Represents file content supplied to a storage operation.
/// </summary>
public class FileContent
{
    /// <summary>
    /// Gets the original file name.
    /// </summary>
    [JsonProperty("fileName")]
    public string FileName { get; init; } = null!;

    /// <summary>
    /// Gets the base64-encoded file content.
    /// </summary>
    [JsonProperty("base64Content")]
    public string? Base64Content { get; init; }

    /// <summary>
    /// Gets the raw file bytes.
    /// </summary>
    [JsonPropertyName("byteArrayContent")]
    public byte[]? ByteArrayContent { get; init; }

    /// <summary>
    /// Gets the MIME content type.
    /// </summary>
    [JsonPropertyName("contentType")]
    public string ContentType { get; init; } = "application/octet-stream";

    /// <summary>
    /// Creates a readable stream for the current content.
    /// </summary>
    /// <returns>A stream that contains the file content.</returns>
    /// <exception cref="InvalidOperationException">Thrown when no content is available.</exception>
    public Stream GetContentStream()
    {
        if (ByteArrayContent != null)
            return new MemoryStream(ByteArrayContent);

        if (!string.IsNullOrEmpty(Base64Content))
            return new MemoryStream(Convert.FromBase64String(Base64Content));

        throw new InvalidOperationException("No content found in FileContent object.");
    }
}
