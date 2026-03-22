# FileFormula.Api.Infrastructure

Reusable ASP.NET Core Web API infrastructure library for rapid, secure, and maintainable service development. Includes database, storage, authentication, resilience, OpenAPI, and more—fully pluggable and opt-in.

## Installation

From NuGet.org:
```bash
dotnet add package FileFormula.Api.Infrastructure
```


> Targets `net10.0`. Bundles Dapper, Polly, NSwag, NLog, CsvHelper, and storage SDKs for S3, Azure Blob, GCP, and MinIO.

---

## Quick Start

```csharp
WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

//sample Application Configuration
ApplicationConfiguration appConfig = new ApplicationConfiguration
{
    // Enable or disable relational database support (SQL Server, PostgreSQL)
    EnableRelationalDatabase = true,

    // List of database connection policies (multiple DBs supported)
    DatabasePolicies = new List<DatabasePolicy>
    {
        new DatabasePolicy
        {
            // Database provider: MSSQL or PostgreSQL
            DatabaseProvider = DatabaseProvider.MSSQL,
            // Name of the environment variable holding the connection string
            ConnectionStringName = "<MSSQL_CONNECTION_STRING_ENVIRONMENT_VARIABLE_NAME>",
            // Run initialization script on startup
            InitializeDatabase = true,
            // Create and wire up audit table/triggers
            InitializeAuditTable = true,
            // SQL script to initialize the database
            InitializationScript = "<MSSQL_SCRIPT>",
            // Unique key for this database (used for DI)
            Name = "<KEYED_IDENTIFIER>",
            // Connection pool settings
            MaxPoolSize = 100,
            MinPoolSize = 10,
            // Command timeout in seconds
            CommandTimeoutSeconds = 30
        },
        new DatabasePolicy
        {
            DatabaseProvider = DatabaseProvider.PostgreSQL,
            ConnectionStringName = "<POSTGRESQL_CONNECTION_STRING_ENVIRONMENT_VARIABLE_NAME>",
            InitializeDatabase = true,
            InitializeAuditTable = true,
            InitializationScript = "<POSTGRESQL_SCRIPT>",
            Name = "<KEYED_IDENTIFIER>",
            MaxPoolSize = 100,
            MinPoolSize = 10,
            CommandTimeoutSeconds = 30
        }
    },

    // Enable or disable health check endpoints
    EnableHealthChecks = true,

    // Enable or disable idempotency middleware
    EnableIdempotency = true,

    // Idempotency policy configuration
    IdempotencyPolicy = new IdempotencyPolicy
    {
        // HTTP methods to treat as idempotent
        ReplayableMethods = new List<string> { "POST", "PUT" },
        // How long to cache idempotency keys (minutes)
        ExpirationMinutes = 10,
        // Include query string in idempotency key
        IncludeQueryStringInKey = true,
        // Max response body size to cache (bytes)
        MaximumResponseBodyBytes = 1024 * 1024 // 1 MB
    },

    // Enable or disable object storage support
    EnableStorage = true,

    // List of object storage policies (S3, Azure, Minio, GCP)
    StoragePolicies = new List<StoragePolicy>
    {
        new StoragePolicy
        {
            // Unique key for this storage provider
            Name = "<KEYED_IDENTIFIER>",
            // Storage provider type
            StorageProvider = StorageProvider.Minio,
            // Name of the environment variable holding the connection string
            ConnectionStringName = "<MINIO_CONNECTION_STRING_ENVIRONMENT_VARIABLE_NAME>",
            // Bucket/container name
            BucketName = "<BUCKET_NAME>"
        },
        new StoragePolicy
        {
            Name = "<KEYED_IDENTIFIER>",
            StorageProvider = StorageProvider.AzureBlob,
            ConnectionStringName = "<AZURE_BLOB_STORAGE_CONNECTION_STRING_ENVIRONMENT_VARIABLE_NAME>",
            BucketName = "<BUCKET_NAME>"
        },
        new StoragePolicy
        {
            Name = "<KEYED_IDENTIFIER>",
            StorageProvider = StorageProvider.S3,
            ConnectionStringName = "<S3_STORAGE_CONNECTION_STRING_ENVIRONMENT_VARIABLE_NAME>",
            BucketName = "<BUCKET_NAME>"
        },
        new StoragePolicy
        {
            Name = "<KEYED_IDENTIFIER>",
            StorageProvider = StorageProvider.GoogleCloud,
            ConnectionStringName = "<GCP_STORAGE_CONNECTION_STRING_ENVIRONMENT_VARIABLE_NAME>",
            BucketName = "<BUCKET_NAME>"
        }
    },

    // Enable or disable rate limiting
    EnableRateLimit = true,

    // List of rate limit policies
    RateLimitPolicies = new List<RateLimitPolicy>
    {
        new RateLimitPolicy
        {
            // Unique name for this rate limit policy
            PolicyName = "<POLICY_NAME>",
            // Algorithm: FixedWindow, SlidingWindow, TokenBucket, Concurrency
            Algorithm = RateLimitAlgorithm.FixedWindow,
            // Scope: IpAddress, User, Endpoint, ApiKey, etc.
            Scope = RateLimitScope.IpAddress,
            // Number of allowed requests per window
            PermitLimit = 2,
            // Time window for rate limiting
            Window = TimeSpan.FromSeconds(2)
        }
    },

    // Enable or disable API versioning
    EnableApiVersioning = true,

    // API versioning policy configuration
    ApiVersioningPolicy = new ApiVersioningPolicy
    {
        // Default API version (major.minor)
        DefaultMajorVersion = 1,
        DefaultMinorVersion = 0,
        // Assume default version if not specified
        AssumeDefaultVersionWhenUnspecified = true,
        // Include API version info in responses
        ReportApiVersions = true,
        // Where to read API version from (query, header, url, media type)
        Readers = [
            ApiVersionReaderType.QueryString,
            ApiVersionReaderType.Header,
            ApiVersionReaderType.UrlSegment,
            ApiVersionReaderType.MediaType
        ],
        // Query string parameter name for version
        QueryStringParameterName = "api-version",
        // Header names for versioning
        HeaderNames = ["x-api-version"],
        // Media type parameter name for versioning
        MediaTypeParameterName = "ver"
    },

    // Enable or disable Swagger/OpenAPI docs
    EnableSwagger = true,

    // Swagger/OpenAPI policy configuration
    SwaggerPolicy = new SwaggerPolicy
    {
        // API title
        Title = "<API_TITLE>",
        // API description
        Description = "<API_DESCRIPTION>",
        // Document mode: Single, PerApiVersion, etc.
        DocumentMode = SwaggerDocumentMode.PerApiVersion,
        // List of Swagger document definitions
        Documents = new List<SwaggerDocumentDefinition>
        {
            new SwaggerDocumentDefinition { DocumentName = "v1", ApiGroupName = "v1", Version = "1.0", Title = "<DOC_TITLE_V1>" },
            new SwaggerDocumentDefinition { DocumentName = "v2", ApiGroupName = "v2", Version = "2.0", Title = "<DOC_TITLE_V2>" },
            new SwaggerDocumentDefinition { DocumentName = "v3", ApiGroupName = "v3", Version = "3.0", Title = "<DOC_TITLE_V3>" }
        }
    },

    // Enable or disable webhook signature validation
    EnableWebhookSignatureValidation = true,

    // List of webhook signature policies
    WebhookSignaturePolicies = new List<WebhookSignaturePolicy>
    {
        new WebhookSignaturePolicy
        {
            // Unique name for this webhook policy
            Name = "<WEBHOOK_POLICY_NAME>",
            // Path prefix for webhook endpoints
            PathPrefix = "<WEBHOOK_PATH_PREFIX>",
            // Name of the environment variable holding the secret
            SecretName = "<WEBHOOK_SECRET_ENVIRONMENT_VARIABLE_NAME>",
            // Signature algorithm
            Algorithm = RequestSignatureAlgorithm.HmacSha256,
            // Allowed clock skew in seconds
            AllowedClockSkewSeconds = 300
        }
    },

    // Enable or disable authentication handlers
    ConfigureAuthentication = true,

    // Enable or disable authorization policies
    ConfigureAuthorization = true,

    // Authentication and authorization manifest
    AuthManifest = new AuthManifest
    {
        // Enable JWT authentication
        EnableJwt = true,
        // Enable cookie authentication
        EnableCookies = true,
        // Enable CSRF protection (only for cookies, enable on HTTPS)
        EnableCsrfProtection = false,
        // Enable API key authentication
        EnableApiKeyAuth = true,
        // List of authorization policies
        Policies = new List<AuthPolicy>
        {
            new AuthPolicy
            {
                // Policy name
                PolicyName = "<AUTH_POLICY_NAME_1>",
                // Required roles for this policy
                RequiredRoles = new List<string> { "<ROLE_1>", "<ROLE_2>" }
            },
            new AuthPolicy
            {
                PolicyName = "<AUTH_POLICY_NAME_2>",
                // Required claims for this policy
                RequiredClaims = new Dictionary<string, string> { { "<CLAIM_KEY>", "<CLAIM_VALUE>" } }
            }
        }
    },
};

builder.RegisterFileFormulaWebApplicationBuilder(appConfig);

WebApplication app = builder.Build();
app.UseFileFormulaPipeline(appConfig);
app.Run();
```

