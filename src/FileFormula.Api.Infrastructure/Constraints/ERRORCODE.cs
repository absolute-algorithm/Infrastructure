using System;

namespace FileFormula.Api.Infrastructure.Constraints;

/// <summary>
/// Defines the standard error codes used by the library response contract.
/// </summary>
public static class ERRORCODE
{
    /// <summary>
    /// Indicates that the operation was canceled.
    /// </summary>
    public const string OPERATIONCANCELLED = "E499";

    /// <summary>
    /// Indicates that the request is invalid.
    /// </summary>
    public const string BADREQUEST = "E400";

    /// <summary>
    /// Indicates that authentication is required or has failed.
    /// </summary>
    public const string UNAUTHORIZED = "E401";

    /// <summary>
    /// Indicates that the caller does not have permission to perform the requested operation.
    /// </summary>
    public const string FORBIDDEN = "E403";

    /// <summary>
    /// Indicates that the requested resource was not found.
    /// </summary>
    public const string NOTFOUND = "E404";

    /// <summary>
    /// Indicates that the requested resource is no longer available.
    /// </summary>
    public const string GONE = "E410";

    /// <summary>
    /// Indicates that the request payload is syntactically valid but failed validation.
    /// </summary>
    public const string UNPROCESSABLEENTITY = "E422";

    /// <summary>
    /// Indicates that a rate limit has been exceeded.
    /// </summary>
    public const string TOOMANYREQUESTS = "E429";

    /// <summary>
    /// Indicates that a request precondition failed.
    /// </summary>
    public const string PRECONDITIONFAILED = "E412";

    /// <summary>
    /// Indicates that the request conflicts with the current state of the target resource.
    /// </summary>
    public const string CONFLICT = "E409";

    /// <summary>
    /// Indicates that an unexpected server error has occurred.
    /// </summary>
    public const string INTERNALSERVERERROR = "E500";
}