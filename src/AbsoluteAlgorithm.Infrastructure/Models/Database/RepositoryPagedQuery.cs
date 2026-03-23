namespace AbsoluteAlgorithm.Infrastructure.Models.Database;

/// <summary>
/// Describes a paged repository query that can safely apply filtering, sorting, and full-text style search.
/// </summary>
public class RepositoryPagedQuery
{
    /// <summary>
    /// Gets the base SQL query used to retrieve the page items.
    /// </summary>
    /// <remarks>
    /// Provide a complete <c>SELECT</c> statement without the final paging clause. Additional filter, search, and order-by fragments are appended automatically.
    /// </remarks>
    public string SelectSql { get; init; } = string.Empty;

    /// <summary>
    /// Gets the base SQL query used to count all matching records.
    /// </summary>
    /// <remarks>
    /// Provide a complete <c>SELECT COUNT(...)</c> statement that matches the same source tables and joins as <see cref="SelectSql"/>.
    /// </remarks>
    public string CountSql { get; init; } = string.Empty;

    /// <summary>
    /// Gets the default order-by fragment used when the request does not provide any sort instructions.
    /// </summary>
    /// <remarks>
    /// The value may be supplied either as a bare expression such as <c>updatedat DESC</c> or a full <c>ORDER BY</c> clause.
    /// </remarks>
    public string? DefaultOrderBy { get; init; }

    /// <summary>
    /// Gets the set of client sort fields mapped to trusted SQL column expressions.
    /// </summary>
    public IReadOnlyDictionary<string, string> SortColumns { get; init; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Gets the set of client filter fields mapped to trusted SQL column expressions.
    /// </summary>
    public IReadOnlyDictionary<string, string> FilterColumns { get; init; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Gets the trusted SQL column expressions that participate in free-text search.
    /// </summary>
    public IReadOnlyList<string> SearchColumns { get; init; } = Array.Empty<string>();
}