---

## Configuration

- All features are opt-in via `ApplicationConfiguration`.
- Secrets (DB, JWT, storage, API keys) are resolved from environment variables.
- Supports config binding from `appsettings.json`:
  ```csharp
  var appConfig = builder.Configuration
      .GetSection("FileFormulaApiInfrastructure")
      .Get<ApplicationConfiguration>()
      ?? new ApplicationConfiguration();
  ```

---

## Features

### Database

- Dapper-based, transaction-aware repositories.
- Supports PostgreSQL and SQL Server.
- Request-scoped transactions, optimistic concurrency, paged queries, and resilience policies.

### Storage

- Keyed `StorageService` for S3, Azure Blob, GCP, MinIO.
- Automatic filename sanitization.

### HTTP Clients

- Named, resilient clients via `HttpClientPolicy` and Polly.

### Authentication & Authorization

- JWT, cookies, API key, and hybrid auth.
- Policy-based and attribute-based authorization.

### Rate Limiting & Idempotency

- Flexible rate limiting (IP, user, endpoint, etc.).
- Idempotency for safe retries on POST/PUT/PATCH.

### Webhook Signature Validation

- HMAC-based signature verification for secure webhooks.

### API Versioning & Swagger

- Flexible versioning (header, query, etc.).
- NSwag-based OpenAPI docs with security integration.

### Health Checks

- Auto-registers checks for all configured services.

### Error Handling

- Standardized error envelope and codes.
- Global model validation.

---

## Utilities

Includes helpers for JWT, hashing, encryption, CSV, ETags, slugs, and more.

---

## Example: Full Configuration

See the full example in the source for advanced scenarios (multiple DBs, storage, custom policies, etc.).

