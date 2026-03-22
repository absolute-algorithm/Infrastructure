namespace FileFormula.Api.Infrastructure.Models.Database;

/// <summary>
/// Describes the SQL statements required to perform an optimistic-concurrency update through <see cref="Infrastructure.Database.Repository"/>.
/// </summary>
public class RepositoryOptimisticUpdateDefinition
{
    /// <summary>
    /// Gets the logical resource name used in generated error messages.
    /// </summary>
    public string ResourceName { get; init; } = "resource";

    /// <summary>
    /// Gets the SQL update statement.
    /// </summary>
    /// <remarks>
    /// The update statement should include the caller's expected version token in its <c>WHERE</c> clause so a concurrent change yields zero affected rows.
    /// </remarks>
    public string UpdateSql { get; init; } = string.Empty;

    /// <summary>
    /// Gets the SQL statement used to determine whether the target resource currently exists.
    /// </summary>
    /// <remarks>
    /// The statement should return a scalar boolean-compatible value.
    /// </remarks>
    public string? ExistsSql { get; init; }

    /// <summary>
    /// Gets the SQL statement used to read the current version token for the target resource.
    /// </summary>
    /// <remarks>
    /// The statement should return a scalar string-compatible value such as a row-version token or incrementing version number.
    /// </remarks>
    public string? CurrentVersionSql { get; init; }

    /// <summary>
    /// Gets a value indicating whether the current HTTP request must include a matching <c>If-Match</c> header before the update is attempted.
    /// </summary>
    public bool RequireIfMatchHeader { get; init; }
}