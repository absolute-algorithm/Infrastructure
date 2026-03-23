using System;
using System.Security.Claims;
using System.Text;
using System.Threading.RateLimiting;
using Amazon.S3;
using Asp.Versioning;
using AbsoluteAlgorithm.Infrastructure.ResilienceFactories;
using AbsoluteAlgorithm.Infrastructure.Constraints;
using AbsoluteAlgorithm.Infrastructure.Database;
using AbsoluteAlgorithm.Infrastructure.Enums;
using AbsoluteAlgorithm.Infrastructure.Filters;
using AbsoluteAlgorithm.Infrastructure.Health;
using AbsoluteAlgorithm.Infrastructure.Models.Auth;
using AbsoluteAlgorithm.Infrastructure.Models.Configuration;
using AbsoluteAlgorithm.Infrastructure.Models.Database;
using AbsoluteAlgorithm.Infrastructure.Models.Documentation;
using AbsoluteAlgorithm.Infrastructure.Models.Http;
using AbsoluteAlgorithm.Infrastructure.Models.Idempotency;
using AbsoluteAlgorithm.Infrastructure.Models.RateLimit;
using AbsoluteAlgorithm.Infrastructure.Models.Response;
using AbsoluteAlgorithm.Infrastructure.Models.Storage;
using AbsoluteAlgorithm.Infrastructure.Models.Webhooks;
using AbsoluteAlgorithm.Infrastructure.OpenApi;
using AbsoluteAlgorithm.Infrastructure.Sanitizers;
using AbsoluteAlgorithm.Infrastructure.Services;
using AbsoluteAlgorithm.Infrastructure.Validation;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Minio;
using Minio.AspNetCore.HealthChecks;
using NLog.Web;
using NSwag;
using NSwag.Generation.Processors.Security;

namespace AbsoluteAlgorithm.Infrastructure.Extensions;

