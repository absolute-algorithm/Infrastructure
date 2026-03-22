using Microsoft.AspNetCore.Identity;

namespace FileFormula.Api.Infrastructure.Utilities;

/// <summary>
/// Provides helper methods for hashing and verifying passwords.
/// </summary>
/// <remarks>
/// This utility uses ASP.NET Core's password hashing format, which applies a per-password salt and a work factor suitable for password storage.
/// </remarks>
public static class PasswordUtility
{
    private static readonly object PasswordHasherUser = new();
    private static readonly PasswordHasher<object> PasswordHasher = new();

    /// <summary>
    /// Hashes a plaintext password for storage.
    /// </summary>
    /// <param name="password">The plaintext password.</param>
    /// <returns>The hashed password payload ready for storage.</returns>
    public static string HashPassword(string password)
    {
        ArgumentNullException.ThrowIfNull(password);

        return PasswordHasher.HashPassword(PasswordHasherUser, password);
    }

    /// <summary>
    /// Verifies a plaintext password against a previously stored password hash.
    /// </summary>
    /// <param name="hashedPassword">The stored password hash.</param>
    /// <param name="providedPassword">The plaintext password to verify.</param>
    /// <returns><see langword="true"/> when the password matches; otherwise, <see langword="false"/>.</returns>
    public static bool VerifyPassword(string hashedPassword, string providedPassword)
    {
        ArgumentNullException.ThrowIfNull(hashedPassword);
        ArgumentNullException.ThrowIfNull(providedPassword);


        PasswordVerificationResult result;

        try
        {
            result = PasswordHasher.VerifyHashedPassword(PasswordHasherUser, hashedPassword, providedPassword);
            return result is PasswordVerificationResult.Success or PasswordVerificationResult.SuccessRehashNeeded;
        }
        catch
        {
            return false;
        }
    }
}