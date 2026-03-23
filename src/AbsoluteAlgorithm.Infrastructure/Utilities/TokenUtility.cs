using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.IdentityModel.Tokens;

namespace AbsoluteAlgorithm.Infrastructure.Utilities;

/// <summary>
/// Provides helper methods for generating, issuing, validating, and comparing security tokens.
/// </summary>
public static class TokenUtility
{
    /// <summary>
    /// Generates a cryptographically secure token.
    /// </summary>
    /// <param name="sizeInBytes">The token size in bytes.</param>
    /// <param name="urlSafe"><see langword="true"/> to return a URL-safe token; otherwise, <see langword="false"/> to return a standard base64 token.</param>
    /// <returns>The generated token string.</returns>
    public static string GenerateToken(int sizeInBytes = 32, bool urlSafe = true)
    {
        if (sizeInBytes <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(sizeInBytes), sizeInBytes, "Token size must be greater than zero.");
        }

        var bytes = RandomNumberGenerator.GetBytes(sizeInBytes);
        return urlSafe ? WebEncoders.Base64UrlEncode(bytes) : Convert.ToBase64String(bytes);
    }

    /// <summary>
    /// Generates an opaque refresh token.
    /// </summary>
    /// <param name="sizeInBytes">The token size in bytes.</param>
    /// <returns>The generated refresh token.</returns>
    public static string GenerateRefreshToken(int sizeInBytes = 64)
    {
        return GenerateToken(sizeInBytes, urlSafe: true);
    }

    /// <summary>
    /// Generates a one-time token suitable for verification and password reset flows.
    /// </summary>
    /// <param name="sizeInBytes">The token size in bytes.</param>
    /// <returns>The generated one-time token.</returns>
    public static string GenerateOneTimeToken(int sizeInBytes = 32)
    {
        return GenerateToken(sizeInBytes, urlSafe: true);
    }

    /// <summary>
    /// Generates an API key with the specified prefix.
    /// </summary>
    /// <param name="prefix">The API key prefix, such as <c>ak_live</c> or <c>ak_test</c>.</param>
    /// <param name="sizeInBytes">The random token size in bytes.</param>
    /// <returns>The generated API key.</returns>
    public static string GenerateApiKey(string prefix = "ak_live", int sizeInBytes = 32)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(prefix);

        return $"{prefix}_{GenerateToken(sizeInBytes, urlSafe: true)}";
    }

    /// <summary>
    /// Generates a cryptographically secure base64 secret suitable for symmetric JWT signing.
    /// </summary>
    /// <param name="sizeInBytes">The key length in bytes. The default produces a 512-bit secret.</param>
    /// <returns>A base64-encoded symmetric signing secret.</returns>
    public static string GenerateSymmetricKey(int sizeInBytes = 64)
    {
        if (sizeInBytes < 32)
        {
            throw new ArgumentOutOfRangeException(nameof(sizeInBytes), sizeInBytes, "JWT symmetric keys must be at least 32 bytes.");
        }

        var bytes = RandomNumberGenerator.GetBytes(sizeInBytes);
        return Convert.ToBase64String(bytes);
    }

    /// <summary>
    /// Generates a signed JWT bearer token.
    /// </summary>
    /// <param name="secretKey">The symmetric signing secret.</param>
    /// <param name="issuer">The token issuer.</param>
    /// <param name="audience">The token audience.</param>
    /// <param name="claims">The token claims.</param>
    /// <param name="expiresUtc">The UTC expiration time. When <see langword="null"/>, the token expires after 60 minutes.</param>
    /// <returns>The serialized JWT token.</returns>
    public static string GenerateToken(
        string secretKey,
        string issuer,
        string audience,
        IEnumerable<Claim>? claims = null,
        DateTime? expiresUtc = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(secretKey);
        ArgumentException.ThrowIfNullOrWhiteSpace(issuer);
        ArgumentException.ThrowIfNullOrWhiteSpace(audience);

        var signingCredentials = new SigningCredentials(CreateSecurityKey(secretKey), SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            notBefore: DateTime.UtcNow,
            expires: expiresUtc ?? DateTime.UtcNow.AddMinutes(60),
            signingCredentials: signingCredentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    /// <summary>
    /// Generates a signed JWT bearer token from a key-value claim dictionary.
    /// </summary>
    /// <param name="secretKey">The symmetric signing secret.</param>
    /// <param name="issuer">The token issuer.</param>
    /// <param name="audience">The token audience.</param>
    /// <param name="claims">The token claims as name-value pairs.</param>
    /// <param name="expiresUtc">The UTC expiration time. When <see langword="null"/>, the token expires after 60 minutes.</param>
    /// <returns>The serialized JWT token.</returns>
    public static string GenerateToken(
        string secretKey,
        string issuer,
        string audience,
        IDictionary<string, string?> claims,
        DateTime? expiresUtc = null)
    {
        ArgumentNullException.ThrowIfNull(claims);

        return GenerateToken(
            secretKey,
            issuer,
            audience,
            claims.Where(entry => entry.Value is not null).Select(entry => new Claim(entry.Key, entry.Value!)),
            expiresUtc);
    }

    /// <summary>
    /// Validates a JWT bearer token and returns the authenticated principal.
    /// </summary>
    /// <param name="token">The token to validate.</param>
    /// <param name="secretKey">The symmetric signing secret.</param>
    /// <param name="issuer">The expected token issuer.</param>
    /// <param name="audience">The expected token audience.</param>
    /// <param name="validateLifetime"><see langword="true"/> to validate token expiration; otherwise, <see langword="false"/>.</param>
    /// <param name="clockSkew">The allowed clock skew. When <see langword="null"/>, no skew is allowed.</param>
    /// <returns>The validated claims principal.</returns>
    public static ClaimsPrincipal ValidateToken(
        string token,
        string secretKey,
        string issuer,
        string audience,
        bool validateLifetime = true,
        TimeSpan? clockSkew = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(token);
        ArgumentException.ThrowIfNullOrWhiteSpace(secretKey);
        ArgumentException.ThrowIfNullOrWhiteSpace(issuer);
        ArgumentException.ThrowIfNullOrWhiteSpace(audience);

        var handler = new JwtSecurityTokenHandler();
        var parameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = issuer,
            ValidateAudience = true,
            ValidAudience = audience,
            ValidateLifetime = validateLifetime,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = CreateSecurityKey(secretKey),
            ClockSkew = clockSkew ?? TimeSpan.Zero
        };

        return handler.ValidateToken(token, parameters, out _);
    }

    /// <summary>
    /// Creates a symmetric security key from the supplied secret.
    /// </summary>
    /// <param name="secretKey">The symmetric signing secret.</param>
    /// <returns>The security key used for signing and validation.</returns>
    public static SymmetricSecurityKey CreateSecurityKey(string secretKey)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(secretKey);

        var keyBytes = TryDecodeBase64(secretKey) ?? Encoding.UTF8.GetBytes(secretKey);
        if (keyBytes.Length < 32)
        {
            throw new ArgumentException("JWT symmetric keys must be at least 32 bytes.", nameof(secretKey));
        }

        return new SymmetricSecurityKey(keyBytes);
    }

    /// <summary>
    /// Generates a numeric verification code.
    /// </summary>
    /// <param name="digits">The number of digits to generate.</param>
    /// <returns>The generated numeric code.</returns>
    public static string GenerateNumericCode(int digits = 6)
    {
        if (digits <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(digits), digits, "The number of digits must be greater than zero.");
        }

        var builder = new StringBuilder(digits);
        for (var index = 0; index < digits; index++)
        {
            builder.Append(RandomNumberGenerator.GetInt32(0, 10));
        }

        return builder.ToString();
    }

    /// <summary>
    /// Compares two token strings using fixed-time comparison.
    /// </summary>
    /// <param name="left">The first token.</param>
    /// <param name="right">The second token.</param>
    /// <param name="encoding">The text encoding to use. When <see langword="null"/>, UTF-8 is used.</param>
    /// <returns><see langword="true"/> when the tokens are equal; otherwise, <see langword="false"/>.</returns>
    public static bool FixedTimeEquals(string left, string right, Encoding? encoding = null)
    {
        ArgumentNullException.ThrowIfNull(left);
        ArgumentNullException.ThrowIfNull(right);

        var textEncoding = encoding ?? Encoding.UTF8;
        var leftBytes = textEncoding.GetBytes(left);
        var rightBytes = textEncoding.GetBytes(right);

        return CryptographicOperations.FixedTimeEquals(leftBytes, rightBytes);
    }

    /// <summary>
    /// Hashes an opaque token so it can be stored without persisting the raw value.
    /// </summary>
    /// <param name="token">The raw token.</param>
    /// <param name="encoding">The text encoding to use. When <see langword="null"/>, UTF-8 is used.</param>
    /// <returns>The token hash as a lowercase hexadecimal string.</returns>
    public static string HashToken(string token, Encoding? encoding = null)
    {
        ArgumentNullException.ThrowIfNull(token);

        return HashingUtility.ComputeSha256(token, encoding);
    }

    /// <summary>
    /// Verifies a raw token against a previously stored token hash.
    /// </summary>
    /// <param name="hashedToken">The stored token hash.</param>
    /// <param name="providedToken">The raw token to verify.</param>
    /// <param name="encoding">The text encoding to use. When <see langword="null"/>, UTF-8 is used.</param>
    /// <returns><see langword="true"/> when the token matches the stored hash; otherwise, <see langword="false"/>.</returns>
    public static bool VerifyHashedToken(string hashedToken, string providedToken, Encoding? encoding = null)
    {
        ArgumentNullException.ThrowIfNull(hashedToken);
        ArgumentNullException.ThrowIfNull(providedToken);

        var hashedProvidedToken = HashToken(providedToken, encoding);
        return FixedTimeEquals(hashedToken, hashedProvidedToken, Encoding.UTF8);
    }

    private static byte[]? TryDecodeBase64(string value)
    {
        try
        {
            return Convert.FromBase64String(value);
        }
        catch (FormatException)
        {
            return null;
        }
    }
}