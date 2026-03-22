using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;

namespace FileFormula.Api.Infrastructure.Utilities;

/// <summary>
/// Provides helper methods for common authentication flows and cookie handling.
/// </summary>
public static class AuthUtility
{
    /// <summary>
    /// Creates a claim when the supplied value is present.
    /// </summary>
    /// <param name="claimType">The claim type.</param>
    /// <param name="value">The claim value.</param>
    /// <returns>The created claim when the value is present; otherwise, <see langword="null"/>.</returns>
    public static Claim? CreateClaim(string claimType, string? value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(claimType);

        return string.IsNullOrWhiteSpace(value)
            ? null
            : new Claim(claimType, value);
    }

    /// <summary>
    /// Creates a claims identity from the supplied values.
    /// </summary>
    /// <param name="claims">The claims to include.</param>
    /// <param name="authenticationType">The authentication type.</param>
    /// <param name="nameClaimType">The name claim type.</param>
    /// <param name="roleClaimType">The role claim type.</param>
    /// <returns>The created claims identity.</returns>
    public static ClaimsIdentity CreateIdentity(
        IEnumerable<Claim> claims,
        string authenticationType = CookieAuthenticationDefaults.AuthenticationScheme,
        string nameClaimType = ClaimTypes.Name,
        string roleClaimType = ClaimTypes.Role)
    {
        ArgumentNullException.ThrowIfNull(claims);
        ArgumentException.ThrowIfNullOrWhiteSpace(authenticationType);

        return new ClaimsIdentity(claims, authenticationType, nameClaimType, roleClaimType);
    }

    /// <summary>
    /// Creates a claims identity from a key-value claim map.
    /// </summary>
    /// <param name="claims">The claims to include.</param>
    /// <param name="authenticationType">The authentication type.</param>
    /// <returns>The created claims identity.</returns>
    public static ClaimsIdentity CreateIdentity(
        IDictionary<string, string?> claims,
        string authenticationType = CookieAuthenticationDefaults.AuthenticationScheme)
    {
        ArgumentNullException.ThrowIfNull(claims);

        return CreateIdentity(
            claims.Where(entry => !string.IsNullOrWhiteSpace(entry.Key) && entry.Value is not null)
                .Select(entry => new Claim(entry.Key, entry.Value!)),
            authenticationType);
    }

    /// <summary>
    /// Creates a claims principal from the supplied claims.
    /// </summary>
    /// <param name="claims">The claims to include.</param>
    /// <param name="authenticationType">The authentication type.</param>
    /// <returns>The created claims principal.</returns>
    public static ClaimsPrincipal CreatePrincipal(
        IEnumerable<Claim> claims,
        string authenticationType = CookieAuthenticationDefaults.AuthenticationScheme)
    {
        return new ClaimsPrincipal(CreateIdentity(claims, authenticationType));
    }

    /// <summary>
    /// Signs in the current user using cookie authentication.
    /// </summary>
    /// <param name="context">The HTTP context.</param>
    /// <param name="principal">The authenticated principal.</param>
    /// <param name="isPersistent"><see langword="true"/> to persist the sign-in cookie across browser sessions.</param>
    /// <param name="expiresUtc">The cookie expiration time.</param>
    /// <param name="authenticationScheme">The authentication scheme.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public static Task SignInWithCookieAsync(
        HttpContext context,
        ClaimsPrincipal principal,
        bool isPersistent = true,
        DateTimeOffset? expiresUtc = null,
        string authenticationScheme = CookieAuthenticationDefaults.AuthenticationScheme)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(principal);
        ArgumentException.ThrowIfNullOrWhiteSpace(authenticationScheme);

        return context.SignInAsync(
            authenticationScheme,
            principal,
            new AuthenticationProperties
            {
                IsPersistent = isPersistent,
                ExpiresUtc = expiresUtc
            });
    }

    /// <summary>
    /// Signs in the current user using cookie authentication from a claim set.
    /// </summary>
    /// <param name="context">The HTTP context.</param>
    /// <param name="claims">The claims to include.</param>
    /// <param name="isPersistent"><see langword="true"/> to persist the sign-in cookie across browser sessions.</param>
    /// <param name="expiresUtc">The cookie expiration time.</param>
    /// <param name="authenticationScheme">The authentication scheme.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public static Task SignInWithCookieAsync(
        HttpContext context,
        IEnumerable<Claim> claims,
        bool isPersistent = true,
        DateTimeOffset? expiresUtc = null,
        string authenticationScheme = CookieAuthenticationDefaults.AuthenticationScheme)
    {
        return SignInWithCookieAsync(context, CreatePrincipal(claims, authenticationScheme), isPersistent, expiresUtc, authenticationScheme);
    }

    /// <summary>
    /// Signs out the current cookie-authenticated user.
    /// </summary>
    /// <param name="context">The HTTP context.</param>
    /// <param name="authenticationScheme">The authentication scheme.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public static Task SignOutCookieAsync(HttpContext context, string authenticationScheme = CookieAuthenticationDefaults.AuthenticationScheme)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentException.ThrowIfNullOrWhiteSpace(authenticationScheme);

        return context.SignOutAsync(authenticationScheme);
    }

    /// <summary>
    /// Appends a secure cookie with sensible API defaults.
    /// </summary>
    /// <param name="response">The HTTP response.</param>
    /// <param name="name">The cookie name.</param>
    /// <param name="value">The cookie value.</param>
    /// <param name="httpOnly"><see langword="true"/> to prevent client-side script access.</param>
    /// <param name="sameSite">The SameSite mode.</param>
    /// <param name="secure">The secure-cookie flag.</param>
    /// <param name="path">The cookie path.</param>
    /// <param name="expiresUtc">The cookie expiration time.</param>
    public static void AppendSecureCookie(
        HttpResponse response,
        string name,
        string value,
        bool httpOnly = true,
        SameSiteMode sameSite = SameSiteMode.Strict,
        bool secure = true,
        string path = "/",
        DateTimeOffset? expiresUtc = null)
    {
        ArgumentNullException.ThrowIfNull(response);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(value);

        response.Cookies.Append(name, value, new CookieOptions
        {
            HttpOnly = httpOnly,
            IsEssential = true,
            Path = path,
            SameSite = sameSite,
            Secure = secure,
            Expires = expiresUtc
        });
    }

    /// <summary>
    /// Writes a refresh-token cookie using secure defaults.
    /// </summary>
    /// <param name="response">The HTTP response.</param>
    /// <param name="refreshToken">The refresh token value.</param>
    /// <param name="cookieName">The cookie name.</param>
    /// <param name="expiresUtc">The cookie expiration time.</param>
    public static void AppendRefreshTokenCookie(
        HttpResponse response,
        string refreshToken,
        string cookieName = "refresh_token",
        DateTimeOffset? expiresUtc = null)
    {
        AppendSecureCookie(response, cookieName, refreshToken, httpOnly: true, sameSite: SameSiteMode.Strict, secure: true, expiresUtc: expiresUtc);
    }

    /// <summary>
    /// Deletes a cookie using secure defaults.
    /// </summary>
    /// <param name="response">The HTTP response.</param>
    /// <param name="name">The cookie name.</param>
    /// <param name="path">The cookie path.</param>
    public static void DeleteCookie(HttpResponse response, string name, string path = "/")
    {
        ArgumentNullException.ThrowIfNull(response);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        response.Cookies.Delete(name, new CookieOptions
        {
            Path = path,
            HttpOnly = true,
            SameSite = SameSiteMode.Strict,
            Secure = true
        });
    }
}