namespace AbsoluteAlgorithm.Infrastructure.Models.Database;

/// <summary>
/// Represents the outcome of a successful optimistic-concurrency update.
/// </summary>
public class RepositoryOptimisticUpdateResult
{
    /// <summary>
    /// Gets the number of rows affected by the update.
    /// </summary>
    public int RowsAffected { get; init; }

    /// <summary>
    /// Gets the latest version token after the update completes.
    /// </summary>
    public string? CurrentVersionToken { get; init; }

    /// <summary>
    /// Gets the latest strong ETag derived from <see cref="CurrentVersionToken"/>.
    /// </summary>
    public string? CurrentEtag { get; init; }
}