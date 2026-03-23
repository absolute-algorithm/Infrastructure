using System.Text.Json.Serialization;
using AbsoluteAlgorithm.Infrastructure.Enums;

namespace AbsoluteAlgorithm.Infrastructure.Models.Pagination;

/// <summary>
/// Represents a filter instruction for paged queries.
/// </summary>
public class FilterDescriptor
{
    /// <summary>
    /// Gets the property or field name to filter.
    /// </summary>
    [JsonPropertyName("field")]
    public string Field { get; init; } = string.Empty;

    /// <summary>
    /// Gets the comparison operator.
    /// </summary>
    [JsonPropertyName("operator")]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public FilterOperator Operator { get; init; } = FilterOperator.Equals;

    /// <summary>
    /// Gets the primary comparison value.
    /// </summary>
    [JsonPropertyName("value")]
    public string? Value { get; init; }

    /// <summary>
    /// Gets the additional comparison values.
    /// </summary>
    [JsonPropertyName("values")]
    public IReadOnlyList<string>? Values { get; init; }
}