/// <summary>
/// Provides extension methods for registering library services.
/// </summary>
public static class WebApplicationBuilderExtensions
{
    /// <summary>
    /// Registers library services for the specified application configuration.
    /// </summary>
    /// <param name="builder">The application builder to configure.</param>
    /// <param name="appConfig">The application configuration.</param>
    /// <returns>The <paramref name="builder"/> instance.</returns>
    public static WebApplicationBuilder AddAbsoluteServices(this WebApplicationBuilder builder, ApplicationConfiguration appConfig)
    {
        ApplicationConfigurationValidator.ValidateOrThrow(appConfig);

        builder.Configuration.AddEnvironmentVariables();
        builder.Configuration.SetBasePath(AppContext.BaseDirectory);

        builder.Configuration.AddJsonFile("nlog.settings.json", optional: false, reloadOnChange: true);
        builder.Logging.ClearProviders();
        builder.Logging.SetMinimumLevel(Microsoft.Extensions.Logging.LogLevel.Information);
        builder.Host.UseNLog();

        builder.Services.AddHsts(options =>
        {
            options.Preload = true;
            options.IncludeSubDomains = true;
            options.MaxAge = TimeSpan.FromDays(60);
        });

        builder.Services.Configure<RouteOptions>(options =>
        {
            options.LowercaseUrls = true;
            options.LowercaseQueryStrings = true;
        });

        builder.Services.AddHttpClient();
        builder.Services.AddHttpContextAccessor();
        builder.Services.AddScoped<IRequestMetadataAccessor, HttpContextRequestMetadataAccessor>();
        builder.Services.AddScoped<ICurrentUserAccessor, HttpContextCurrentUserAccessor>();
        builder.Services.AddResponseCompression(options =>
        {
            options.EnableForHttps = true;
            options.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(new[] { "application/json" });
        });

        builder.Services.AddControllers(options =>
        {
            options.ModelBinderProviders.Insert(0, new SpreadsheetFormulaSanitizingStringModelBinderProvider());
            options.Filters.Add<ValidateModelFilter>();
        })
        .AddJsonOptions(options =>
        {
            options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
            options.JsonSerializerOptions.Converters.Add(new SpreadsheetFormulaSanitizingStringJsonConverter());
        })
        .ConfigureApiBehaviorOptions(options =>
        {
            options.SuppressModelStateInvalidFilter = true;
        });

        builder.Services.AddResponseCaching();

        if (appConfig.EnableApiVersioning)
        {
            builder.Services.AddAbsoluteApiVersioning(appConfig.ApiVersioningPolicy);
        }

        if (appConfig.EnableSwagger)
        {
            builder.Services.AddAbsoluteSwagger(appConfig);
        }

        if (appConfig.EnableRelationalDatabase)
        {
            builder.Services.AddAbsoluteDatabase(appConfig.DatabasePolicies);
        }

        if (appConfig.EnableStorage)
        {
            builder.Services.AddAbsoluteStorage(appConfig.StoragePolicies);
        }

        builder.Services.AddAbsoluteHttpClients(appConfig.HttpClientPolicies);

        if (appConfig.EnableRateLimit)
        {
            builder.Services.AddAbsoluteRateLimits(appConfig.RateLimitPolicies);
        }

        if (appConfig.ConfigureAuthentication)
        {
            builder.Services.AddAbsoluteAuthentication(appConfig.AuthManifest);
        }

        if (appConfig.ConfigureAuthorization)
        {
            builder.Services.AddAbsoluteAuthorization(appConfig.AuthManifest?.Policies);
        }

        if (appConfig.EnableIdempotency)
        {
            builder.Services.AddAbsoluteIdempotency(appConfig.IdempotencyPolicy);
        }

        if (appConfig.EnableWebhookSignatureValidation && appConfig.WebhookSignaturePolicies is not null)
        {
            builder.Services.AddAbsoluteWebhookSignatureValidation(appConfig.WebhookSignaturePolicies);
        }

        builder.Services.Configure<ForwardedHeadersOptions>(options =>
        {
            options.ForwardedHeaders = ForwardedHeaders.XForwardedProto | ForwardedHeaders.XForwardedFor;

            options.KnownIPNetworks.Clear();
            options.KnownProxies.Clear();
        });

        if (appConfig.EnableHealthChecks)
        {
            builder.Services.AddAbsoluteHealthChecks(appConfig.DatabasePolicies, appConfig.StoragePolicies);
        }

        return builder;
    }

    /// <summary>
    /// Registers request idempotency services.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="policy">The idempotency policy.</param>
    /// <returns>The <paramref name="services"/> instance.</returns>
    private static IServiceCollection AddAbsoluteIdempotency(this IServiceCollection services, IdempotencyPolicy? policy)
    {
        if (policy is null)
        {
            return services;
        }

        services.AddMemoryCache();
        services.AddSingleton(policy);
        services.AddSingleton<IIdempotencyStore, InMemoryIdempotencyStore>();
        return services;
    }

    /// <summary>
    /// Registers API versioning services.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="policy">The API versioning policy.</param>
    /// <returns>The <paramref name="services"/> instance.</returns>
    private static IServiceCollection AddAbsoluteApiVersioning(this IServiceCollection services, ApiVersioningPolicy? policy)
    {
        if (policy is null)
        {
            return services;
        }

        var builder = services
            .AddApiVersioning(options =>
            {
                options.DefaultApiVersion = new ApiVersion(policy.DefaultMajorVersion, policy.DefaultMinorVersion);
                options.AssumeDefaultVersionWhenUnspecified = policy.AssumeDefaultVersionWhenUnspecified;
                options.ReportApiVersions = policy.ReportApiVersions;
                options.ApiVersionReader = BuildApiVersionReader(policy);
            })
            .AddMvc();

        if (policy.EnableApiExplorer)
        {
            builder.AddApiExplorer(options =>
            {
                options.GroupNameFormat = policy.GroupNameFormat;
                options.SubstituteApiVersionInUrl = policy.SubstituteApiVersionInUrl;
            });
        }

        return services;
    }

