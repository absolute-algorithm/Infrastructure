using AbsoluteAlgorithm.Core.Constraints;
using AbsoluteAlgorithm.Core.Models.Response;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using System.Text.Json;

namespace AbsoluteAlgorithm.Infrastructure.Filters;

/// <summary>
/// Validates model state and returns the standard API validation response when validation fails.
/// </summary>
public class ValidateModelFilter : ActionFilterAttribute
{
    /// <summary>
    /// Called before the action executes.
    /// </summary>
    /// <param name="context">The action execution context.</param>
    public override void OnActionExecuting(ActionExecutingContext context)
    {
        if (context.ModelState.IsValid)
        {
            return;
        }

        var summary = SummarizeErrors(context.ModelState);
        context.Result = new ObjectResult(new ApiResponse<object>(IsSuccess: false, Error: new ErrorResponse
        {
            ErrorCode = summary.ErrorCode,
            ErrorMessage = summary.ErrorMessage,
            ValidationErrors = summary.ValidationErrors
        }))
        {
            StatusCode = summary.StatusCode
        };
    }

    private static ValidationSummary SummarizeErrors(ModelStateDictionary modelState)
    {
        var errors = modelState
            .Where(entry => entry.Value?.Errors.Count > 0)
            .Select(entry => new ValidationErrorDetail
            {
                Field = NormalizeField(entry.Key),
                Messages = entry.Value!.Errors
                    .Select(GetErrorMessage)
                    .Where(message => !string.IsNullOrWhiteSpace(message))
                    .Distinct(StringComparer.Ordinal)
                    .ToArray()
            })
            .Where(detail => detail.Messages.Count > 0)
            .ToArray();

        if (errors.Length == 0)
        {
            return new ValidationSummary(
                StatusCodes.Status400BadRequest,
                ERRORCODE.BADREQUEST,
                "The request is invalid.",
                null);
        }

        if (errors.Any(detail => detail.Messages.Any(IsMissingBodyMessage)))
        {
            return new ValidationSummary(
                StatusCodes.Status400BadRequest,
                ERRORCODE.BADREQUEST,
                "The request body is required.",
                errors);
        }

        var jsonErrors = errors
            .SelectMany(detail => detail.Messages.Select(message => new { detail.Field, Message = message }))
            .ToArray();

        var malformedJsonMessage = jsonErrors
            .Select(error => error.Message)
            .FirstOrDefault(IsMalformedJsonMessage);

        if (malformedJsonMessage is not null)
        {
            return new ValidationSummary(
                StatusCodes.Status400BadRequest,
                ERRORCODE.BADREQUEST,
                malformedJsonMessage,
                errors);
        }

        var invalidBodyMessage = jsonErrors
            .Select(error => error.Message)
            .FirstOrDefault(IsInvalidBodyMessage);

        if (invalidBodyMessage is not null)
        {
            return new ValidationSummary(
                StatusCodes.Status400BadRequest,
                ERRORCODE.BADREQUEST,
                invalidBodyMessage,
                errors);
        }

        return new ValidationSummary(
            StatusCodes.Status422UnprocessableEntity,
            ERRORCODE.UNPROCESSABLEENTITY,
            errors.SelectMany(detail => detail.Messages).First(),
            errors);
    }

    private static string NormalizeField(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            return "body";
        }

        return key.StartsWith("$", StringComparison.Ordinal) ? key : key.TrimStart('.');
    }

    private static string GetErrorMessage(ModelError error)
    {
        if (!string.IsNullOrWhiteSpace(error.ErrorMessage))
        {
            return error.ErrorMessage;
        }

        return error.Exception?.Message ?? string.Empty;
    }

    private static bool IsMissingBodyMessage(string message)
    {
        return message.Contains("A non-empty request body is required", StringComparison.OrdinalIgnoreCase)
            || message.Contains("The request field is required", StringComparison.OrdinalIgnoreCase)
            || message.Contains("The body field is required", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsMalformedJsonMessage(string message)
    {
        if (!IsJsonRelatedMessage(message))
        {
            return false;
        }

        return !message.Contains("could not be converted", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsInvalidBodyMessage(string message)
    {
        return IsJsonRelatedMessage(message)
            || message.Contains("The input was not valid", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsJsonRelatedMessage(string message)
    {
        return message.Contains("JSON", StringComparison.OrdinalIgnoreCase)
            || message.Contains("JsonException", StringComparison.OrdinalIgnoreCase)
            || message.Contains(nameof(JsonException), StringComparison.Ordinal)
            || message.Contains("Path:", StringComparison.OrdinalIgnoreCase)
            || message.Contains("BytePositionInLine", StringComparison.OrdinalIgnoreCase)
            || message.Contains("LineNumber", StringComparison.OrdinalIgnoreCase);
    }

    private sealed record ValidationSummary(
        int StatusCode,
        string ErrorCode,
        string ErrorMessage,
        IReadOnlyList<ValidationErrorDetail>? ValidationErrors);
}