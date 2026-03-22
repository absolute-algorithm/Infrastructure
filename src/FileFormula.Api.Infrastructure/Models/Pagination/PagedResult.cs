using System.Text.Json.Serialization;

namespace FileFormula.Api.Infrastructure.Models.Pagination;

/// <summary>
/// Represents a paged result set.
/// </summary>
/// <typeparam name="T">The item type.</typeparam>
public class PagedResult<T>
{
    /// <summary>
    /// Gets the returned items.
    /// </summary>
    [JsonPropertyName("items")]
    public IReadOnlyList<T> Items { get; init; } = Array.Empty<T>();

    /// <summary>
    /// Gets the total number of matching records.
    /// </summary>
    [JsonPropertyName("totalCount")]
    public long TotalCount { get; init; }

    /// <summary>
    /// Gets the 1-based page number.
    /// </summary>
    [JsonPropertyName("pageNumber")]
    public int PageNumber { get; init; }

    /// <summary>
    /// Gets the page size.
    /// </summary>
    [JsonPropertyName("pageSize")]
    public int PageSize { get; init; }

    /// <summary>
    /// Gets the total page count.
    /// </summary>
    [JsonPropertyName("totalPages")]
    public int TotalPages => PageSize <= 0 ? 0 : (int)Math.Ceiling(TotalCount / (double)PageSize);

    /// <summary>
    /// Gets a value indicating whether a previous page exists.
    /// </summary>
    [JsonPropertyName("hasPreviousPage")]
    public bool HasPreviousPage => PageNumber > 1;

    /// <summary>
    /// Gets a value indicating whether a next page exists.
    /// </summary>
    [JsonPropertyName("hasNextPage")]
    public bool HasNextPage => PageNumber < TotalPages;
}