---

## Tips

- Prefer interpolated queries for SQL safety.
- Use environment variables for all secrets.
- Implement `IIdempotencyStore` for distributed idempotency.
- Keep your `JWT_SECRET` strong and rotated.

---

For more, see the source code and XML docs.

You can also bind from `appsettings.json`:

```csharp
var appConfig = builder.Configuration
    .GetSection("FileFormulaApiInfrastructure")
    .Get<ApplicationConfiguration>()
    ?? new ApplicationConfiguration();
```

---

## Environment Variables

Secrets are resolved from environment variables — not from config files.

| Variable | When Required |
|---|---|
| `JWT_SECRET` (min 32 chars) | JWT auth enabled |
| `JWT_ISSUER`, `JWT_AUDIENCE` | Optional (defaults: `AbsoluteAlgorithm.Identity`, `AbsoluteAlgorithm.Apps`) |
| `DatabasePolicy.ConnectionStringName` | Per database policy |
| `StoragePolicy.ConnectionStringName` | Per storage policy |
| `WebhookSignaturePolicy.SecretName` | Per webhook policy |
| `AuthorizeKeyAttribute(secretName)` | Per API key endpoint |

> `ConnectionStringName` and `SecretName` are **names of environment variables**, not raw values.

---

## ApplicationConfiguration

All features are opt-in through the root configuration model.

| Property | Type | Purpose |
|---|---|---|
| `EnableRelationalDatabase` | `bool` | Database + transaction middleware |
| `DatabasePolicies` | `List<DatabasePolicy>?` | Named database registrations |
| `EnableStorage` | `bool` | Object storage services |
| `StoragePolicies` | `List<StoragePolicy>?` | Named storage registrations |
| `HttpClientPolicies` | `List<HttpClientPolicy>?` | Named HTTP clients |
| `EnableApiVersioning` | `bool` | API versioning |
| `ApiVersioningPolicy` | `ApiVersioningPolicy?` | Versioning options |
| `EnableSwagger` | `bool` | OpenAPI / Swagger |
| `SwaggerPolicy` | `SwaggerPolicy?` | Swagger options |
| `EnableIdempotency` | `bool` | Request replay protection |
| `IdempotencyPolicy` | `IdempotencyPolicy?` | Idempotency options |
| `ConfigureAuthentication` | `bool` | Auth handlers |
| `ConfigureAuthorization` | `bool` | Authorization policies |
| `AuthManifest` | `AuthManifest?` | Auth settings |
| `EnableWebhookSignatureValidation` | `bool` | Webhook verification |
| `WebhookSignaturePolicies` | `List<WebhookSignaturePolicy>?` | Webhook policies |
| `EnableRateLimit` | `bool` | Rate limiting |
| `RateLimitPolicies` | `List<RateLimitPolicy>?` | Rate limit policies |
| `EnableHealthChecks` | `bool` | Health endpoints (default `true`) |

---

## Database

Dapper-based, request-scoped, transaction-aware repositories. Supports **PostgreSQL** and **SQL Server**.

### Configuration

```csharp
new DatabasePolicy
{
    Name = "primary",
    DatabaseProvider = DatabaseProvider.PostgreSQL,
    ConnectionStringName = "PRIMARY_DB_CONNECTION",
    InitializeDatabase = true,
    InitializeAuditTable = true,
    InitializationScript = "CREATE TABLE IF NOT EXISTS ...",
    MaxPoolSize = 100,
    MinPoolSize = 10,
    CommandTimeoutSeconds = 30,
    ResiliencePolicy = new ResiliencePolicy
    {
        Retry = new RetryResiliencePolicy
        {
            MaxRetryAttempts = 3,
            DelayStrategy = RetryDelayStrategy.Exponential,
            DelayMilliseconds = 200,
            BackoffMultiplier = 2
        },
        Timeout = new TimeoutResiliencePolicy { TimeoutSeconds = 30 },
        CircuitBreaker = new CircuitBreakerResiliencePolicy
        {
            HandledEventsAllowedBeforeBreaking = 5,
            DurationOfBreakSeconds = 30
        }
    }
}
```

### Resolving a Repository

```csharp
public sealed class UsersService
{
    private readonly Repository _repository;

    public UsersService(IServiceProvider sp)
    {
        _repository = sp.GetRequiredKeyedService<Repository>("primary");
    }
}
```

### Querying

Prefer interpolated queries — values become parameters automatically:

```csharp
var users = await _repository.QueryInterpolatedAsync<UserRow>(
    $"SELECT id, email FROM users WHERE tenant_id = {tenantId} AND is_active = {true}",
    cancellationToken: ct);
```

Raw SQL when needed:

```csharp
var users = await _repository.QueryAsync<UserRow>(
    "SELECT id, email FROM users WHERE tenant_id = @tenantId",
    new { tenantId }, cancellationToken: ct);
```

Other methods: `ExecuteAsync`, `ExecuteInterpolatedAsync`, `ExecuteStoredProcedureAsync`, `ExecuteScalarAsync`.

> **Never** let users provide SQL identifiers (table/column names). Only values are parameterized.

### Paged Queries

