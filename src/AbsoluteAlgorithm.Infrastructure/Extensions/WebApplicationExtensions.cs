using AbsoluteAlgorithm.Infrastructure.Database;
using AbsoluteAlgorithm.Infrastructure.Middlewares;
using AbsoluteAlgorithm.Core.Enums;
using AbsoluteAlgorithm.Core.Models.Auth;
using AbsoluteAlgorithm.Core.Models.Configuration;
using AbsoluteAlgorithm.Infrastructure.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using AbsoluteAlgorithm.Core.Models.Idempotency;
using AbsoluteAlgorithm.Core.Models.Webhooks;
using AbsoluteAlgorithm.Core.Models.Documentation;

namespace AbsoluteAlgorithm.Infrastructure.Extensions;

/// <summary>
/// Provides extension methods for configuring the application pipeline.
/// </summary>
public static class IApplicationBuilderExtensions
{

    #region SQL Scripts

    private const string postgresAuditSetup = @"
    CREATE TABLE IF NOT EXISTS audit_logs (
        id SERIAL PRIMARY KEY,
        entityname VARCHAR(255) NOT NULL,
        entityid INT NOT NULL,
        operation VARCHAR(50) NOT NULL,
        olddata JSONB,
        newdata JSONB,
        changedby VARCHAR(255),
        correlationid VARCHAR(255),
        createdat TIMESTAMPTZ DEFAULT CURRENT_TIMESTAMP
    );

    CREATE OR REPLACE FUNCTION fn_absolute_audit_handler()
    RETURNS TRIGGER AS $$
    BEGIN
        INSERT INTO audit_logs (entityname, entityid, operation, olddata, newdata, changedby, correlationid)
        VALUES (
            TG_TABLE_NAME,
            COALESCE(NEW.id, OLD.id),
            TG_OP,
            CASE WHEN TG_OP IN ('UPDATE', 'DELETE') THEN to_jsonb(OLD) ELSE NULL END,
            CASE WHEN TG_OP IN ('INSERT', 'UPDATE') THEN to_jsonb(NEW) ELSE NULL END,
            current_setting('app.user_id', true),
            current_setting('app.correlation_id', true)
        );
        RETURN NULL;
    END;
    $$ LANGUAGE plpgsql;";

    private const string postgresDiscoveryLoop = @"
DO $$ 
DECLARE 
    t_name text;
BEGIN
    FOR t_name IN 
        SELECT table_name 
        FROM information_schema.tables 
        WHERE table_schema = 'public' 
          AND table_type = 'BASE TABLE' 
          AND table_name != 'audit_logs'
    LOOP
        -- %I is for Identifiers (tables/columns), %s is for strings.
        -- We use a single % because this is a verbatim C# string (@).
        EXECUTE format('CREATE OR REPLACE TRIGGER tr_audit_%I AFTER INSERT OR UPDATE OR DELETE ON %I FOR EACH ROW EXECUTE FUNCTION fn_absolute_audit_handler()', t_name, t_name);
    END LOOP;
END $$;";

    private const string mssqlAuditTable = @"
    IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'audit_logs')
    BEGIN
        CREATE TABLE audit_logs (
            id INT IDENTITY(1,1) PRIMARY KEY,
            entityname NVARCHAR(255) NOT NULL,
            entityid INT NOT NULL,
            operation NVARCHAR(50) NOT NULL,
            olddata NVARCHAR(MAX),
            newdata NVARCHAR(MAX),
            changedby NVARCHAR(255),
            correlationid NVARCHAR(255),
            createdat DATETIMEOFFSET DEFAULT SYSDATETIMEOFFSET()
        );
    END;";

    // Part A: The Clean-up (Batch 1)
    private const string mssqlDropProcedure = @"
IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[sp_EnableAbsoluteAudit]') AND type in (N'P', N'PC'))
    DROP PROCEDURE [dbo].[sp_EnableAbsoluteAudit];";

    // Part B: The Definition (Batch 2) - Notice NO 'GO' here
    private const string mssqlCreateProcedure = @"
