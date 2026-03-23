using AbsoluteAlgorithm.Infrastructure.Filters;
using NJsonSchema;
using NSwag;
using NSwag.Generation.Processors;
using NSwag.Generation.Processors.Contexts;

namespace AbsoluteAlgorithm.Infrastructure.OpenApi;

internal sealed class SwaggerAuthorizeKeyOperationProcessor : IOperationProcessor
{
    public bool Process(OperationProcessorContext context)
    {
        var authorizeKeyAttributes = SwaggerOperationMetadata.GetAuthorizeKeyAttributes(context.MethodInfo, context.ControllerType);
        if (authorizeKeyAttributes.Count == 0)
        {
            return true;
        }

        foreach (var attribute in authorizeKeyAttributes.DistinctBy(attribute => attribute.HeaderName, StringComparer.OrdinalIgnoreCase))
        {
            var schemeName = $"ApiKey_{attribute.HeaderName}";
            EnsureSecurityScheme(context.Document, schemeName, attribute.HeaderName);
            EnsureSecurityRequirement(context.OperationDescription.Operation, schemeName);
            EnsureHeaderParameter(context.OperationDescription.Operation, attribute.HeaderName, Array.Empty<string>());
        }

        return true;
    }

    private static void EnsureSecurityScheme(OpenApiDocument document, string schemeName, string headerName)
    {
        if (document.SecurityDefinitions.ContainsKey(schemeName))
        {
            return;
        }

        document.SecurityDefinitions[schemeName] = new OpenApiSecurityScheme
        {
            Type = OpenApiSecuritySchemeType.ApiKey,
            Name = headerName,
            In = OpenApiSecurityApiKeyLocation.Header,
            Description = $"Provide the API key using the '{headerName}' header."
        };
    }

    private static void EnsureSecurityRequirement(OpenApiOperation operation, string schemeName)
    {
        operation.Security ??= new List<OpenApiSecurityRequirement>();

        if (operation.Security.Any(requirement => requirement.ContainsKey(schemeName)))
        {
            return;
        }

        operation.Security.Add(new OpenApiSecurityRequirement
        {
            { schemeName, Array.Empty<string>() }
        });
    }

    private static void EnsureHeaderParameter(OpenApiOperation operation, string headerName, string[] secretNames)
    {
        if (operation.Parameters.Any(parameter => parameter.Kind == OpenApiParameterKind.Header && string.Equals(parameter.Name, headerName, StringComparison.OrdinalIgnoreCase)))
        {
            return;
        }

        operation.Parameters.Add(new OpenApiParameter
        {
            Name = headerName,
            Kind = OpenApiParameterKind.Header,
            IsRequired = true,
            Description = $"API key header backed by environment variable(s): {string.Join(", ", secretNames)}.",
            Schema = new JsonSchema
            {
                Type = JsonObjectType.String
            }
        });
    }
}