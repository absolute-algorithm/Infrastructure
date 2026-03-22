using System.Text.Json.Serialization;
using FileFormula.Api.Infrastructure.Enums;

namespace FileFormula.Api.Infrastructure.Models.Pagination;

/// <summary>
/// Represents a sort instruction for paged queries.
/// </summary>
public class SortDescriptor
{
    /// <summary>
    /// Gets the property or field name to sort by.
    /// </summary>
    [JsonPropertyName("field")]
    public string Field { get; init; } = string.Empty;

    /// <summary>
    /// Gets the sort direction.
    /// </summary>
    [JsonPropertyName("direction")]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public SortDirection Direction { get; init; } = SortDirection.Ascending;
}