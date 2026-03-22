using FileFormula.Api.Infrastructure.Models.Documentation;
using NJsonSchema;
using NSwag;
using NSwag.Generation.Processors;
using NSwag.Generation.Processors.Contexts;

namespace FileFormula.Api.Infrastructure.OpenApi;

internal sealed class SwaggerHeaderOperationProcessor(IReadOnlyList<SwaggerHeaderDefinition> headers) : IOperationProcessor
{
    private readonly IReadOnlyList<SwaggerHeaderDefinition> _headers = headers;

    public bool Process(OperationProcessorContext context)
    {
        if (_headers.Count == 0)
        {
            return true;
        }

        var methodInfo = context.MethodInfo;
        var controllerType = context.ControllerType;
        var requiresAuthorization = SwaggerOperationMetadata.RequiresAnyAuthorization(methodInfo, controllerType);
        var parameters = context.OperationDescription.Operation.Parameters;

        foreach (var header in _headers)
        {
            if (header.AuthorizedOnly && !requiresAuthorization)
            {
                continue;
            }

            if (parameters.Any(parameter => parameter.Kind == OpenApiParameterKind.Header && string.Equals(parameter.Name, header.Name, StringComparison.OrdinalIgnoreCase)))
            {
                continue;
            }

            parameters.Add(new OpenApiParameter
            {
                Name = header.Name,
                Kind = OpenApiParameterKind.Header,
                IsRequired = header.Required,
                Description = header.Description,
                Schema = new JsonSchema
                {
                    Type = JsonObjectType.String
                }
            });
        }

        return true;
    }
}