    /// <summary>
    /// Registers NSwag OpenAPI generation and Swagger UI services.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The application configuration.</param>
    /// <returns>The <paramref name="services"/> instance.</returns>
    private static IServiceCollection AddAbsoluteSwagger(this IServiceCollection services, ApplicationConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        var swaggerPolicy = configuration.SwaggerPolicy ?? throw new InvalidOperationException("SwaggerPolicy is required when Swagger is enabled.");
        var documents = BuildSwaggerDocuments(swaggerPolicy);
        var headers = BuildSwaggerHeaders(swaggerPolicy, configuration.ApiVersioningPolicy);

        foreach (var document in documents)
        {
            services.AddOpenApiDocument((settings, _) =>
            {
                settings.DocumentName = document.DocumentName;
                settings.Title = document.Title;
                settings.Version = document.Version;
                settings.Description = document.Description;

                if (!string.IsNullOrWhiteSpace(document.ApiGroupName))
                {
                    settings.ApiGroupNames = [document.ApiGroupName];
                }

                if (configuration.ConfigureAuthentication && (configuration.AuthManifest?.EnableJwt ?? true))
                {
                    settings.AddSecurity("Bearer", new OpenApiSecurityScheme
                    {
                        Type = OpenApiSecuritySchemeType.Http,
                        Scheme = "bearer",
                        BearerFormat = "JWT",
                        Description = "Provide a bearer token in the Authorization header."
                    });
                    settings.OperationProcessors.Add(new AspNetCoreOperationSecurityScopeProcessor("Bearer"));
                }

                if (configuration.ConfigureAuthentication && configuration.AuthManifest?.EnableCookies == true)
                {
                    settings.AddSecurity("Cookie", new OpenApiSecurityScheme
                    {
                        Type = OpenApiSecuritySchemeType.ApiKey,
                        Name = CookieAuthenticationDefaults.CookiePrefix + CookieAuthenticationDefaults.AuthenticationScheme,
                        In = OpenApiSecurityApiKeyLocation.Cookie,
                        Description = "Provide the authentication cookie value."
                    });
                    settings.OperationProcessors.Add(new AspNetCoreOperationSecurityScopeProcessor("Cookie"));
                }

                settings.OperationProcessors.Add(new SwaggerAuthorizeKeyOperationProcessor());
                settings.OperationProcessors.Add(new SwaggerHeaderOperationProcessor(headers));
                settings.PostProcess = documentInfo =>
                {
                    documentInfo.Info.Title = document.Title;
                    documentInfo.Info.Version = document.Version;
                    documentInfo.Info.Description = document.Description;
                };
            });
        }

        return services;
    }

    /// <summary>
    /// Registers webhook request-signature validation services.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="policies">The webhook signature policies.</param>
    /// <returns>The <paramref name="services"/> instance.</returns>
    private static IServiceCollection AddAbsoluteWebhookSignatureValidation(this IServiceCollection services, IReadOnlyList<WebhookSignaturePolicy> policies)
    {
        ArgumentNullException.ThrowIfNull(policies);

        services.AddSingleton<IReadOnlyList<WebhookSignaturePolicy>>(policies.ToArray());
        return services;
    }

    private static IReadOnlyList<SwaggerDocumentRegistrationInfo> BuildSwaggerDocuments(SwaggerPolicy policy)
    {
        return policy.DocumentMode switch
        {
            SwaggerDocumentMode.Single =>
            [
                new SwaggerDocumentRegistrationInfo(
                    policy.SingleDocumentName,
                    ApiGroupName: null,
                    policy.SingleDocumentVersion,
                    policy.Title,
                    policy.Description)
            ],
            _ => policy.Documents
                .Select(document => new SwaggerDocumentRegistrationInfo(
                    document.DocumentName,
                    document.ApiGroupName,
                    document.Version,
                    document.Title ?? $"{policy.Title} {document.Version}",
                    document.Description ?? policy.Description))
                .ToArray()
        };
    }

