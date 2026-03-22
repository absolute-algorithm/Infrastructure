using FileFormula.Api.Infrastructure.Constraints;

namespace FileFormula.Api.Infrastructure.Exceptions;

/// <summary>
/// Provides factory members for creating common <see cref="ApiException"/> instances.
/// </summary>
public static class ApiExceptions
{
    /// <summary>
    /// Gets an exception that represents an unauthorized request.
    /// </summary>
    public static ApiException Unauthorized => new ApiException(ERRORCODE.UNAUTHORIZED, "Unauthorized");

    /// <summary>
    /// Gets an exception that represents a forbidden request.
    /// </summary>
    public static ApiException Forbidden => new ApiException(ERRORCODE.FORBIDDEN, "Forbidden");

    /// <summary>
    /// Creates an exception that represents a missing resource.
    /// </summary>
    /// <param name="entity">The resource name to include in the error message.</param>
    /// <returns>An <see cref="ApiException"/> instance.</returns>
    public static ApiException Notfound(string entity) => new ApiException(ERRORCODE.NOTFOUND, string.Format("{0} not found.", entity));

    /// <summary>
    /// Creates an exception that represents an invalid request.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <returns>An <see cref="ApiException"/> instance.</returns>
    public static ApiException Badrequest(string message) => new ApiException(ERRORCODE.BADREQUEST, message);

    /// <summary>
    /// Creates an exception that represents a failed request precondition.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <returns>An <see cref="ApiException"/> instance.</returns>
    public static ApiException PreconditionFailed(string message) => new ApiException(ERRORCODE.PRECONDITIONFAILED, message);

    /// <summary>
    /// Creates an exception that represents an optimistic concurrency conflict.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <returns>An <see cref="ApiException"/> instance.</returns>
    public static ApiException Conflict(string message) => new ApiException(ERRORCODE.CONFLICT, message);

    /// <summary>
    /// Creates an exception for the specified error code and message.
    /// </summary>
    /// <param name="code">The application-specific error code.</param>
    /// <param name="message">The error message.</param>
    /// <returns>An <see cref="ApiException"/> instance.</returns>
    public static ApiException FromCode(string code, string message) => new ApiException(code, message);
}