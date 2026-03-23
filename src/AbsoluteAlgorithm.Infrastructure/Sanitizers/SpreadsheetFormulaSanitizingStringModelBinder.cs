using System;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace AbsoluteAlgorithm.Infrastructure.Sanitizers;

/// <summary>
/// Sanitizes controller-bound string values for non-body inputs such as query, route, and form data.
/// </summary>
public sealed class SpreadsheetFormulaSanitizingStringModelBinder : IModelBinder
{
    /// <summary>
    /// Binds and sanitizes the current model value.
    /// </summary>
    /// <param name="bindingContext">The model binding context.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public Task BindModelAsync(ModelBindingContext bindingContext)
    {
        ArgumentNullException.ThrowIfNull(bindingContext);

        var valueResult = bindingContext.ValueProvider.GetValue(bindingContext.ModelName);
        if (valueResult == ValueProviderResult.None)
        {
            return Task.CompletedTask;
        }

        bindingContext.ModelState.SetModelValue(bindingContext.ModelName, valueResult);

        var value = valueResult.FirstValue;
        bindingContext.Result = ModelBindingResult.Success(SpreadsheetFormulaSanitizer.SanitizeCell(value));

        return Task.CompletedTask;
    }
}