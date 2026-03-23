using System;

namespace AbsoluteAlgorithm.Infrastructure.Constraints;

/// <summary>
/// Provides keys used by the library to store database state in the current HTTP request.
/// </summary>
public static class DATABASE
{
    /// <summary>
    /// Gets the <see cref="Microsoft.AspNetCore.Http.HttpContext.Items"/> key that stores request-scoped database connections and transactions.
    /// </summary>
    public const string CONNECTIONSKEY = "DB_CONNECTIONS";
}
