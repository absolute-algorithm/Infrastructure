using System.Data.Common;
using System.Net;
using System.Text.Json;
using AbsoluteAlgorithm.Core.Constraints;
using AbsoluteAlgorithm.Core.Exceptions;
using AbsoluteAlgorithm.Core.Models.Response;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace AbsoluteAlgorithm.Infrastructure.Middlewares;

/// <summary>
/// Converts exceptions to the standard API error response.
/// </summary>
public class ExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionMiddleware> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ExceptionMiddleware"/> class.
    /// </summary>
    /// <param name="next">The next middleware in the pipeline.</param>
    /// <param name="logger">The logger used for exception reporting.</param>
    public ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    /// <summary>
    /// Processes the current request.
    /// </summary>
    /// <param name="context">The current HTTP context.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (OperationCanceledException oex)
        {
            await HandleOperationCancelledExceptionAsync(context, oex).ConfigureAwait(false);
        }
        catch (Core.Exceptions.ApplicationException apiex)
        {
            await HandleApiExceptionAsync(context, apiex).ConfigureAwait(false);
        }
        catch (DbException dbex)
        {
            await HandleDbExceptionAsync(context, dbex).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex).ConfigureAwait(false);
        }
    }

    private Task HandleOperationCancelledExceptionAsync(HttpContext context, OperationCanceledException exception)
    {
        var error = new ErrorResponse
        {
            ErrorCode = "E499",
            ErrorMessage = "Operation Cancelled"
        };

        return WriteResponse(error, context);
    }

    private Task HandleApiExceptionAsync(HttpContext context, Core.Exceptions.ApplicationException exception)
    {
        _logger.LogError(exception, "{message}", exception.Message);

        var error = new ErrorResponse
        {
            ErrorCode = exception.ErrorCode,
            ErrorMessage = exception.ErrorMessage
        };

        return WriteResponse(error, context);
    }

    private Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        _logger.LogError(exception, "{message}", exception.Message);

        var error = new ErrorResponse
        {
            ErrorCode = "E500",
            ErrorMessage = "Something went wrong, Please try again!"
        };

        return WriteResponse(error, context);
    }

    private Task HandleDbExceptionAsync(HttpContext context, DbException exception)
    {
        _logger.LogError(exception, "Database Error: {message}", exception.Message);

        var error = new ErrorResponse
        {
            ErrorCode = ERRORCODE.BADREQUEST,
            ErrorMessage = "A database error occurred."
        };

        // Pattern matching to find specific provider codes
        switch (exception)
        {
            case Npgsql.NpgsqlException nex:
                error = HandlePostgresError(nex);
                break;

            case Microsoft.Data.SqlClient.SqlException sex:
                error = HandleSqlServerError(sex);
                break;
        }

        return WriteResponse(error, context);
    }

    private ErrorResponse HandlePostgresError(Npgsql.NpgsqlException ex)
    {
        return ex.SqlState switch
        {
            "23505" => new ErrorResponse { ErrorCode = ERRORCODE.CONFLICT, ErrorMessage = "Duplicate record: This entry already exists." },
            "23503" => new ErrorResponse { ErrorCode = ERRORCODE.CONFLICT, ErrorMessage = "Consistency error: This record is tied to other data." },
            "23502" => new ErrorResponse { ErrorCode = ERRORCODE.BADREQUEST, ErrorMessage = "Required data is missing." },
            _ => new ErrorResponse { ErrorCode = ERRORCODE.INTERNALSERVERERROR, ErrorMessage = "Something went wrong, Please try again!" }
        };
    }

    private ErrorResponse HandleSqlServerError(Microsoft.Data.SqlClient.SqlException ex)
    {
        return ex.Number switch
        {
            2627 or 2601 => new ErrorResponse { ErrorCode = ERRORCODE.CONFLICT, ErrorMessage = "Duplicate record: This entry already exists." },
            547 => new ErrorResponse { ErrorCode = ERRORCODE.CONFLICT, ErrorMessage = "Constraint violation occurred." },
            _ => new ErrorResponse { ErrorCode = ERRORCODE.INTERNALSERVERERROR, ErrorMessage = "Something went wrong, Please try again!" }
        };
    }

    private static Task WriteResponse(ErrorResponse error, HttpContext context)
    {
        HttpStatusCode httpStatusCode = HttpStatusCode.BadRequest;

        switch (error.ErrorCode)
        {
            case ERRORCODE.UNAUTHORIZED:
                httpStatusCode = HttpStatusCode.Unauthorized;
                break;
            case ERRORCODE.FORBIDDEN:
                httpStatusCode = HttpStatusCode.Forbidden;
                break;
            case ERRORCODE.NOTFOUND:
                httpStatusCode = HttpStatusCode.NotFound;
                break;
            case ERRORCODE.BADREQUEST:
                httpStatusCode = HttpStatusCode.BadRequest;
                break;
            case ERRORCODE.GONE:
                httpStatusCode = HttpStatusCode.Gone;
                break;
            case ERRORCODE.PRECONDITIONFAILED:
                httpStatusCode = HttpStatusCode.PreconditionFailed;
                break;
            case ERRORCODE.UNPROCESSABLEENTITY:
                httpStatusCode = HttpStatusCode.UnprocessableEntity;
                break;
            case ERRORCODE.CONFLICT:
                httpStatusCode = HttpStatusCode.Conflict;
                break;
            case ERRORCODE.TOOMANYREQUESTS:
                httpStatusCode = HttpStatusCode.TooManyRequests;
                break;
            case ERRORCODE.OPERATIONCANCELLED:
                httpStatusCode = HttpStatusCode.RequestTimeout;
                break;
            case ERRORCODE.INTERNALSERVERERROR:
                httpStatusCode = HttpStatusCode.InternalServerError;
                break;
            default:
                httpStatusCode = HttpStatusCode.BadRequest;
                break;
        }

        ApiResponse<object> apiResponse = new ApiResponse<object>
        {
            IsSuccess = false,
            Error = error
        };

        var jsonResponse = JsonSerializer.Serialize(apiResponse);
        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)httpStatusCode;
        return context.Response.WriteAsync(jsonResponse);
    }
}