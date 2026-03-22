using System;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace FileFormula.Api.Infrastructure.Sanitizers;

/// <summary>
/// Supplies the string model binder that sanitizes controller-bound string values.
/// </summary>
public sealed class SpreadsheetFormulaSanitizingStringModelBinderProvider : IModelBinderProvider
{
    /// <summary>
    /// Returns a model binder for string values.
    /// </summary>
    /// <param name="context">The provider context.</param>
    /// <returns>A model binder for <see cref="string"/>, or <see langword="null"/> for other types.</returns>
    public IModelBinder? GetBinder(ModelBinderProviderContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return context.Metadata.ModelType == typeof(string)
            ? new SpreadsheetFormulaSanitizingStringModelBinder()
            : null;
    }
}