    private static IReadOnlyList<SwaggerHeaderDefinition> BuildSwaggerHeaders(SwaggerPolicy swaggerPolicy, ApiVersioningPolicy? apiVersioningPolicy)
    {
        var headers = swaggerPolicy.Headers.ToList();

        if (apiVersioningPolicy is not null && apiVersioningPolicy.Readers.Contains(ApiVersionReaderType.Header))
        {
            foreach (var headerName in apiVersioningPolicy.HeaderNames.Where(name => !string.IsNullOrWhiteSpace(name)))
            {
                if (headers.Any(header => string.Equals(header.Name, headerName, StringComparison.OrdinalIgnoreCase)))
                {
                    continue;
                }

                headers.Add(new SwaggerHeaderDefinition
                {
                    Name = headerName,
                    Description = "API version header.",
                    Required = apiVersioningPolicy.Readers.Distinct().Count() == 1,
                    AuthorizedOnly = false
                });
            }
        }

        return headers;
    }

    private static IApiVersionReader BuildApiVersionReader(ApiVersioningPolicy policy)
    {
        var readers = new List<IApiVersionReader>();

        foreach (var readerType in policy.Readers.Distinct())
        {
            switch (readerType)
            {
                case ApiVersionReaderType.QueryString:
                    readers.Add(new QueryStringApiVersionReader(policy.QueryStringParameterName));
                    break;

                case ApiVersionReaderType.Header:
                    readers.Add(new HeaderApiVersionReader(policy.HeaderNames.ToArray()));
                    break;

                case ApiVersionReaderType.MediaType:
                    readers.Add(new MediaTypeApiVersionReader(policy.MediaTypeParameterName));
                    break;

                case ApiVersionReaderType.UrlSegment:
                    readers.Add(new UrlSegmentApiVersionReader());
                    break;
            }
        }

        return readers.Count == 1 ? readers[0] : ApiVersionReader.Combine(readers.ToArray());
    }

    private sealed record SwaggerDocumentRegistrationInfo(string DocumentName, string? ApiGroupName, string Version, string Title, string? Description);

    /// <summary>
    /// Registers keyed <see cref="Repository"/> services for the specified database policies.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="databasePolicies">The database policies to register.</param>
    /// <returns>The <paramref name="services"/> instance.</returns>
    private static IServiceCollection AddAbsoluteDatabase(this IServiceCollection services, List<DatabasePolicy>? databasePolicies)
    {
        if (databasePolicies is null || !databasePolicies.Any()) return services;

        foreach (var policy in databasePolicies)
        {
            services.AddKeyedScoped<Repository>(policy.Name, (sp, key) => new Repository(policy, sp.GetRequiredService<IHttpContextAccessor>()));
        }

        return services;
    }

    /// <summary>
    /// Registers keyed <see cref="StorageService"/> services for the specified storage policies.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="policies">The storage policies to register.</param>
    /// <returns>The <paramref name="services"/> instance.</returns>
    private static IServiceCollection AddAbsoluteStorage(this IServiceCollection services, List<StoragePolicy>? policies)
    {
        if (policies is null || !policies.Any()) return services;

        foreach (var policy in policies)
        {
            services.AddKeyedSingleton<StorageService>(policy.Name, (sp, key) => new StorageService(policy));
        }

        return services;
    }

    /// <summary>
    /// Registers named <see cref="HttpClient"/> services for the specified HTTP client policies.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="policies">The HTTP client policies to register.</param>
    /// <returns>The <paramref name="services"/> instance.</returns>
    private static IServiceCollection AddAbsoluteHttpClients(this IServiceCollection services, List<HttpClientPolicy>? policies)
    {
        if (policies is null || !policies.Any()) return services;

        foreach (var policy in policies)
        {
            var httpClientBuilder = services.AddHttpClient(policy.Name, client =>
            {
                if (!string.IsNullOrWhiteSpace(policy.BaseAddress))
                {
                    client.BaseAddress = new Uri(policy.BaseAddress, UriKind.Absolute);
                }

                if (policy.TimeoutSeconds > 0)
                {
                    client.Timeout = TimeSpan.FromSeconds(policy.TimeoutSeconds);
                }

                if (policy.DefaultHeaders is not null)
                {
                    foreach (var header in policy.DefaultHeaders)
                    {
                        client.DefaultRequestHeaders.Remove(header.Key);
                        client.DefaultRequestHeaders.TryAddWithoutValidation(header.Key, header.Value);
                    }
                }
            });

            if (policy.ResiliencePolicy is not null)
            {
                var resiliencePolicy = HttpResiliencePolicyFactory.CreateHttpPolicy(policy.ResiliencePolicy);
                httpClientBuilder.AddHttpMessageHandler(() => new PollyHttpMessageHandler(resiliencePolicy));
            }
        }

        return services;
    }

