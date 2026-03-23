using System.Text.Json.Serialization;

namespace AbsoluteAlgorithm.Infrastructure.Models.Response;

/// <summary>
/// Represents an error payload returned by the API.
/// </summary>
public class ErrorResponse
{
    /// <summary>
    /// Gets the application-specific error code.
    /// </summary>
    [JsonPropertyName("errorCode")]
    public string ErrorCode { get; init; } = null!;

    /// <summary>
    /// Gets the error message.
    /// </summary>
    [JsonPropertyName("errorMessage")]
    public string ErrorMessage { get; init; } = null!;

    /// <summary>
    /// Gets the field-specific validation errors when additional detail is available.
    /// </summary>
    [JsonPropertyName("validationErrors")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IReadOnlyList<ValidationErrorDetail>? ValidationErrors { get; init; }
}

/// <summary>
/// Represents validation messages associated with a specific field or input path.
/// </summary>
public class ValidationErrorDetail
{
    /// <summary>
    /// Gets the field name or input path associated with the error.
    /// </summary>
    [JsonPropertyName("field")]
    public string Field { get; init; } = null!;

    /// <summary>
    /// Gets the validation messages for the field.
    /// </summary>
    [JsonPropertyName("messages")]
    public IReadOnlyList<string> Messages { get; init; } = Array.Empty<string>();
}