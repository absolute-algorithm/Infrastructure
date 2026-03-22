namespace FileFormula.Api.Infrastructure.Enums;

/// <summary>
/// Specifies how API versions are read from incoming requests.
/// </summary>
public enum ApiVersionReaderType : byte
{
    /// <summary>
    /// Reads the version from a query-string parameter.
    /// </summary>
    QueryString = 1,

    /// <summary>
    /// Reads the version from a request header.
    /// </summary>
    Header,

    /// <summary>
    /// Reads the version from a media-type parameter.
    /// </summary>
    MediaType,

    /// <summary>
    /// Reads the version from a URL segment such as <c>/v1/</c>.
    /// </summary>
    UrlSegment
}