    /// <summary>
    /// Registers health checks for the configured database and storage policies.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="dbPolicies">Database policies to expose as health checks.</param>
    /// <param name="storagePolicies">Storage policies to expose as health checks.</param>
    /// <returns>The <paramref name="services"/> instance.</returns>
    private static IServiceCollection AddAbsoluteHealthChecks(
    this IServiceCollection services,
    List<DatabasePolicy>? dbPolicies,
    List<StoragePolicy>? storagePolicies)
    {
        var healthBuilder = services.AddHealthChecks();
        healthBuilder.AddCheck(
            "self",
            () => HealthCheckResult.Healthy("Application is running."),
            tags: ["live", "ready", "startup"]);

        if (dbPolicies is not null && dbPolicies.Any())
        {
            foreach (var policy in dbPolicies)
            {
                string connectionString = Environment.GetEnvironmentVariable(policy.ConnectionStringName) ?? "";

                if (string.IsNullOrEmpty(connectionString))
                {
                    healthBuilder.AddCheck($"DB: {policy.Name}", () => HealthCheckResult.Unhealthy($"Missing Connection String: {policy.ConnectionStringName}"));
                    continue;
                }

                _ = policy.DatabaseProvider switch
                {
                    DatabaseProvider.PostgreSQL => healthBuilder.AddNpgSql(
                        connectionString,
                        name: $"Postgres: {policy.Name}",
                        tags: ["db", "postgres", "ready"]),

                    DatabaseProvider.MSSQL => healthBuilder.AddSqlServer(
                        connectionString,
                        name: $"MSSQL: {policy.Name}",
                        tags: ["db", "mssql", "ready"]),

                    _ => healthBuilder
                };
            }
        }

        if (storagePolicies is not null && storagePolicies.Any())
        {
            foreach (var policy in storagePolicies)
            {
                _ = policy.StorageProvider switch
                {
                    StorageProvider.Minio => healthBuilder.AddMinio(sp => (IMinioClient)StorageFactory.Create(policy), name: policy.Name, failureStatus: HealthStatus.Unhealthy, tags: ["storage", "minio", "ready"]),

                    StorageProvider.AzureBlob => healthBuilder.AddAzureBlobStorage(
                        Environment.GetEnvironmentVariable(policy.ConnectionStringName)!,
                        name: policy.Name, tags: ["storage", "blob", "ready"]),

                    StorageProvider.S3 =>
                            healthBuilder.Add(new HealthCheckRegistration(
                                policy.Name,
                                sp => new S3StorageHealthCheck((IAmazonS3)StorageFactory.Create(policy), policy.BucketName),
                                failureStatus: HealthStatus.Unhealthy,
                                tags: ["storage", "s3", "ready"]
                            )),

                    StorageProvider.GoogleCloud =>
                            healthBuilder.Add(new HealthCheckRegistration(
                                policy.Name,
                                sp => new GcpStorageHealthCheck(sp, policy.Name),
                                failureStatus: HealthStatus.Unhealthy,
                                tags: ["storage", "gcp", "ready"]
                            )),


                    _ => healthBuilder
                };
            }
        }

        return services;
    }

