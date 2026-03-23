using System.Security.Claims;

namespace AbsoluteAlgorithm.Infrastructure.Services;

/// <summary>
/// Provides access to the current authenticated user.
/// </summary>
public interface ICurrentUserAccessor
{
    /// <summary>
    /// Gets the current claims principal when an HTTP request is active.
    /// </summary>
    ClaimsPrincipal? Principal { get; }

    /// <summary>
    /// Gets the current user identifier.
    /// </summary>
    string? UserId { get; }

    /// <summary>
    /// Gets the current user email address.
    /// </summary>
    string? Email { get; }

    /// <summary>
    /// Gets a value indicating whether the current user is authenticated.
    /// </summary>
    bool IsAuthenticated { get; }

    /// <summary>
    /// Gets the current user roles.
    /// </summary>
    IReadOnlyList<string> Roles { get; }
}