using AbsoluteAlgorithm.Core.Constraints;
using AbsoluteAlgorithm.Core.Exceptions;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Http;

namespace AbsoluteAlgorithm.Infrastructure.Middlewares;

/// <summary>
/// Issues and validates antiforgery tokens for cookie-authenticated API requests.
/// </summary>
public class CsrfMiddleware
{
    private const string AuthenticationCookieName = "AbsoluteAuth";
    private const string RequestTokenCookieName = "XSRF-TOKEN";
    private readonly RequestDelegate _next;
    private readonly IAntiforgery _antiforgery;

    /// <summary>
    /// Initializes a new instance of the <see cref="CsrfMiddleware"/> class.
    /// </summary>
    /// <param name="next">The next middleware in the pipeline.</param>
    /// <param name="antiforgery">The antiforgery service.</param>
    public CsrfMiddleware(RequestDelegate next, IAntiforgery antiforgery)
    {
        _next = next;
        _antiforgery = antiforgery;
    }

    /// <summary>
    /// Processes the current request.
    /// </summary>
    /// <param name="context">The current HTTP context.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public async Task InvokeAsync(HttpContext context)
    {
        if (IsSafeMethod(context.Request.Method))
        {
            var tokens = _antiforgery.GetAndStoreTokens(context);
            if (!string.IsNullOrWhiteSpace(tokens.RequestToken))
            {
                context.Response.Cookies.Append(RequestTokenCookieName, tokens.RequestToken, new CookieOptions
                {
                    HttpOnly = false,
                    IsEssential = true,
                    Path = "/",
                    SameSite = SameSiteMode.Strict,
                    Secure = context.Request.IsHttps
                });
            }

            await _next(context);
            return;
        }

        if (ShouldValidateRequest(context))
        {
            try
            {
                await _antiforgery.ValidateRequestAsync(context);
            }
            catch (AntiforgeryValidationException)
            {
                throw new Core.Exceptions.ApplicationException(ERRORCODE.FORBIDDEN, "A valid CSRF token is required for cookie-authenticated requests.");
            }
        }

        await _next(context);
    }

    private static bool ShouldValidateRequest(HttpContext context)
    {
        if (IsSafeMethod(context.Request.Method))
        {
            return false;
        }

        if (!context.Request.Cookies.ContainsKey(AuthenticationCookieName))
        {
            return false;
        }

        var authorizationHeader = context.Request.Headers.Authorization.ToString();
        return string.IsNullOrWhiteSpace(authorizationHeader) || !authorizationHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsSafeMethod(string method)
    {
        return HttpMethods.IsGet(method)
            || HttpMethods.IsHead(method)
            || HttpMethods.IsOptions(method)
            || HttpMethods.IsTrace(method);
    }
}