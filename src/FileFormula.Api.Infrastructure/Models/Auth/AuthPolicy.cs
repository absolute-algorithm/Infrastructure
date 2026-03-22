using System.Text.Json.Serialization;

namespace FileFormula.Api.Infrastructure.Models.Auth;

/// <summary>
/// Represents a named authorization policy.
/// </summary>
public class AuthPolicy
{
    /// <summary>
    /// Gets or sets the policy name.
    /// </summary>
    [JsonPropertyName("policyName")]
    public string PolicyName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the roles required by the policy.
    /// </summary>
    [JsonPropertyName("requiredRoles")]
    public List<string> RequiredRoles { get; set; } = new();

    /// <summary>
    /// Gets or sets the claims required by the policy.
    /// </summary>
    [JsonPropertyName("requiredClaims")]
    public Dictionary<string, string> RequiredClaims { get; set; } = new();
}