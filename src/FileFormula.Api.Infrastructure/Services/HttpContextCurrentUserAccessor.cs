using System.Security.Claims;
using FileFormula.Api.Infrastructure.Utilities;
using Microsoft.AspNetCore.Http;

namespace FileFormula.Api.Infrastructure.Services;

/// <summary>
/// Resolves the current authenticated user from the active HTTP context.
/// </summary>
public class HttpContextCurrentUserAccessor : ICurrentUserAccessor
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    /// <summary>
    /// Initializes a new instance of the <see cref="HttpContextCurrentUserAccessor"/> class.
    /// </summary>
    /// <param name="httpContextAccessor">The HTTP context accessor.</param>
    public HttpContextCurrentUserAccessor(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    /// <inheritdoc />
    public ClaimsPrincipal? Principal => _httpContextAccessor.HttpContext?.User;

    /// <inheritdoc />
    public string? UserId => ClaimUtility.GetUserId(Principal);

    /// <inheritdoc />
    public string? Email => ClaimUtility.GetEmail(Principal);

    /// <inheritdoc />
    public bool IsAuthenticated => Principal?.Identity?.IsAuthenticated ?? false;

    /// <inheritdoc />
    public IReadOnlyList<string> Roles => ClaimUtility.GetRoles(Principal);
}