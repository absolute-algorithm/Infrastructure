using System.Reflection;
using AbsoluteAlgorithm.Infrastructure.Constraints;
using AbsoluteAlgorithm.Infrastructure.Models.Response;
using AbsoluteAlgorithm.Infrastructure.Utilities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.AspNetCore.Mvc.Filters;

namespace AbsoluteAlgorithm.Infrastructure.Filters;

/// <summary>
/// Requires a valid API key to access the decorated controller or action.
/// </summary>
/// <remarks>
/// The expected API key is read from the environment variable identified by <see cref="SecretName"/>.
/// The request is authorized when the provided key matches the configured secret.
/// </remarks>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
public sealed class AuthorizeKeyAttribute : Attribute, IAuthorizationFilter
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AuthorizeKeyAttribute"/> class.
    /// </summary>
    /// <param name="headerName">The request header that carries the API key.</param>
    /// <param name="secretName">The environment variable that contains the expected API key.</param>
    public AuthorizeKeyAttribute(string headerName, string secretName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(headerName);
        ArgumentNullException.ThrowIfNull(secretName);

        if (string.IsNullOrWhiteSpace(secretName))
        {
            throw new ArgumentException("At least one non-empty secret name is required.", nameof(secretName));
        }

        HeaderName = headerName;
        SecretName = secretName;
    }

    /// <summary>
    /// Gets the request header that carries the API key.
    /// </summary>
    public string HeaderName { get; }

    /// <summary>
    /// Gets the environment variable that contains the expected API key.
    /// </summary>
    public string SecretName { get; }

    /// <inheritdoc />
    public void OnAuthorization(AuthorizationFilterContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (AllowsAnonymous(context))
        {
            return;
        }

        var providedApiKey = context.HttpContext.Request.Headers[HeaderName].ToString();
        if (string.IsNullOrWhiteSpace(providedApiKey))
        {
            SetUnauthorized(context);
            return;
        }

        var expectedApiKey = Environment.GetEnvironmentVariable(SecretName);
        if (!string.IsNullOrWhiteSpace(expectedApiKey) && TokenUtility.FixedTimeEquals(expectedApiKey, providedApiKey))
        {
            return;
        }

        SetUnauthorized(context);
    }

    private static void SetUnauthorized(AuthorizationFilterContext context)
    {
        context.Result = new ObjectResult(new ApiResponse<object>(
            IsSuccess: false,
            Error: new ErrorResponse
            {
                ErrorCode = ERRORCODE.UNAUTHORIZED,
                ErrorMessage = "Unauthorized"
            }))
        {
            StatusCode = StatusCodes.Status401Unauthorized
        };
    }

    private static bool AllowsAnonymous(AuthorizationFilterContext context)
    {
        return context.Filters.OfType<IAllowAnonymousFilter>().Any()
            || context.ActionDescriptor.EndpointMetadata.OfType<IAllowAnonymous>().Any()
            || context.ActionDescriptor.EndpointMetadata.OfType<AllowAnonymousAttribute>().Any()
            || context.ActionDescriptor is Microsoft.AspNetCore.Mvc.Controllers.ControllerActionDescriptor actionDescriptor
            && (actionDescriptor.MethodInfo.IsDefined(typeof(AllowAnonymousAttribute), inherit: true)
                || actionDescriptor.ControllerTypeInfo.IsDefined(typeof(AllowAnonymousAttribute), inherit: true));
    }
}