```csharp
var query = new RepositoryPagedQuery
{
    SelectSql = "SELECT u.id, u.email, u.display_name AS displayName FROM users u",
    CountSql = "SELECT COUNT(1) FROM users u",
    DefaultOrderBy = "u.updated_at DESC",
    SortColumns = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        ["email"] = "u.email",
        ["displayName"] = "u.display_name"
    },
    FilterColumns = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        ["email"] = "u.email",
        ["status"] = "u.status"
    },
    SearchColumns = ["u.email", "u.display_name"]
};

var request = new PagedRequest
{
    PageNumber = 1,
    PageSize = 20,
    SearchTerm = "lalith",
    Sorts = [new SortDescriptor { Field = "email", Direction = SortDirection.Descending }],
    Filters = [new FilterDescriptor { Field = "status", Operator = FilterOperator.Equals, Value = "active" }]
};

var result = await _repository.QueryPageAsync<UserListItem>(query, request, cancellationToken: ct);
// result.Items, result.TotalCount, result.TotalPages, result.HasNextPage, etc.
```

Clients send logical field names. SQL expressions stay server-owned via `SortColumns`/`FilterColumns` maps.

### Optimistic Concurrency

```csharp
var definition = new RepositoryOptimisticUpdateDefinition
{
    ResourceName = "user",
    CurrentVersionSql = "SELECT version_token FROM users WHERE id = @id",
    ExistsSql = "SELECT CASE WHEN EXISTS (SELECT 1 FROM users WHERE id = @id) THEN CAST(1 AS bit) ELSE CAST(0 AS bit) END",
    UpdateSql = @"UPDATE users SET display_name = @displayName, version_token = @newVersionToken
                  WHERE id = @id AND version_token = @expectedVersionToken",
    RequireIfMatchHeader = true
};

var result = await _repository.ExecuteOptimisticUpdateAsync(definition,
    new { id, displayName, expectedVersionToken, newVersionToken }, cancellationToken: ct);
```

Throws `E404` if not found, `E409` on version mismatch, `E412` if `If-Match` header is missing/stale.

### Transactions

`DatabaseTransactionMiddleware` manages request-scoped transactions automatically. All repository calls within one request share the same connection and transaction per named database. Commits on success, rolls back on failure.

The repository also sets provider session context for `UserId` and `CorrelationId` (useful for audit triggers).

---

## Storage

Keyed `StorageService` per `StoragePolicy.Name`. Providers: **MinIO**, **Azure Blob**, **GCP**, **S3**.

```csharp
new StoragePolicy
{
    Name = "files",
    StorageProvider = StorageProvider.S3,
    ConnectionStringName = "S3_CONNECTION",
    BucketName = "my-app-files",
    GcpProjectId = "my-project"  // only for GoogleCloud
}
```

---

```csharp
var builder = WebApplication.CreateBuilder(args);

ApplicationConfiguration appConfig = new ApplicationConfiguration
{
    EnableRelationalDatabase = true,
    DatabasePolicies = new List<DatabasePolicy>
    {
        new DatabasePolicy
        {
            DatabaseProvider = DatabaseProvider.MSSQL,
            ConnectionStringName = "MSSQLCS",
            InitializeDatabase = true,
            InitializeAuditTable = true,
            InitializationScript = msSqlScript,
            Name = "testdb",
            MaxPoolSize = 100,
            MinPoolSize = 10,
            CommandTimeoutSeconds = 30
        },
        new DatabasePolicy
        {
            DatabaseProvider = DatabaseProvider.PostgreSQL,
            ConnectionStringName = "POSTGRESCS",
            InitializeDatabase = true,
            InitializeAuditTable = true,
            InitializationScript = postgreSqlScript,
            Name = "test_db",
            MaxPoolSize = 100,
            MinPoolSize = 10,
            CommandTimeoutSeconds = 30
        }
    },
    EnableHealthChecks = true,
    EnableIdempotency = true,
    IdempotencyPolicy = new IdempotencyPolicy
    {
        ReplayableMethods = new List<string> { "POST", "PUT" },
        ExpirationMinutes = 10,
        IncludeQueryStringInKey = true,
        MaximumResponseBodyBytes = 1024 * 1024 // 1 MB
    },
    EnableStorage = true,
    StoragePolicies = new List<StoragePolicy>
    {
        new StoragePolicy
        {
            Name = "minio",
            StorageProvider = StorageProvider.Minio,
            ConnectionStringName = "MINIO_CONNECTION_STRING",
            BucketName = "images"
        },
        new StoragePolicy
        {
            Name = "azure",
            StorageProvider = StorageProvider.AzureBlob,
            ConnectionStringName = "AZURE_STORAGE_CONNECTION_STRING",
            BucketName = "images"
        },
        new StoragePolicy
        {
            Name = "s3",
            StorageProvider = StorageProvider.S3,
            ConnectionStringName = "S3_CONNECTION_STRING",
            BucketName = "images"
        }
    },
    EnableRateLimit = true,
    RateLimitPolicies = new List<RateLimitPolicy>
    {
        new RateLimitPolicy
        {
            PolicyName = "rpol",
            Algorithm = RateLimitAlgorithm.FixedWindow,
            Scope = RateLimitScope.IpAddress,
            PermitLimit = 2,
            Window = TimeSpan.FromSeconds(2)
        }
    },
    EnableApiVersioning = true,
    ApiVersioningPolicy = new ApiVersioningPolicy
    {
        DefaultMajorVersion = 1,
        DefaultMinorVersion = 0,
        AssumeDefaultVersionWhenUnspecified = true,
        ReportApiVersions = true,
        Readers = [
            ApiVersionReaderType.QueryString,
            ApiVersionReaderType.Header,
            ApiVersionReaderType.UrlSegment,
            ApiVersionReaderType.MediaType
        ],
        QueryStringParameterName = "api-version",
        HeaderNames = ["x-api-version"],
        MediaTypeParameterName = "ver"
    },
    EnableSwagger = true,
    SwaggerPolicy = new SwaggerPolicy
    {
        Title = "Sandbox API",
        Description = "Sandbox API Documentation",
        DocumentMode = SwaggerDocumentMode.PerApiVersion,
        Documents = new List<SwaggerDocumentDefinition>
        {
            new SwaggerDocumentDefinition { DocumentName = "v1", ApiGroupName = "v1", Version = "1.0", Title = "Sandbox API v1" },
            new SwaggerDocumentDefinition { DocumentName = "v2", ApiGroupName = "v2", Version = "2.0", Title = "Sandbox API v2" },
            new SwaggerDocumentDefinition { DocumentName = "v3", ApiGroupName = "v3", Version = "3.0", Title = "Sandbox API v3" }
        }
    },
    EnableWebhookSignatureValidation = true,
    WebhookSignaturePolicies = new List<WebhookSignaturePolicy>
    {
        new WebhookSignaturePolicy
        {
            Name = "default",
            PathPrefix = "/webhooks",
            SecretName = "WEBHOOK_SECRET",
            Algorithm = RequestSignatureAlgorithm.HmacSha256,
            AllowedClockSkewSeconds = 300
        }
    },
    ConfigureAuthentication = true,
    ConfigureAuthorization = true,
    AuthManifest = new AuthManifest
    {
        EnableJwt = true,
        EnableCookies = true,
        EnableCsrfProtection = false, //enable this on https
        EnableApiKeyAuth = true,
        Policies = new List<AuthPolicy>
        {
            new AuthPolicy { PolicyName = "AdminOnly", RequiredRoles = new List<string> { "Admin" } },
            new AuthPolicy { PolicyName = "PremiumUser", RequiredClaims = new Dictionary<string, string> { { "subscription", "premium" } } }
        }
    },
};


var app = builder.Build();
app.UseFileFormulaPipeline(appConfig);
app.Run();
```
```csharp
new HttpClientPolicy
{
    Name = "github",
    BaseAddress = "https://api.github.com/",
    TimeoutSeconds = 30,
    DefaultHeaders = new Dictionary<string, string> { ["User-Agent"] = "my-api" },
    ResiliencePolicy = new ResiliencePolicy
    {
        Retry = new RetryResiliencePolicy
        {
            MaxRetryAttempts = 3,
            DelayStrategy = RetryDelayStrategy.Linear,
            DelayMilliseconds = 250,
            DelayIncrementMilliseconds = 250
        }
    }
}
```

