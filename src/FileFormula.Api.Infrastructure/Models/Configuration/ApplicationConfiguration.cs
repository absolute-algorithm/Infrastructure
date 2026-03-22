using System;
using System.Text.Json.Serialization;
using FileFormula.Api.Infrastructure.Models.Auth;
using FileFormula.Api.Infrastructure.Models.Configuration;
using FileFormula.Api.Infrastructure.Models.Database;
using FileFormula.Api.Infrastructure.Models.Documentation;
using FileFormula.Api.Infrastructure.Models.Http;
using FileFormula.Api.Infrastructure.Models.Idempotency;
using FileFormula.Api.Infrastructure.Models.Pagination;
using FileFormula.Api.Infrastructure.Models.RateLimit;
using FileFormula.Api.Infrastructure.Models.Storage;
using FileFormula.Api.Infrastructure.Models.Webhooks;

namespace FileFormula.Api.Infrastructure.Models.Configuration;

/// <summary>
/// Represents the top-level configuration used to enable and configure library features.
/// </summary>
public class ApplicationConfiguration
{
    // --- Relational DB ---
    /// <summary>
    /// Gets a value indicating whether relational database services and middleware are enabled.
    /// </summary>
    [JsonPropertyName("enableRelationalDatabase")]
    public bool EnableRelationalDatabase { get; init; } = false;

    /// <summary>
    /// Gets the database policies to register.
    /// </summary>
    [JsonPropertyName("databasePolicies")]
    public List<DatabasePolicy>? DatabasePolicies { get; init; }

    // --- Storage ---
    /// <summary>
    /// Gets a value indicating whether object storage services are enabled.
    /// </summary>
    [JsonPropertyName("enableStorage")]
    public bool EnableStorage { get; init; } = false;

    /// <summary>
    /// Gets the storage policies to register.
    /// </summary>
    [JsonPropertyName("storagePolicies")]
    public List<StoragePolicy>? StoragePolicies { get; init; }

    // --- HttpClient ---

    /// <summary>
    /// Gets the named HTTP client policies to register.
    /// </summary>
    [JsonPropertyName("httpClientPolicies")]
    public List<HttpClientPolicy>? HttpClientPolicies { get; init; }

    /// <summary>
    /// Gets a value indicating whether API versioning is enabled.
    /// </summary>
    [JsonPropertyName("enableApiVersioning")]
    public bool EnableApiVersioning { get; init; } = false;

    /// <summary>
    /// Gets the API versioning configuration.
    /// </summary>
    [JsonPropertyName("apiVersioningPolicy")]
    public ApiVersioningPolicy? ApiVersioningPolicy { get; init; }

    /// <summary>
    /// Gets a value indicating whether Swagger and OpenAPI endpoints are enabled.
    /// </summary>
    [JsonPropertyName("enableSwagger")]
    public bool EnableSwagger { get; init; } = false;

    /// <summary>
    /// Gets the Swagger and OpenAPI documentation configuration.
    /// </summary>
    [JsonPropertyName("swaggerPolicy")]
    public SwaggerPolicy? SwaggerPolicy { get; init; }


    /// <summary>
    /// Gets a value indicating whether request idempotency is enabled.
    /// </summary>
    [JsonPropertyName("enableIdempotency")]
    public bool EnableIdempotency { get; init; } = false;

    /// <summary>
    /// Gets the request idempotency configuration.
    /// </summary>
    [JsonPropertyName("idempotencyPolicy")]
    public IdempotencyPolicy? IdempotencyPolicy { get; init; }

    // --- Auth Sections ---

    /// <summary>
    /// Gets a value indicating whether authentication services are enabled.
    /// </summary>
    [JsonPropertyName("configureAuthentication")]
    public bool ConfigureAuthentication { get; init; } = false;

    /// <summary>
    /// Gets a value indicating whether authorization services are enabled.
    /// </summary>
    [JsonPropertyName("configureAuthorization")]
    public bool ConfigureAuthorization { get; init; } = false;

    /// <summary>
    /// Gets the authentication and authorization manifest.
    /// </summary>
    [JsonPropertyName("authManifest")]
    public AuthManifest? AuthManifest { get; init; }

    /// <summary>
    /// Gets a value indicating whether webhook signature validation is enabled.
    /// </summary>
    [JsonPropertyName("enableWebhookSignatureValidation")]
    public bool EnableWebhookSignatureValidation { get; init; } = false;

    /// <summary>
    /// Gets the webhook signature validation policies.
    /// </summary>
    [JsonPropertyName("webhookSignaturePolicies")]
    public List<WebhookSignaturePolicy>? WebhookSignaturePolicies { get; init; }

    // --- RateLimit ---

    /// <summary>
    /// Gets a value indicating whether rate limiting is enabled.
    /// </summary>
    [JsonPropertyName("enableRateLimit")]
    public bool EnableRateLimit { get; init; } = false;

    /// <summary>
    /// Gets the rate-limit policies to register.
    /// </summary>
    [JsonPropertyName("rateLimitPolicies")]
    public List<RateLimitPolicy>? RateLimitPolicies { get; init; }


    /// <summary>
    /// Gets a value indicating whether health endpoints are enabled.
    /// </summary>
    [JsonPropertyName("enableHealthChecks")]
    public bool EnableHealthChecks { get; init; } = true;
}
