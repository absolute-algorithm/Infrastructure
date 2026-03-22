using System.Text.Json.Serialization;

namespace FileFormula.Api.Infrastructure.Models.Pagination;

/// <summary>
/// Represents a standard paged query request.
/// </summary>
public class PagedRequest
{
    /// <summary>
    /// Gets the 1-based page number.
    /// </summary>
    [JsonPropertyName("pageNumber")]
    public int PageNumber { get; init; } = 1;

    /// <summary>
    /// Gets the page size.
    /// </summary>
    [JsonPropertyName("pageSize")]
    public int PageSize { get; init; } = 25;

    /// <summary>
    /// Gets the free-text search term.
    /// </summary>
    [JsonPropertyName("searchTerm")]
    public string? SearchTerm { get; init; }

    /// <summary>
    /// Gets the sort instructions.
    /// </summary>
    [JsonPropertyName("sorts")]
    public IReadOnlyList<SortDescriptor> Sorts { get; init; } = Array.Empty<SortDescriptor>();

    /// <summary>
    /// Gets the filter instructions.
    /// </summary>
    [JsonPropertyName("filters")]
    public IReadOnlyList<FilterDescriptor> Filters { get; init; } = Array.Empty<FilterDescriptor>();

    /// <summary>
    /// Gets the zero-based row offset derived from the page settings.
    /// </summary>
    [JsonIgnore]
    public int Offset => Math.Max(0, (Math.Max(1, PageNumber) - 1) * Math.Max(1, PageSize));
}