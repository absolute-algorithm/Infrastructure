using System.Security.Claims;

namespace AbsoluteAlgorithm.Infrastructure.Utilities;

/// <summary>
/// Provides helper methods for reading claims from an authenticated principal.
/// </summary>
public static class ClaimUtility
{
    /// <summary>
    /// Gets a claim value from the supplied principal.
    /// </summary>
    /// <param name="principal">The claims principal.</param>
    /// <param name="claimType">The claim type to read.</param>
    /// <returns>The claim value when present; otherwise, <see langword="null"/>.</returns>
    public static string? GetClaimValue(ClaimsPrincipal? principal, string claimType)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(claimType);

        return principal?.FindFirst(claimType)?.Value;
    }

    /// <summary>
    /// Gets the user identifier from the supplied principal.
    /// </summary>
    /// <param name="principal">The claims principal.</param>
    /// <returns>The user identifier when present; otherwise, <see langword="null"/>.</returns>
    public static string? GetUserId(ClaimsPrincipal? principal)
    {
        return GetClaimValue(principal, ClaimTypes.NameIdentifier)
            ?? GetClaimValue(principal, "sub");
    }

    /// <summary>
    /// Gets the email address from the supplied principal.
    /// </summary>
    /// <param name="principal">The claims principal.</param>
    /// <returns>The email address when present; otherwise, <see langword="null"/>.</returns>
    public static string? GetEmail(ClaimsPrincipal? principal)
    {
        return GetClaimValue(principal, ClaimTypes.Email)
            ?? GetClaimValue(principal, "email");
    }

    /// <summary>
    /// Gets the roles from the supplied principal.
    /// </summary>
    /// <param name="principal">The claims principal.</param>
    /// <returns>The role values associated with the principal.</returns>
    public static IReadOnlyList<string> GetRoles(ClaimsPrincipal? principal)
    {
        return principal?
            .Claims
            .Where(claim => claim.Type == ClaimTypes.Role || claim.Type == "role" || claim.Type == "roles")
            .Select(claim => claim.Value)
            .Distinct(StringComparer.Ordinal)
            .ToArray() ?? [];
    }

    /// <summary>
    /// Determines whether the supplied principal has the specified role.
    /// </summary>
    /// <param name="principal">The claims principal.</param>
    /// <param name="role">The role to check.</param>
    /// <returns><see langword="true"/> when the principal has the role; otherwise, <see langword="false"/>.</returns>
    public static bool HasRole(ClaimsPrincipal? principal, string role)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(role);

        return GetRoles(principal).Contains(role, StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Determines whether the supplied principal has any of the specified roles.
    /// </summary>
    /// <param name="principal">The claims principal.</param>
    /// <param name="roles">The roles to check.</param>
    /// <returns><see langword="true"/> when the principal has any matching role; otherwise, <see langword="false"/>.</returns>
    public static bool HasAnyRole(ClaimsPrincipal? principal, IEnumerable<string> roles)
    {
        ArgumentNullException.ThrowIfNull(roles);

        var principalRoles = GetRoles(principal);
        return roles.Any(role => principalRoles.Contains(role, StringComparer.OrdinalIgnoreCase));
    }
}