CREATE PROCEDURE [dbo].[sp_EnableAbsoluteAudit] @TableName NVARCHAR(255)
AS
BEGIN
    DECLARE @sql NVARCHAR(MAX);
    SET @sql = N'
    IF EXISTS (SELECT * FROM sys.tables WHERE name = ''' + @TableName + ''')
    BEGIN
        IF NOT EXISTS (SELECT * FROM sys.triggers WHERE name = ''tr_audit_' + @TableName + ''')
        BEGIN
            EXEC(''CREATE TRIGGER tr_audit_' + @TableName + ' ON [' + @TableName + '] AFTER INSERT, UPDATE, DELETE AS
            BEGIN
                SET NOCOUNT ON;
                INSERT INTO audit_logs (entityname, entityid, operation, olddata, newdata, changedby, correlationid)
                SELECT 
                    ''''' + @TableName + ''''',
                    ISNULL(i.id, d.id),
                    CASE 
                        WHEN i.id IS NOT NULL AND d.id IS NOT NULL THEN ''''UPDATE''''
                        WHEN i.id IS NOT NULL THEN ''''INSERT''''
                        ELSE ''''DELETE''''
                    END,
                    (SELECT d.* FOR JSON PATH, WITHOUT_ARRAY_WRAPPER),
                    (SELECT i.* FOR JSON PATH, WITHOUT_ARRAY_WRAPPER),
                    CAST(SESSION_CONTEXT(N''''user_id'''') AS NVARCHAR(255)),
                    CAST(SESSION_CONTEXT(N''''correlation_id'''') AS NVARCHAR(255))
                FROM inserted i FULL OUTER JOIN deleted d ON i.id = d.id;
            END'');
        END
    END';
    EXEC sp_executesql @sql;
END;";

    private const string mssqlDiscoveryLoop = @"
    DECLARE @TName NVARCHAR(255);
    DECLARE C CURSOR FOR SELECT name FROM sys.tables WHERE name != 'audit_logs' AND name != 'sysdiagrams';
    OPEN C; FETCH NEXT FROM C INTO @TName;
    WHILE @@FETCH_STATUS = 0 BEGIN
        EXEC sp_EnableAbsoluteAudit @TableName = @TName;
        FETCH NEXT FROM C INTO @TName;
    END;
    CLOSE C; DEALLOCATE C;";

    #endregion

    /// <summary>
    /// Configures the application pipeline for the specified library configuration.
    /// </summary>
    /// <param name="app">The application to configure.</param>
    /// <param name="appConfig">The application configuration.</param>
    /// <returns>The <paramref name="app"/> instance.</returns>
    public static WebApplication UseAbsolutePipeline(this WebApplication app, ApplicationConfiguration appConfig)
    {
        app.UseForwardedHeaders();

        if (!app.Environment.IsDevelopment())
        {
            app.UseHsts();
            app.UseHttpsRedirection();
        }

        if (appConfig.EnableRelationalDatabase)
        {
            InitializeDatabase(app, appConfig);
        }

        InitializeStorage(app, appConfig);

        app.UseRouting();

        if (appConfig.EnableRateLimit)
        {
            app.UseRateLimiter();
        }

        app.UseCorrelationId();
        app.UseExceptionMiddleware();
        if (appConfig.LoggingConfiguration is not null && appConfig.LoggingConfiguration.EnableRequestAndResponseLogging)
        {
            app.UseRequestResponseLogging();
        }

        if (appConfig.EnableWebhookSignatureValidation)
        {
            app.UseAbsoluteWebhookSignatureValidation(appConfig.WebhookSignaturePolicies);
        }
        app.UseResponseCompression();
        app.UseResponseCaching();
        app.UseStaticFiles();
        app.UseAbsoluteSwagger(appConfig.SwaggerPolicy);

        app.UseAuthentication();
        app.UseAbsoluteCsrfProtection(appConfig.AuthManifest);
        app.UseAuthorization();
        app.UseAbsoluteIdempotency(appConfig.IdempotencyPolicy);

        if (appConfig.EnableRelationalDatabase)
        {
            app.UseAbsoluteDatabase();
        }

        app.UseAbsoluteHealthEndpoints(appConfig.EnableHealthChecks);

        app.MapGet("/", () => $"API is Operational");
        app.MapControllers();

        return app;
    }

    private static IApplicationBuilder UseAbsoluteCsrfProtection(this IApplicationBuilder app, AuthManifest? authManifest)
    {
        if (authManifest?.EnableCookies != true || authManifest.EnableCsrfProtection != true)
        {
            return app;
        }

        return app.UseMiddleware<CsrfMiddleware>();
    }

    private static IApplicationBuilder UseAbsoluteIdempotency(this IApplicationBuilder app, IdempotencyPolicy? policy)
    {
        if (policy is null)
        {
            return app;
        }

        return app.UseMiddleware<IdempotencyMiddleware>();
    }

    private static IApplicationBuilder UseAbsoluteWebhookSignatureValidation(this IApplicationBuilder app, IReadOnlyList<WebhookSignaturePolicy>? policies)
    {
        if (policies is null || !policies.Any())
        {
            return app;
        }

        return app.UseMiddleware<WebhookSignatureMiddleware>();
    }

    private static IApplicationBuilder UseAbsoluteSwagger(this IApplicationBuilder app, SwaggerPolicy? policy)
    {
        if (policy is null)
        {
            return app;
        }

        app.UseOpenApi(settings =>
        {
            settings.Path = policy.OpenApiPath;
        });

        if (policy.UseSwaggerUi)
        {
            app.UseSwaggerUi(settings =>
            {
                settings.Path = policy.SwaggerUiPath;
                settings.DocumentTitle = policy.Title;
                settings.EnableTryItOut = policy.EnableTryItOut;
                settings.PersistAuthorization = policy.PersistAuthorization;
            });
        }

        return app;
    }

    private static void InitializeDatabase(IApplicationBuilder app, ApplicationConfiguration configuration)
    {
        if (configuration.DatabasePolicies is null) return;

        var loggerFactory = app.ApplicationServices.GetRequiredService<ILoggerFactory>();
        var logger = loggerFactory.CreateLogger("DatabaseInitializer");

        foreach (var policy in configuration.DatabasePolicies.Where(x => x.InitializeDatabase))
        {
            string connectionString = Environment.GetEnvironmentVariable(policy.ConnectionStringName)!;

            DatabaseInitializer.Initialize(logger, connectionString, policy.DatabaseProvider, policy.InitializationScript!);

            if (policy.InitializeAuditTable)
            {
                switch (policy.DatabaseProvider)
                {
                    case DatabaseProvider.PostgreSQL:
                        DatabaseInitializer.Initialize(logger, connectionString, policy.DatabaseProvider, postgresAuditSetup);
                        DatabaseInitializer.Initialize(logger, connectionString, policy.DatabaseProvider, postgresDiscoveryLoop);
                        break;

                    case DatabaseProvider.MSSQL:
                        DatabaseInitializer.Initialize(logger, connectionString, policy.DatabaseProvider, mssqlAuditTable);
                        DatabaseInitializer.Initialize(logger, connectionString, policy.DatabaseProvider, mssqlDropProcedure);
                        DatabaseInitializer.Initialize(logger, connectionString, policy.DatabaseProvider, mssqlCreateProcedure);
                        DatabaseInitializer.Initialize(logger, connectionString, policy.DatabaseProvider, mssqlDiscoveryLoop);
                        break;
                }
            }
        }
    }

    private static void InitializeStorage(IApplicationBuilder app, ApplicationConfiguration configuration)
    {
        if (configuration.StoragePolicies is null || !configuration.StoragePolicies.Any()) return;

        var loggerFactory = app.ApplicationServices.GetRequiredService<ILoggerFactory>();
        var logger = loggerFactory.CreateLogger("StorageInitializer");

        foreach (var policy in configuration.StoragePolicies)
        {
            try
            {
                StorageFactory.EnsureBucketExistsAsync(policy).GetAwaiter().GetResult();
                logger.LogInformation("Bucket '{BucketName}' ensured for {Provider} ({PolicyName}).", policy.BucketName, policy.StorageProvider, policy.Name);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Could not ensure bucket '{BucketName}' for {Provider} ({PolicyName}).", policy.BucketName, policy.StorageProvider, policy.Name);
            }
        }
    }


    /// <summary>
    /// Maps the library health check endpoints.
    /// </summary>
    /// <param name="app">The application to configure.</param>
    /// <param name="enableHealthChecks">A value indicating whether health endpoints are enabled.</param>
    private static void UseAbsoluteHealthEndpoints(this WebApplication app, bool enableHealthChecks)
    {
        if (!enableHealthChecks)
        {
            return;
        }

        app.MapHealthChecks("/health", new HealthCheckOptions
        {
            ResponseWriter = (context, report) => WriteAbsoluteHealthResponse(context, report, includeDetails: true)
        });
    }

    private static async Task WriteAbsoluteHealthResponse(HttpContext context, HealthReport report, bool includeDetails)
    {
        context.Response.ContentType = "application/json";

        if (!includeDetails)
        {
            await WriteMinimalHealthResponse(context, report);
            return;
        }

        var response = new
        {
            status = report.Status.ToString(),
            totalDuration = report.TotalDuration.ToString(),
            entries = report.Entries.ToDictionary(
                entry => entry.Key,
                entry => new
                {
                    data = entry.Value.Data,
                    duration = entry.Value.Duration.ToString(),
                    status = entry.Value.Status.ToString(),
                    tags = entry.Value.Tags,
                    description = entry.Value.Description,
                    exception = entry.Value.Exception?.Message
                }
            )
        };

        var options = new System.Text.Json.JsonSerializerOptions
        {
            WriteIndented = true
        };

        await context.Response.WriteAsJsonAsync(response, options);
    }

    private static Task WriteMinimalHealthResponse(HttpContext context, HealthReport report)
    {
        context.Response.ContentType = "application/json";
        return context.Response.WriteAsJsonAsync(new
        {
            status = report.Status.ToString()
        });
    }

    /// <summary>
    /// Adds the request header middleware.
    /// </summary>
    /// <param name="app">The application builder.</param>
    /// <returns>The <paramref name="app"/> instance.</returns>
    public static IApplicationBuilder UseCorrelationId(this IApplicationBuilder app) => app.UseMiddleware<RequestHeaderMiddleware>();

    /// <summary>
    /// Adds the exception handling middleware.
    /// </summary>
    /// <param name="app">The application builder.</param>
    /// <returns>The <paramref name="app"/> instance.</returns>
    public static IApplicationBuilder UseExceptionMiddleware(this IApplicationBuilder app) => app.UseMiddleware<ExceptionMiddleware>();

    /// <summary>
    /// Adds the database transaction middleware.
    /// </summary>
    /// <param name="app">The application builder.</param>
    /// <returns>The <paramref name="app"/> instance.</returns>
    private static IApplicationBuilder UseAbsoluteDatabase(this IApplicationBuilder app) => app.UseMiddleware<DatabaseTransactionMiddleware>();

    /// <summary>
    /// Adds the request and response logging middleware.
    /// </summary>
    /// <param name="app">The application builder.</param>
    /// <returns>The <paramref name="app"/> instance.</returns>
    private static IApplicationBuilder UseRequestResponseLogging(this IApplicationBuilder app) => app.UseMiddleware<RequestResponseLoggingMiddleware>();
}