```csharp
public sealed class GitHubClient(IHttpClientFactory factory)
{
    private readonly HttpClient _http = factory.CreateClient("github");

    public Task<string> GetRateLimitAsync(CancellationToken ct)
        => _http.GetStringAsync("rate_limit", ct);
}
```

### Resilience

Set `ResiliencePolicy` to non-null to enable Polly-backed resilience. Omit or set to `null` to disable.

**HTTP resilience** retries on: `HttpRequestException`, timeouts, `408`, `429`, `5xx`.

**Database resilience** handles: `DbException`, timeouts, PostgreSQL transient errors, SQL Server deadlocks.

### ResiliencePolicy Reference

```csharp
new ResiliencePolicy
{
    Retry = new RetryResiliencePolicy          // null = no retry
    {
        MaxRetryAttempts = 3,                  // default: 3
        DelayStrategy = RetryDelayStrategy.Exponential, // Fixed | Linear | Exponential | CustomSchedule
        DelayMilliseconds = 200,               // base delay
        DelayIncrementMilliseconds = 200,      // for Linear
        BackoffMultiplier = 2.0,               // for Exponential
        DelayScheduleMilliseconds = [200, 500, 1000] // for CustomSchedule
    },
    Timeout = new TimeoutResiliencePolicy      // null = no timeout
    {
        TimeoutSeconds = 30
    },
    CircuitBreaker = new CircuitBreakerResiliencePolicy  // null = no breaker
    {
        HandledEventsAllowedBeforeBreaking = 5,
        DurationOfBreakSeconds = 30
    }
}
```

---

## Authentication & Authorization

### Auth Manifest

```csharp
new AuthManifest
{
    EnableJwt = true,
    EnableCookies = true,
    EnableCsrfProtection = false,
    EnableApiKeyAuth = true,
    Policies =
    [
        new AuthPolicy { PolicyName = "AdminOnly", RequiredRoles = ["Admin"] },
        new AuthPolicy { PolicyName = "PremiumUser", RequiredClaims = new() { ["subscription"] = "premium" } }
    ]
}
```

**JWT**: requires `JWT_SECRET` env var (min 32 chars). Defaults: issuer `FileFormula.Identity`, audience `FileFormula.Apps`.

**Cookies**: registers `AbsoluteAuth` cookie — `HttpOnly`, `SameSite=Strict`, `Secure=Always`, 7-day sliding expiry. Login redirects become `401`.

**Hybrid**: when both JWT and cookies are enabled, a policy scheme (`AbsoluteHybrid`) picks JWT if `Authorization: Bearer ...` is present, cookies otherwise.

**CSRF**: only active when `EnableCookies` + `EnableCsrfProtection` are both true. Issues `XSRF-TOKEN` cookie on safe methods, validates `x-csrf-token` header on mutations. Bearer-only APIs don't need this.

### Authorization Policies

```csharp
[Authorize(Policy = "AdminOnly")]
[HttpGet("admin/report")]
public IActionResult GetReport() => Ok();
```

### API Key Authorization

