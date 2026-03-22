using System.Text.Json.Serialization;
using FileFormula.Api.Infrastructure.Models.Resilience;

namespace FileFormula.Api.Infrastructure.Models.Http;

/// <summary>
/// Represents the configuration for a named HTTP client registration.
/// </summary>
public class HttpClientPolicy
{
    /// <summary>
    /// Gets the name used to register and resolve the HTTP client.
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; init; } = null!;

    /// <summary>
    /// Gets the optional base address.
    /// </summary>
    [JsonPropertyName("baseAddress")]
    public string? BaseAddress { get; init; }

    /// <summary>
    /// Gets the timeout value, in seconds, applied to the HTTP client instance.
    /// </summary>
    [JsonPropertyName("timeoutSeconds")]
    public int TimeoutSeconds { get; init; } = 100;

    /// <summary>
    /// Gets the default headers applied to outgoing requests.
    /// </summary>
    [JsonPropertyName("defaultHeaders")]
    public Dictionary<string, string>? DefaultHeaders { get; init; }

    /// <summary>
    /// Gets the resilience configuration for the HTTP client.
    /// </summary>
    [JsonPropertyName("resiliencePolicy")]
    public ResiliencePolicy? ResiliencePolicy { get; init; }
}