using System.Reflection;
using FileFormula.Api.Infrastructure.Filters;
using Microsoft.AspNetCore.Authorization;

namespace FileFormula.Api.Infrastructure.OpenApi;

internal static class SwaggerOperationMetadata
{
    public static bool AllowsAnonymous(MethodInfo methodInfo, Type? controllerType)
    {
        return methodInfo.IsDefined(typeof(AllowAnonymousAttribute), inherit: true)
            || (controllerType is not null && controllerType.IsDefined(typeof(AllowAnonymousAttribute), inherit: true));
    }

    public static bool RequiresAuthorize(MethodInfo methodInfo, Type? controllerType)
    {
        return !AllowsAnonymous(methodInfo, controllerType)
            && GetAuthorizeAttributes(methodInfo, controllerType).Count > 0;
    }

    public static IReadOnlyList<AuthorizeAttribute> GetAuthorizeAttributes(MethodInfo methodInfo, Type? controllerType)
    {
        return methodInfo.GetCustomAttributes<AuthorizeAttribute>(inherit: true)
            .Concat(controllerType?.GetCustomAttributes<AuthorizeAttribute>(inherit: true) ?? Array.Empty<AuthorizeAttribute>())
            .ToArray();
    }

    public static IReadOnlyList<AuthorizeKeyAttribute> GetAuthorizeKeyAttributes(MethodInfo methodInfo, Type? controllerType)
    {
        if (AllowsAnonymous(methodInfo, controllerType))
        {
            return Array.Empty<AuthorizeKeyAttribute>();
        }

        return methodInfo.GetCustomAttributes<AuthorizeKeyAttribute>(inherit: true)
            .Concat(controllerType?.GetCustomAttributes<AuthorizeKeyAttribute>(inherit: true) ?? Array.Empty<AuthorizeKeyAttribute>())
            .ToArray();
    }

    public static bool RequiresAnyAuthorization(MethodInfo methodInfo, Type? controllerType)
    {
        return RequiresAuthorize(methodInfo, controllerType)
            || GetAuthorizeKeyAttributes(methodInfo, controllerType).Count > 0;
    }
}