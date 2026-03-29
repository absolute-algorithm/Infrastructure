using System;
using AbsoluteAlgorithm.Core.Models.Response;
using Microsoft.AspNetCore.Mvc;

namespace AbsoluteAlgorithm.Infrastructure.Web;

/// <summary>
/// Base controller for Absolute Algorithm APIs, providing common response formatting and utilities.
/// </summary>
public abstract class AbsoluteController : ControllerBase
{

    /// <summary>
    /// Returns 200 OK with the Absolute Envelope.
    /// </summary>
    protected ActionResult<ApiResponse<T>> Ok<T>(T data) where T : class => base.Ok(new ApiResponse<T> { IsSuccess = true, Data = data });

    /// <summary>
    /// Returns 201 Created with the Absolute Envelope.
    /// Useful for POST requests.
    /// </summary>
    protected ActionResult<ApiResponse<T>> Created<T>(string uri, T data) where T : class => base.Created(uri, new ApiResponse<T> { IsSuccess = true, Data = data });

    /// <summary>
    /// Returns 200 OK with the Absolute Envelope.
    /// Useful for PUT requests.
    /// </summary>
    protected ActionResult<ApiResponse<object>> Updated(string message = "Operation successful") => base.Ok(new ApiResponse<object> { IsSuccess = true, Data = message });

    /// <summary>
    /// Returns 200 OK with the Absolute Envelope.
    /// Useful for DELETE requests.
    /// </summary>
    protected ActionResult<ApiResponse<object>> Deleted(string message = "Operation successful") => base.Ok(new ApiResponse<object> { IsSuccess = true, Data = message });

    /// <summary>
    /// Returns 204 No Content. (Strictly no body allowed).
    /// </summary>
    protected IActionResult SuccessNoContent() => base.NoContent();

    /// <summary>
    /// Returns 400 Bad Request with the Absolute Envelope.
    /// Useful for validation or logic errors.
    /// </summary>
    protected ActionResult<ApiResponse<object>> Error(string message, int statusCode = 400) => StatusCode(statusCode, new ApiResponse<object> { IsSuccess = false, Data = message });

    /// <summary>
    /// For File downloads, we skip the envelope entirely.
    /// </summary>
    protected FileResult Download(byte[] fileContents, string contentType, string fileName) => File(fileContents, contentType, fileName);
}