    /// <summary>
    /// Registers the specified rate-limit policies.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="policies">The rate limit policies to register.</param>
    /// <returns>The <paramref name="services"/> instance.</returns>
    private static IServiceCollection AddAbsoluteRateLimits(this IServiceCollection services, List<RateLimitPolicy>? policies)
    {
        if (policies is null || !policies.Any()) return services;

        return services.AddRateLimiter(options =>
        {
            options.OnRejected = async (context, token) =>
            {
                context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;

                var apiResponse = new ApiResponse<object>(
                    IsSuccess: false,
                    Error: new ErrorResponse
                    {
                        ErrorCode = ERRORCODE.TOOMANYREQUESTS,
                        ErrorMessage = "Rate limit exceeded. Please try again later."
                    });

                await context.HttpContext.Response.WriteAsJsonAsync(apiResponse, cancellationToken: token);
            };

            foreach (var policy in policies)
            {

                Func<HttpContext, string> keyResolver = (httpContext) => policy.Scope switch
                {
                    RateLimitScope.IpAddress => httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown_ip",
                    RateLimitScope.User => httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? httpContext.User.Identity?.Name ?? "anonymous",
                    RateLimitScope.Endpoint => httpContext.Request.Path.Value ?? "global_path",
                    RateLimitScope.ApiKey => httpContext.Request.Headers[HEADER.APIKEY].ToString() ?? "no_key",
                    _ => "global_shared_key"
                };

                switch (policy.Algorithm)
                {
                    case RateLimitAlgorithm.FixedWindow:
                        options.AddPolicy(policy.PolicyName, httpContext =>
                            RateLimitPartition.GetFixedWindowLimiter(
                                partitionKey: keyResolver(httpContext),
                                factory: _ => new FixedWindowRateLimiterOptions
                                {
                                    PermitLimit = policy.PermitLimit,
                                    Window = policy.Window,
                                    QueueLimit = 0
                                }));
                        break;

                    case RateLimitAlgorithm.SlidingWindow:
                        options.AddPolicy(policy.PolicyName, httpContext =>
                            RateLimitPartition.GetSlidingWindowLimiter(
                                partitionKey: keyResolver(httpContext),
                                factory: _ => new SlidingWindowRateLimiterOptions
                                {
                                    PermitLimit = policy.PermitLimit,
                                    Window = policy.Window,
                                    SegmentsPerWindow = policy.SegmentsPerWindow > 0 ? policy.SegmentsPerWindow : 1,
                                    QueueLimit = 0
                                }));
                        break;

                    case RateLimitAlgorithm.TokenBucket:
                        options.AddPolicy(policy.PolicyName, httpContext =>
                            RateLimitPartition.GetTokenBucketLimiter(
                                partitionKey: keyResolver(httpContext),
                                factory: _ => new TokenBucketRateLimiterOptions
                                {
                                    TokenLimit = policy.TokenLimit,
                                    TokensPerPeriod = policy.TokensPerPeriod,
                                    ReplenishmentPeriod = policy.Window,
                                    AutoReplenishment = true,
                                    QueueLimit = 0
                                }));
                        break;

                    case RateLimitAlgorithm.Concurrency:
                        options.AddPolicy(policy.PolicyName, httpContext =>
                            RateLimitPartition.GetConcurrencyLimiter(
                                partitionKey: keyResolver(httpContext),
                                factory: _ => new ConcurrencyLimiterOptions
                                {
                                    PermitLimit = policy.PermitLimit,
                                    QueueLimit = 0
                                }));
                        break;
                }
            }
        });
    }

    /// <summary>
    /// Registers the library authentication configuration.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The <paramref name="services"/> instance.</returns>
    private static IServiceCollection AddAbsoluteAuthentication(this IServiceCollection services)
    {
        return services.AddAbsoluteAuthentication(authManifest: null);
    }

