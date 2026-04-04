using AbsoluteAlgorithm.Core.Models.Request;
using AbsoluteAlgorithm.Core.Networking;
using Microsoft.AspNetCore.Http;

namespace AbsoluteAlgorithm.Infrastructure.Services;

/// <summary>
/// Resolves normalized request metadata from the current HTTP context.
/// </summary>
public class HttpContextRequestMetadataAccessor : IRequestMetadataAccessor
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    /// <summary>
    /// Initializes a new instance of the <see cref="HttpContextRequestMetadataAccessor"/> class.
    /// </summary>
    /// <param name="httpContextAccessor">The HTTP context accessor.</param>
    public HttpContextRequestMetadataAccessor(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    /// <inheritdoc />
    public RequestMetadata? Current => _httpContextAccessor.HttpContext is { } context
        ? HttpUtility.GetRequestMetadata(context)
        : null;
}