```csharp
[AuthorizeKey("INTERNAL_API_KEY")]            // reads env var, validates x-api-key header
[HttpPost("sync")]
public IActionResult Sync() => Ok();

[AuthorizeKey("PARTNER_KEY", "x-partner-key")] // custom header
```

Constant-time comparison. Returns `401` on failure. Respects `[AllowAnonymous]`.

---

## Rate Limiting

```csharp
EnableRateLimit = true,
RateLimitPolicies =
[
    new RateLimitPolicy
    {
        PolicyName = "login-limit",
        Algorithm = RateLimitAlgorithm.FixedWindow,  // FixedWindow | SlidingWindow | TokenBucket | Concurrency
        Scope = RateLimitScope.IpAddress,            // Global | IpAddress | User | Endpoint | ApiKey
        PermitLimit = 5,
        Window = TimeSpan.FromMinutes(1)
    }
]
```

Additional properties for specific algorithms: `SegmentsPerWindow` (sliding), `TokenLimit` + `TokensPerPeriod` (token bucket).

Rejected requests get `E429` with a standardized error response.

---

## Idempotency

```csharp
EnableIdempotency = true,
IdempotencyPolicy = new IdempotencyPolicy
{
    HeaderName = "x-idempotency-key",       // default
    RequireHeader = true,
    ReplayableMethods = ["POST", "PUT", "PATCH"],
    ExpirationMinutes = 60,
    MaximumResponseBodyBytes = 64 * 1024,
    IncludeQueryStringInKey = false
}
```

Client sends `x-idempotency-key: ord_001`. If a matching successful response exists, it's replayed with `x-idempotency-replayed: true`. In-flight duplicates get `E409`.

Cache key = method + path + subject (user ID / IP) + idempotency key.

Default store: `InMemoryIdempotencyStore`. Implement `IIdempotencyStore` for distributed support.

---

## Webhook Signature Validation

```csharp
EnableWebhookSignatureValidation = true,
WebhookSignaturePolicies =
[
    new WebhookSignaturePolicy
    {
        Name = "stripe",
        PathPrefix = "/webhooks/stripe",
        SecretName = "STRIPE_WEBHOOK_SECRET",
        SignatureHeaderName = "stripe-signature",
        TimestampHeaderName = "x-signature-timestamp",
        Algorithm = RequestSignatureAlgorithm.HmacSha256,  // or HmacSha512
        AllowedClockSkewSeconds = 300
    }
]
```

Middleware validates signature + timestamp for matching path prefixes. Failures return `E401`.

`RequestSignatureUtility` exposes `GenerateTimestamp`, `ComputeSignature`, `VerifySignature` for manual use.

---

## API Versioning

```csharp
EnableApiVersioning = true,
ApiVersioningPolicy = new ApiVersioningPolicy
{
    DefaultMajorVersion = 1,
    DefaultMinorVersion = 0,
    Readers = [ApiVersionReaderType.Header, ApiVersionReaderType.QueryString],
    HeaderNames = ["x-api-version"],
    QueryStringParameterName = "api-version",
    AssumeDefaultVersionWhenUnspecified = true,
    ReportApiVersions = true,
    EnableApiExplorer = true,
    GroupNameFormat = "'v'VVV",
    SubstituteApiVersionInUrl = true
}
```

Reader types: `QueryString`, `Header`, `MediaType`, `UrlSegment`. Can be combined.

---

## Swagger / OpenAPI (NSwag)

```csharp
EnableSwagger = true,
SwaggerPolicy = new SwaggerPolicy
{
    Title = "My API",
    Description = "API documentation",
    DocumentMode = SwaggerDocumentMode.Single,  // or PerApiVersion
    SingleDocumentName = "v1",
    SingleDocumentVersion = "1.0",
    OpenApiPath = "/swagger/{documentName}/swagger.json",
    SwaggerUiPath = "/swagger",
    UseSwaggerUi = true,
    PersistAuthorization = true,
    EnableTryItOut = true,
    Headers =
    [
        new SwaggerHeaderDefinition
        {
            Name = "x-tenant-id",
            Description = "Tenant key",
            Required = true,
            AuthorizedOnly = true
        }
    ]
}
```

For per-version docs:

```csharp
DocumentMode = SwaggerDocumentMode.PerApiVersion,
Documents =
[
    new SwaggerDocumentDefinition { DocumentName = "v1", ApiGroupName = "v1", Version = "1.0", Title = "API v1" },
    new SwaggerDocumentDefinition { DocumentName = "v2", ApiGroupName = "v2", Version = "2.0", Title = "API v2" }
]
```

Automatically adds bearer/cookie/API key security definitions based on your auth config. Lock icons only appear on endpoints that actually require auth.

---

## Health Checks

Enabled by default (`EnableHealthChecks = true`). Default endpoint: `/health`.

Auto-registers checks for: self, PostgreSQL, SQL Server, MinIO, Azure Blob, S3, GCP Storage — based on your configured policies.

---

## Error Contracts

All errors use a standard envelope:

```json
{
  "isSuccess": false,
  "error": {
    "errorCode": "E422",
    "errorMessage": "The Email field is required.",
    "validationErrors": [
      { "field": "email", "messages": ["The Email field is required."] }
    ]
  }
}
```

Successful responses:

```json
{
  "isSuccess": true,
  "data": { ... }
}
```

### Throwing Errors

