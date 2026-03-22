
# FileFormula.Api.Infrastructure


Reusable ASP.NET Core Web API infrastructure library. Configure once, get databases, storage, auth, resilience, docs, and more — wired automatically.


```bash
dotnet add package FileFormula.Api.Infrastructure
```

---

To use the package from the GitLab NuGet feed:

```bash
dotnet nuget add source "https://gitlab.com/api/v4/groups/127290252/-/packages/nuget/index.json" --name FileFormulaOrg --username <your-gitlab-username> --password <your-personal-access-token> --store-password-in-clear-text
dotnet add package FileFormula.Api.Infrastructure
```

> Targets `net10.0`. Bundles Dapper, Polly, NSwag, NLog, CsvHelper, and provider SDKs for S3, Azure Blob, GCP Storage, and MinIO.

---

## Quick Start

```csharp
var builder = WebApplication.CreateBuilder(args);

var appConfig = new ApplicationConfiguration
{
    EnableRelationalDatabase = true,
    DatabasePolicies =
    [
        new DatabasePolicy
        {
            Name = "primary",
            DatabaseProvider = DatabaseProvider.PostgreSQL,
            ConnectionStringName = "PRIMARY_DB_CONNECTION"
        }
    ],
    ConfigureAuthentication = true,
    ConfigureAuthorization = true,
    AuthManifest = new AuthManifest
    {
        EnableJwt = true,
        EnableCookies = false
    }
};

builder.RegisterAbsoluteWebApplicationBuilder(appConfig);

var app = builder.Build();

app.UseAbsolutePipeline(appConfig);

app.Run();
```

Two calls. That's it. The library validates config, registers services, and builds the middleware pipeline.

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

### Usage

```csharp
var storage = sp.GetRequiredKeyedService<StorageService>("files");

// Upload
var objectName = await storage.UploadAsync(new FileContent
{
    FileName = "photo.png",
    ByteArrayContent = bytes,
    ContentType = "image/png"
});

// Download URL
var url = await storage.GetDownloadUrlAsync(objectName, TimeSpan.FromMinutes(15));
```

Filenames are sanitized automatically. Extensions are preserved.

---

## HTTP Clients

Named clients registered from `HttpClientPolicy`, resolved via `IHttpClientFactory`.

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

`UseAbsolutePipeline` wires everything in this order:

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