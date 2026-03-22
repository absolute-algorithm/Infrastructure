using System.Text.Json.Serialization;

namespace FileFormula.Api.Infrastructure.Models.Auth;

/// <summary>
/// Represents authentication and authorization settings for the library.
/// </summary>
public class AuthManifest
{
    /// <summary>
    /// Gets a value indicating whether JWT authentication is enabled in the manifest.
    /// </summary>
    [JsonPropertyName("enableJwt")]
    public bool EnableJwt { get; init; } = true;

    /// <summary>
    /// Gets a value indicating whether cookie authentication is enabled in the manifest.
    /// </summary>
    [JsonPropertyName("enableCookies")]
    public bool EnableCookies { get; init; } = true;

    /// <summary>
    /// Gets a value indicating whether CSRF protection is enabled for cookie-authenticated requests.
    /// </summary>
    /// <remarks>
    /// This setting is only applied when <see cref="EnableCookies"/> is <see langword="true"/>.
    /// Bearer token authentication does not require CSRF protection because credentials are not sent automatically by the browser.
    /// </remarks>
    [JsonPropertyName("enableCsrfProtection")]
    public bool EnableCsrfProtection { get; init; } = false;

    /// <summary>
    /// Gets a value indicating whether API key authentication via request headers is enabled.
    /// </summary>
    /// <remarks>
    /// When enabled, the <see cref="Filters.AuthorizeKeyAttribute"/> can be used on controllers and actions
    /// to validate a request header against one or more environment variable secrets.
    /// </remarks>
    [JsonPropertyName("enableApiKeyAuth")]
    public bool EnableApiKeyAuth { get; init; } = false;

    /// <summary>
    /// Gets the authorization policies to register.
    /// </summary>
    [JsonPropertyName("policies")]
    public List<AuthPolicy>? Policies { get; init; }
}