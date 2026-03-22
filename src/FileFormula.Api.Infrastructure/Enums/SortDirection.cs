namespace FileFormula.Api.Infrastructure.Enums;

/// <summary>
/// Specifies the direction used for sorting query results.
/// </summary>
public enum SortDirection : byte
{
    /// <summary>
    /// Sorts values in ascending order.
    /// </summary>
    Ascending = 1,

    /// <summary>
    /// Sorts values in descending order.
    /// </summary>
    Descending = 2
}