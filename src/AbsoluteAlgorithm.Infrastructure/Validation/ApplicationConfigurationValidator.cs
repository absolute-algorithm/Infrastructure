using AbsoluteAlgorithm.Infrastructure.Models.Configuration;
using AbsoluteAlgorithm.Infrastructure.Models.Documentation;
using AbsoluteAlgorithm.Infrastructure.Models.Resilience;

namespace AbsoluteAlgorithm.Infrastructure.Validation;

/// <summary>
/// Validates library configuration before service registration begins.
/// </summary>
public static class ApplicationConfigurationValidator
{
    /// <summary>
    /// Validates the supplied application configuration and throws when the configuration is invalid.
    /// </summary>
    /// <param name="configuration">The application configuration to validate.</param>
    public static void ValidateOrThrow(ApplicationConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        var errors = new List<string>();

        ValidateDatabasePolicies(configuration, errors);
        ValidateStoragePolicies(configuration, errors);
        ValidateHttpClientPolicies(configuration, errors);
        ValidateApiVersioning(configuration, errors);
        ValidateSwagger(configuration, errors);
        ValidateAuthentication(configuration, errors);
        ValidateRateLimits(configuration, errors);
        ValidateIdempotency(configuration, errors);
        ValidateWebhookPolicies(configuration, errors);

        if (errors.Count > 0)
        {
            throw new InvalidOperationException($"Application configuration is invalid:{Environment.NewLine}- {string.Join(Environment.NewLine + "- ", errors)}");
        }
    }