```csharp
throw ApiExceptions.Notfound("user");
throw ApiExceptions.Badrequest("Invalid input");
throw ApiExceptions.Conflict("Duplicate entry");
throw ApiExceptions.Unauthorized;
throw ApiExceptions.Forbidden;
throw ApiExceptions.PreconditionFailed("Version mismatch");
throw ApiExceptions.FromCode("E410", "Resource gone");
```

### Error Codes

`E400` bad request · `E401` unauthorized · `E403` forbidden · `E404` not found · `E409` conflict · `E410` gone · `E412` precondition failed · `E422` unprocessable · `E429` rate limited · `E499` cancelled · `E500` internal error

### Validation

`ValidateModelFilter` runs globally. Returns `E400` for malformed JSON, `E422` for validation failures — using the standard envelope above.

---

## Middleware Pipeline

`UseFileFormulaPipeline` wires everything in this order:

1. Forwarded headers
2. HSTS + HTTPS redirect (non-dev)
3. Database initialization
4. Routing
5. Rate limiting
6. Request metadata + correlation ID
7. Exception handling
8. Webhook signature validation
9. Response compression + caching
10. Static files
11. Swagger UI
12. Authentication
13. CSRF protection
14. Authorization
15. Idempotency
16. Database transactions
17. Health endpoints
18. `GET /` root endpoint
19. Controllers

---

## Services

### ICurrentUserAccessor

```csharp
public interface ICurrentUserAccessor
{
    ClaimsPrincipal? Principal { get; }
    string? UserId { get; }
    string? Email { get; }
    bool IsAuthenticated { get; }
    IReadOnlyList<string> Roles { get; }
}
```

### IRequestMetadataAccessor

Provides `RequestMetadata` with: `CorrelationId`, `UserId`, `TenantId`, `IdempotencyKey`, `ClientIpAddress`, `UserAgent`, `Path`, `Method`, `IsAuthenticated`.

---

## Utilities

Standalone helpers — usable even outside the pipeline.

| Utility | Purpose |
|---|---|
| `AuthUtility` | Create claims/identities/principals, cookie sign-in/out, secure cookies |
| `ClaimUtility` | Read user ID, email, roles from `ClaimsPrincipal` |
| `TokenUtility` | Generate tokens, API keys, JWTs, numeric codes, constant-time compare |
| `HashingUtility` | SHA-256/SHA-512 from strings, bytes, streams, base64 |
| `PasswordUtility` | Hash and verify passwords |
| `EncryptionUtility` | AES symmetric encrypt/decrypt |
| `AsymmetricKeyUtility` | RSA/ECDsa key generation and digital signatures |
| `RequestSignatureUtility` | HMAC request signing and verification |
| `ETagUtility` | Strong/weak ETags, `If-Match`/`If-None-Match` |
| `OptimisticConcurrencyUtility` | Version tokens, ETag matching, `304 Not Modified` |
| `CsvUtility` | Read/write CSV with spreadsheet-formula safety |
| `DateTimeUtility` | UTC normalization, Unix time conversion |
| `FileUtility` | Extensions, content types, file size formatting |
| `HttpUtility` | Header extraction, bearer token, IP, correlation ID, query strings |
| `JsonUtility` | Serialize/deserialize with consistent options |
| `ReflectionUtility` | Property discovery helpers |
| `EnumUtility` | Safe parsing, names, values, `IsDefined` |
| `CompressionUtility` | GZip and Brotli compress/decompress |
| `SlugUtility` | URL-safe slug generation |

---

## Sanitizers

**SpreadsheetFormulaSanitizer** — prefixes a single quote when strings start with `=`, `+`, `-`, `@`, tab, CR, or LF. Applied automatically to incoming JSON strings and model-bound values.

**FileNameSanitizer** — strips unsafe path/filename characters. Used automatically by `StorageService`.

---

## Full Configuration Example