    /// <summary>
    /// Registers the library authentication configuration.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="authManifest">The authentication manifest that controls which handlers are registered.</param>
    /// <returns>The <paramref name="services"/> instance.</returns>
    private static IServiceCollection AddAbsoluteAuthentication(this IServiceCollection services, AuthManifest? authManifest)
    {
        // 1. Fetch all settings from Environment Variables
        var secretKey = Environment.GetEnvironmentVariable("JWT_SECRET");
        var issuer = Environment.GetEnvironmentVariable("JWT_ISSUER") ?? "AbsoluteAlgorithm.Identity";
        var audience = Environment.GetEnvironmentVariable("JWT_AUDIENCE") ?? "AbsoluteAlgorithm.Apps";
        var enableJwt = authManifest?.EnableJwt ?? true;
        var enableCookies = authManifest?.EnableCookies ?? true;

        if (!enableJwt && !enableCookies)
        {
            throw new InvalidOperationException("At least one authentication handler must be enabled.");
        }

        if (enableJwt && (string.IsNullOrEmpty(secretKey) || secretKey.Length < 32))
        {
            throw new Exception("CRITICAL: JWT_SECRET must be set as an environment variable and be at least 32 characters.");
        }

        var keyBytes = enableJwt ? Encoding.UTF8.GetBytes(secretKey!) : null;

        var authenticationBuilder = services.AddAuthentication(options =>
        {
            if (enableJwt && enableCookies)
            {
                options.DefaultScheme = "AbsoluteHybrid";
                options.DefaultChallengeScheme = "AbsoluteHybrid";
                return;
            }

            if (enableJwt)
            {
                options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                return;
            }

            options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = CookieAuthenticationDefaults.AuthenticationScheme;
        })
        ;

        if (enableJwt && enableCookies)
        {
            authenticationBuilder = authenticationBuilder.AddPolicyScheme("AbsoluteHybrid", "JWT or Cookie", options =>
            {
                options.ForwardDefaultSelector = context =>
                {
                    string auth = context.Request.Headers["Authorization"]!;
                    if (!string.IsNullOrEmpty(auth) && auth.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                    {
                        return JwtBearerDefaults.AuthenticationScheme;
                    }

                    return CookieAuthenticationDefaults.AuthenticationScheme;
                };
            });
        }

        if (enableJwt)
        {
            authenticationBuilder = authenticationBuilder.AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = issuer,

                    ValidateAudience = true,
                    ValidAudience = audience,

                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(keyBytes!),

                    ClockSkew = TimeSpan.Zero
                };
            });
        }

        if (enableCookies)
        {
            authenticationBuilder = authenticationBuilder.AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, options =>
            {
                options.Cookie.Name = "AbsoluteAuth";
                options.Cookie.HttpOnly = true;
                options.Cookie.SameSite = SameSiteMode.Strict;
                options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
                options.ExpireTimeSpan = TimeSpan.FromDays(7);
                options.SlidingExpiration = true;

                options.Events.OnRedirectToLogin = context =>
                {
                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    return Task.CompletedTask;
                };
            });
        }

        if (enableCookies && authManifest?.EnableCsrfProtection == true)
        {
            services.AddAbsoluteAntiforgery();
        }

        return services;
    }

    /// <summary>
    /// Registers antiforgery services for cookie-authenticated API requests.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The <paramref name="services"/> instance.</returns>
    private static IServiceCollection AddAbsoluteAntiforgery(this IServiceCollection services)
    {
        services.AddAntiforgery(options =>
        {
            options.HeaderName = HEADER.CSRFTOKEN;
            options.Cookie.Name = "__Host-AbsoluteCsrf";
            options.Cookie.HttpOnly = true;
            options.Cookie.SameSite = SameSiteMode.Strict;
            options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
        });

        return services;
    }

    /// <summary>
    /// Registers the specified authorization policies.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="policies">The authorization policies to register.</param>
    /// <returns>The <paramref name="services"/> instance.</returns>
    private static IServiceCollection AddAbsoluteAuthorization(this IServiceCollection services, List<AuthPolicy>? policies)
    {
        if (policies is null || !policies.Any()) return services;

        services.AddAuthorization(options =>
        {
            foreach (var policyModel in policies)
            {
                options.AddPolicy(policyModel.PolicyName, builder =>
                {
                    if (policyModel.RequiredRoles.Any())
                    {
                        builder.RequireRole(policyModel.RequiredRoles);
                    }

                    foreach (var claim in policyModel.RequiredClaims)
                    {
                        builder.RequireClaim(claim.Key, claim.Value);
                    }

                    builder.RequireAuthenticatedUser();
                });
            }
        });

        return services;
    }
}