    private static void ValidateDatabasePolicies(ApplicationConfiguration configuration, List<string> errors)
    {
        if (!configuration.EnableRelationalDatabase)
        {
            return;
        }

        if (configuration.DatabasePolicies is null || configuration.DatabasePolicies.Count == 0)
        {
            errors.Add("UseRelationalDatabase is enabled but no database policies were provided.");
            return;
        }

        var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var policy in configuration.DatabasePolicies)
        {
            if (string.IsNullOrWhiteSpace(policy.Name))
            {
                errors.Add("Each database policy must define a non-empty name.");
            }
            else if (!names.Add(policy.Name))
            {
                errors.Add($"Duplicate database policy name '{policy.Name}'.");
            }

            if (string.IsNullOrWhiteSpace(policy.ConnectionStringName))
            {
                errors.Add($"Database policy '{policy.Name}' must define a connectionStringName.");
            }
            else if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(policy.ConnectionStringName)))
            {
                errors.Add($"Database policy '{policy.Name}' references missing environment variable '{policy.ConnectionStringName}'.");
            }

            if (policy.MinPoolSize < 0)
            {
                errors.Add($"Database policy '{policy.Name}' must use a non-negative minPoolSize.");
            }

            if (policy.MaxPoolSize <= 0 || policy.MaxPoolSize < policy.MinPoolSize)
            {
                errors.Add($"Database policy '{policy.Name}' must use a maxPoolSize greater than zero and greater than or equal to minPoolSize.");
            }

            if (policy.CommandTimeoutSeconds <= 0)
            {
                errors.Add($"Database policy '{policy.Name}' must use a commandTimeoutSeconds value greater than zero.");
            }

            ValidateResilience($"Database policy '{policy.Name}'", policy.ResiliencePolicy, errors);
        }
    }

    private static void ValidateStoragePolicies(ApplicationConfiguration configuration, List<string> errors)
    {
        if (!configuration.EnableStorage)
        {
            return;
        }

        if (configuration.StoragePolicies is null || configuration.StoragePolicies.Count == 0)
        {
            errors.Add("UseStorage is enabled but no storage policies were provided.");
            return;
        }

        var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var policy in configuration.StoragePolicies)
        {
            if (string.IsNullOrWhiteSpace(policy.Name))
            {
                errors.Add("Each storage policy must define a non-empty name.");
            }
            else if (!names.Add(policy.Name))
            {
                errors.Add($"Duplicate storage policy name '{policy.Name}'.");
            }

            if (string.IsNullOrWhiteSpace(policy.ConnectionStringName))
            {
                errors.Add($"Storage policy '{policy.Name}' must define a connectionStringName.");
            }
            else if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(policy.ConnectionStringName)))
            {
                errors.Add($"Storage policy '{policy.Name}' references missing environment variable '{policy.ConnectionStringName}'.");
            }

            if (string.IsNullOrWhiteSpace(policy.BucketName))
            {
                errors.Add($"Storage policy '{policy.Name}' must define a bucketName.");
            }

            if (policy.StorageProvider == Enums.StorageProvider.GoogleCloud && string.IsNullOrWhiteSpace(policy.GcpProjectId))
            {
                errors.Add($"Google Cloud storage policy '{policy.Name}' must define gcpProjectId.");
            }
        }
    }

    private static void ValidateHttpClientPolicies(ApplicationConfiguration configuration, List<string> errors)
    {
        if (configuration.HttpClientPolicies is null || configuration.HttpClientPolicies.Count == 0)
        {
            return;
        }

        var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var policy in configuration.HttpClientPolicies)
        {
            if (string.IsNullOrWhiteSpace(policy.Name))
            {
                errors.Add("Each HTTP client policy must define a non-empty name.");
            }
            else if (!names.Add(policy.Name))
            {
                errors.Add($"Duplicate HTTP client policy name '{policy.Name}'.");
            }

            if (!string.IsNullOrWhiteSpace(policy.BaseAddress) && !Uri.TryCreate(policy.BaseAddress, UriKind.Absolute, out _))
            {
                errors.Add($"HTTP client policy '{policy.Name}' must use an absolute baseAddress.");
            }

            if (policy.TimeoutSeconds <= 0)
            {
                errors.Add($"HTTP client policy '{policy.Name}' must use a timeoutSeconds value greater than zero.");
            }

            ValidateResilience($"HTTP client policy '{policy.Name}'", policy.ResiliencePolicy, errors);
        }
    }

    private static void ValidateAuthentication(ApplicationConfiguration configuration, List<string> errors)
    {
        if (configuration.ConfigureAuthentication)
        {
            var authManifest = configuration.AuthManifest;
            var enableJwt = authManifest?.EnableJwt ?? true;
            var enableCookies = authManifest?.EnableCookies ?? true;

            if (!enableJwt && !enableCookies)
            {
                errors.Add("Authentication configuration must enable at least one of JWT or cookie authentication.");
            }

            if (enableJwt)
            {
                var secret = Environment.GetEnvironmentVariable("JWT_SECRET");
                if (string.IsNullOrWhiteSpace(secret) || secret.Length < 32)
                {
                    errors.Add("JWT authentication requires JWT_SECRET to be present and at least 32 characters long.");
                }
            }

            if (authManifest?.EnableCsrfProtection == true && !enableCookies)
            {
                errors.Add("CSRF protection can only be enabled when cookie authentication is enabled.");
            }
        }

        if (configuration.ConfigureAuthorization && configuration.AuthManifest?.Policies is not null)
        {
            var policyNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var policy in configuration.AuthManifest.Policies)
            {
                if (string.IsNullOrWhiteSpace(policy.PolicyName))
                {
                    errors.Add("Authorization policies must define a non-empty policyName.");
                    continue;
                }

                if (!policyNames.Add(policy.PolicyName))
                {
                    errors.Add($"Duplicate authorization policy name '{policy.PolicyName}'.");
                }
            }
        }
    }

    private static void ValidateApiVersioning(ApplicationConfiguration configuration, List<string> errors)
    {
        if (!configuration.EnableApiVersioning)
        {
            return;
        }

        var policy = configuration.ApiVersioningPolicy ?? throw new InvalidOperationException("ApiVersioningPolicy must be provided when API versioning is enabled.");

        if (policy.DefaultMajorVersion < 0)
        {
            errors.Add("ApiVersioningPolicy must use a non-negative defaultMajorVersion.");
        }

        if (policy.DefaultMinorVersion < 0)
        {
            errors.Add("ApiVersioningPolicy must use a non-negative defaultMinorVersion.");
        }

        if (policy.Readers.Count == 0)
        {
            errors.Add("ApiVersioningPolicy must define at least one reader.");
        }

        if (policy.Readers.Contains(Enums.ApiVersionReaderType.QueryString)
            && string.IsNullOrWhiteSpace(policy.QueryStringParameterName))
        {
            errors.Add("ApiVersioningPolicy must define a non-empty queryStringParameterName when query-string versioning is enabled.");
        }

        if (policy.Readers.Contains(Enums.ApiVersionReaderType.Header)
            && (policy.HeaderNames.Count == 0 || policy.HeaderNames.Any(string.IsNullOrWhiteSpace)))
        {
            errors.Add("ApiVersioningPolicy must define at least one non-empty header name when header versioning is enabled.");
        }

        if (policy.Readers.Contains(Enums.ApiVersionReaderType.MediaType)
            && string.IsNullOrWhiteSpace(policy.MediaTypeParameterName))
        {
            errors.Add("ApiVersioningPolicy must define a non-empty mediaTypeParameterName when media-type versioning is enabled.");
        }

        if (policy.EnableApiExplorer && string.IsNullOrWhiteSpace(policy.GroupNameFormat))
        {
            errors.Add("ApiVersioningPolicy must define a non-empty groupNameFormat when the API explorer is enabled.");
        }
    }

    private static void ValidateSwagger(ApplicationConfiguration configuration, List<string> errors)
    {
        if (!configuration.EnableSwagger)
        {
            return;
        }

        var policy = configuration.SwaggerPolicy ?? throw new InvalidOperationException("SwaggerPolicy must be provided when Swagger is enabled.");

        if (string.IsNullOrWhiteSpace(policy.Title))
        {
            errors.Add("SwaggerPolicy must define a non-empty title.");
        }

        if (string.IsNullOrWhiteSpace(policy.OpenApiPath))
        {
            errors.Add("SwaggerPolicy must define a non-empty openApiPath.");
        }

        if (policy.UseSwaggerUi && string.IsNullOrWhiteSpace(policy.SwaggerUiPath))
        {
            errors.Add("SwaggerPolicy must define a non-empty swaggerUiPath when Swagger UI is enabled.");
        }

        ValidateSwaggerHeaders(policy.Headers, errors);

        switch (policy.DocumentMode)
        {
            case Enums.SwaggerDocumentMode.Single:
                if (string.IsNullOrWhiteSpace(policy.SingleDocumentName))
                {
                    errors.Add("SwaggerPolicy must define a non-empty singleDocumentName when single-document mode is used.");
                }

                if (string.IsNullOrWhiteSpace(policy.SingleDocumentVersion))
                {
                    errors.Add("SwaggerPolicy must define a non-empty singleDocumentVersion when single-document mode is used.");
                }

                break;

            case Enums.SwaggerDocumentMode.PerApiVersion:
                if (!configuration.EnableApiVersioning)
                {
                    errors.Add("SwaggerPolicy per-api-version mode requires API versioning to be enabled.");
                }

                if (policy.Documents.Count == 0)
                {
                    errors.Add("SwaggerPolicy must define at least one document when per-api-version mode is used.");
                }

                var documentNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (var document in policy.Documents)
                {
                    if (string.IsNullOrWhiteSpace(document.DocumentName))
                    {
                        errors.Add("Each Swagger document must define a non-empty documentName.");
                    }
                    else if (!documentNames.Add(document.DocumentName))
                    {
                        errors.Add($"Duplicate Swagger document name '{document.DocumentName}'.");
                    }

                    if (string.IsNullOrWhiteSpace(document.ApiGroupName))
                    {
                        errors.Add($"Swagger document '{document.DocumentName}' must define a non-empty apiGroupName.");
                    }

                    if (string.IsNullOrWhiteSpace(document.Version))
                    {
                        errors.Add($"Swagger document '{document.DocumentName}' must define a non-empty version.");
                    }
                }

                break;
        }
    }

    private static void ValidateSwaggerHeaders(IReadOnlyList<SwaggerHeaderDefinition> headers, List<string> errors)
    {
        var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var header in headers)
        {
            if (string.IsNullOrWhiteSpace(header.Name))
            {
                errors.Add("Each Swagger header definition must define a non-empty name.");
                continue;
            }

            if (!names.Add(header.Name))
            {
                errors.Add($"Duplicate Swagger header definition '{header.Name}'.");
            }
        }
    }

    private static void ValidateRateLimits(ApplicationConfiguration configuration, List<string> errors)
    {
        if (!configuration.EnableRateLimit)
        {
            return;
        }

        if (configuration.RateLimitPolicies is null || configuration.RateLimitPolicies.Count == 0)
        {
            errors.Add("RateLimit is enabled but no rate-limit policies were provided.");
            return;
        }

        var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var policy in configuration.RateLimitPolicies)
        {
            if (string.IsNullOrWhiteSpace(policy.PolicyName))
            {
                errors.Add("Each rate-limit policy must define a non-empty policyName.");
            }
            else if (!names.Add(policy.PolicyName))
            {
                errors.Add($"Duplicate rate-limit policy name '{policy.PolicyName}'.");
            }

            if (policy.PermitLimit <= 0)
            {
                errors.Add($"Rate-limit policy '{policy.PolicyName}' must use a permitLimit greater than zero.");
            }

            if (policy.Window <= TimeSpan.Zero)
            {
                errors.Add($"Rate-limit policy '{policy.PolicyName}' must use a positive window.");
            }
        }
    }

    private static void ValidateIdempotency(ApplicationConfiguration configuration, List<string> errors)
    {
        if (!configuration.EnableIdempotency)
        {
            return;
        }

        var policy = configuration.IdempotencyPolicy ?? throw new InvalidOperationException("IdempotencyPolicy must be provided when idempotency is enabled.");

        if (string.IsNullOrWhiteSpace(policy.HeaderName))
        {
            errors.Add("IdempotencyPolicy must define a non-empty headerName.");
        }

        if (policy.ReplayableMethods.Count == 0)
        {
            errors.Add("IdempotencyPolicy must define at least one replayable method.");
        }

        if (policy.ExpirationMinutes <= 0)
        {
            errors.Add("IdempotencyPolicy must use an expirationMinutes value greater than zero.");
        }

        if (policy.MaximumResponseBodyBytes <= 0)
        {
            errors.Add("IdempotencyPolicy must use a maximumResponseBodyBytes value greater than zero.");
        }
    }

    private static void ValidateWebhookPolicies(ApplicationConfiguration configuration, List<string> errors)
    {
        if (!configuration.EnableWebhookSignatureValidation || configuration.WebhookSignaturePolicies is null || configuration.WebhookSignaturePolicies.Count == 0)
        {
            return;
        }

        var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var policy in configuration.WebhookSignaturePolicies)
        {
            if (string.IsNullOrWhiteSpace(policy.Name))
            {
                errors.Add("Each webhook signature policy must define a non-empty name.");
            }
            else if (!names.Add(policy.Name))
            {
                errors.Add($"Duplicate webhook signature policy name '{policy.Name}'.");
            }

            if (string.IsNullOrWhiteSpace(policy.PathPrefix))
            {
                errors.Add($"Webhook signature policy '{policy.Name}' must define a pathPrefix.");
            }

            if (string.IsNullOrWhiteSpace(policy.SecretName))
            {
                errors.Add($"Webhook signature policy '{policy.Name}' must define a secretName.");
            }
            else if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(policy.SecretName)))
            {
                errors.Add($"Webhook signature policy '{policy.Name}' references missing environment variable '{policy.SecretName}'.");
            }

            if (policy.AllowedClockSkewSeconds < 0)
            {
                errors.Add($"Webhook signature policy '{policy.Name}' must use a non-negative allowedClockSkewSeconds value.");
            }
        }
    }

    private static void ValidateResilience(string owner, ResiliencePolicy? policy, List<string> errors)
    {
        if (policy is null)
        {
            return;
        }

        if (policy.Retry is null && policy.Timeout is null && policy.CircuitBreaker is null)
        {
            errors.Add($"{owner} enables resilience but does not configure retry, timeout, or circuit breaker settings.");
        }

        if (policy.Retry is { } retry)
        {
            if (retry.MaxRetryAttempts <= 0)
            {
                errors.Add($"{owner} retry settings must use a maxRetryAttempts value greater than zero.");
            }

            if (retry.DelayMilliseconds <= 0)
            {
                errors.Add($"{owner} retry settings must use a delayMilliseconds value greater than zero.");
            }

            if (retry.DelayScheduleMilliseconds is not null && retry.DelayScheduleMilliseconds.Any(value => value <= 0))
            {
                errors.Add($"{owner} retry delay schedule values must all be greater than zero.");
            }
        }

        if (policy.Timeout is { } timeout && timeout.TimeoutSeconds <= 0)
        {
            errors.Add($"{owner} timeout settings must use a timeoutSeconds value greater than zero.");
        }

        if (policy.CircuitBreaker is { } circuitBreaker)
        {
            if (circuitBreaker.HandledEventsAllowedBeforeBreaking <= 0)
            {
                errors.Add($"{owner} circuit breaker settings must use a handledEventsAllowedBeforeBreaking value greater than zero.");
            }

            if (circuitBreaker.DurationOfBreakSeconds <= 0)
            {
                errors.Add($"{owner} circuit breaker settings must use a durationOfBreakSeconds value greater than zero.");
            }
        }
    }
}