```csharp
var appConfig = new ApplicationConfiguration
{
    EnableRelationalDatabase = true,
    DatabasePolicies =
    [
        new DatabasePolicy
        {
            Name = "primary",
            DatabaseProvider = DatabaseProvider.PostgreSQL,
            ConnectionStringName = "PRIMARY_DB_CONNECTION",
            InitializeDatabase = false,
            InitializeAuditTable = false,
            CommandTimeoutSeconds = 30,
            MaxPoolSize = 100,
            MinPoolSize = 10,
            ResiliencePolicy = new ResiliencePolicy
            {
                Retry = new RetryResiliencePolicy
                {
                    MaxRetryAttempts = 3,
                    DelayStrategy = RetryDelayStrategy.Exponential,
                    DelayMilliseconds = 200,
                    BackoffMultiplier = 2
                },
                Timeout = new TimeoutResiliencePolicy { TimeoutSeconds = 30 },
                CircuitBreaker = new CircuitBreakerResiliencePolicy
                {
                    HandledEventsAllowedBeforeBreaking = 5,
                    DurationOfBreakSeconds = 30
                }
            }
        }
    ],
    EnableStorage = true,
    StoragePolicies =
    [
        new StoragePolicy
        {
            Name = "files",
            StorageProvider = StorageProvider.S3,
            ConnectionStringName = "S3_CONNECTION",
            BucketName = "my-app-files"
        }
    ],
    HttpClientPolicies =
    [
        new HttpClientPolicy
        {
            Name = "github",
            BaseAddress = "https://api.github.com/",
            TimeoutSeconds = 30,
            DefaultHeaders = new Dictionary<string, string> { ["User-Agent"] = "my-api" },
            ResiliencePolicy = new ResiliencePolicy
            {
                Retry = new RetryResiliencePolicy
                {
                    MaxRetryAttempts = 3,
                    DelayStrategy = RetryDelayStrategy.Linear,
                    DelayMilliseconds = 250,
                    DelayIncrementMilliseconds = 250
                }
            }
        }
    ],
    ConfigureAuthentication = true,
    ConfigureAuthorization = true,
    AuthManifest = new AuthManifest
    {
        EnableJwt = true,
        EnableCookies = false,
        EnableCsrfProtection = false,
        EnableApiKeyAuth = true,
        Policies =
        [
            new AuthPolicy { PolicyName = "AdminOnly", RequiredRoles = ["Admin"] },
            new AuthPolicy
            {
                PolicyName = "PremiumUser",
                RequiredClaims = new Dictionary<string, string> { ["subscription"] = "premium" }
            }
        ]
    },
    EnableRateLimit = true,
    RateLimitPolicies =
    [
        new RateLimitPolicy
        {
            PolicyName = "default",
            Algorithm = RateLimitAlgorithm.FixedWindow,
            Scope = RateLimitScope.IpAddress,
            PermitLimit = 100,
            Window = TimeSpan.FromMinutes(1)
        }
    ],
    EnableIdempotency = true,
    IdempotencyPolicy = new IdempotencyPolicy
    {
        RequireHeader = true,
        ExpirationMinutes = 60
    },
    EnableApiVersioning = true,
    ApiVersioningPolicy = new ApiVersioningPolicy
    {
        Readers = [ApiVersionReaderType.Header],
        HeaderNames = ["x-api-version"],
        DefaultMajorVersion = 1,
        DefaultMinorVersion = 0,
        EnableApiExplorer = true
    },
    EnableSwagger = true,
    SwaggerPolicy = new SwaggerPolicy
    {
        Title = "My API",
        Description = "Service API documentation",
        Headers =
        [
            new SwaggerHeaderDefinition
            {
                Name = "x-tenant-id",
                Description = "Tenant partition key",
                Required = false,
                AuthorizedOnly = false
            }
        ]
    },
    EnableWebhookSignatureValidation = true,
    WebhookSignaturePolicies =
    [
        new WebhookSignaturePolicy
        {
            Name = "stripe",
            PathPrefix = "/webhooks/stripe",
            SecretName = "STRIPE_WEBHOOK_SECRET"
        }
    ]
};
```

## Equivalent of appsettings.json

```json
{
  "AbsoluteCommon": {
    "enableRelationalDatabase": true,
    "databasePolicies": [
      {
        "name": "primary",
        "databaseProvider": "PostgreSQL",
        "connectionStringName": "PRIMARY_DB_CONNECTION",
        "maxPoolSize": 100,
        "commandTimeoutSeconds": 30,
        "resiliencePolicy": {
          "retry": {
            "maxRetryAttempts": 3,
            "delayStrategy": "Exponential",
            "delayMilliseconds": 200,
            "backoffMultiplier": 2.0
          },
          "timeout": { "timeoutSeconds": 30 },
          "circuitBreaker": {
            "handledEventsAllowedBeforeBreaking": 5,
            "durationOfBreakSeconds": 30
          }
        }
      }
    ],
    "enableStorage": true,
    "storagePolicies": [
      {
        "name": "files",
        "storageProvider": "S3",
        "connectionStringName": "S3_CONNECTION",
        "bucketName": "my-app-files"
      }
    ],
    "httpClientPolicies": [
      {
        "name": "github",
        "baseAddress": "https://api.github.com/",
        "timeoutSeconds": 30,
        "defaultHeaders": { "User-Agent": "my-api" },
        "resiliencePolicy": {
          "retry": {
            "maxRetryAttempts": 3,
            "delayStrategy": "Linear",
            "delayMilliseconds": 250,
            "delayIncrementMilliseconds": 250
          }
        }
      }
    ],
    "configureAuthentication": true,
    "configureAuthorization": true,
    "authManifest": {
      "enableJwt": true,
      "enableCookies": false,
      "enableApiKeyAuth": true,
      "policies": [
        { "policyName": "AdminOnly", "requiredRoles": ["Admin"] }
      ]
    },
    "enableRateLimit": true,
    "rateLimitPolicies": [
      {
        "policyName": "default",
        "algorithm": "FixedWindow",
        "scope": "IpAddress",
        "permitLimit": 100,
        "window": "00:01:00"
      }
    ],
    "enableIdempotency": true,
    "idempotencyPolicy": {
      "requireHeader": true,
      "expirationMinutes": 60
    },
    "enableApiVersioning": true,
    "apiVersioningPolicy": {
      "readers": ["Header"],
      "headerNames": ["x-api-version"],
      "enableApiExplorer": true
    },
    "enableSwagger": true,
    "swaggerPolicy": {
      "title": "My API",
      "description": "Service API documentation"
    },
    "enableWebhookSignatureValidation": true,
    "webhookSignaturePolicies": [
      {
        "name": "stripe",
        "pathPrefix": "/webhooks/stripe",
        "secretName": "STRIPE_WEBHOOK_SECRET"
      }
    ]
  }
}
```

---

## Tips

- Prefer `QueryInterpolatedAsync` / `ExecuteInterpolatedAsync` over raw SQL for value safety.
- Keep SQL identifiers server-owned — never accept table/column names from users.
- Enable CSRF only for cookie-authenticated APIs.
- Use `AuthorizeKeyAttribute` for machine-to-machine endpoints.
- For distributed idempotency, implement `IIdempotencyStore`.
- If startup schema creation is risky, leave `InitializeDatabase = false`.
- Keep `JWT_SECRET` strong and rotated.