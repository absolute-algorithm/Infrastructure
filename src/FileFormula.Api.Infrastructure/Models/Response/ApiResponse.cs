using System.Text.Json.Serialization;

namespace FileFormula.Api.Infrastructure.Models.Response;

/// <summary>
/// Represents the standard API response envelope.
/// </summary>
/// <typeparam name="T">The type of the response payload.</typeparam>
public class ApiResponse<T> where T : class
{
    /// <summary>
    /// Gets a value indicating whether the operation succeeded.
    /// </summary>
    [JsonPropertyName("isSuccess")]
    public bool IsSuccess { get; init; } = true;

    /// <summary>
    /// Gets the response payload.
    /// </summary>
    [JsonPropertyName("data")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public T? Data { get; init; }

    /// <summary>
    /// Gets the error payload.
    /// </summary>
    [JsonPropertyName("error")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public ErrorResponse? Error { get; init; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ApiResponse{T}"/> class.
    /// </summary>
    /// <param name="IsSuccess">A value indicating whether the operation succeeded.</param>
    /// <param name="Data">The response payload.</param>
    /// <param name="Error">The error payload.</param>
    public ApiResponse(bool IsSuccess = true, T? Data = null, ErrorResponse? Error = null)
    {
        this.IsSuccess = IsSuccess;
        this.Data = Data;
        this.Error = Error;
    }
}
