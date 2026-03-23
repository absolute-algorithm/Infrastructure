using AbsoluteAlgorithm.Infrastructure.Models.Request;

namespace AbsoluteAlgorithm.Infrastructure.Services;

/// <summary>
/// Provides access to normalized metadata for the current request.
/// </summary>
public interface IRequestMetadataAccessor
{
    /// <summary>
    /// Gets the current request metadata when an HTTP request is active.
    /// </summary>
    RequestMetadata? Current { get; }
}