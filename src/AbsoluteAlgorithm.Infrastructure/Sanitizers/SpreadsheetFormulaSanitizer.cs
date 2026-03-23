using System;
using System.Collections.Generic;

namespace AbsoluteAlgorithm.Infrastructure.Sanitizers;

/// <summary>
/// Provides helpers for sanitizing values written to CSV or spreadsheet-compatible exports.
/// </summary>
/// <remarks>
/// Spreadsheet applications can interpret cells that begin with formula prefixes such as <c>=</c>, <c>+</c>, <c>-</c>, or <c>@</c> as executable formulas.
/// This type is also used by the MVC integration to sanitize controller-bound string input.
/// </remarks>
public static class SpreadsheetFormulaSanitizer
{
    private static readonly HashSet<char> DangerousLeadingCharacters = ['=', '+', '-', '@', '\t', '\r', '\n'];

    /// <summary>
    /// Returns a value that is safe to emit as a CSV or spreadsheet cell.
    /// </summary>
    /// <param name="value">The source cell value.</param>
    /// <returns>
    /// The original value when sanitization is not required; otherwise, the value prefixed with a single quote.
    /// </returns>
    /// <remarks>
    /// A leading single quote is commonly used to force spreadsheet applications to treat the cell as text.
    /// </remarks>
    public static string? SanitizeCell(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return value;
        }

        if (!RequiresSanitization(value) || value[0] == '\'')
        {
            return value;
        }

        return $"'{value}";
    }

    /// <summary>
    /// Returns values that are safe to emit as CSV or spreadsheet cells.
    /// </summary>
    /// <param name="values">The source cell values.</param>
    /// <returns>A sequence of sanitized cell values.</returns>
    public static IEnumerable<string?> SanitizeCells(IEnumerable<string?> values)
    {
        ArgumentNullException.ThrowIfNull(values);

        foreach (var value in values)
        {
            yield return SanitizeCell(value);
        }
    }

    /// <summary>
    /// Determines whether a cell value should be sanitized for spreadsheet export.
    /// </summary>
    /// <param name="value">The source cell value.</param>
    /// <returns><see langword="true"/> when the value begins with a known spreadsheet formula prefix; otherwise, <see langword="false"/>.</returns>
    public static bool RequiresSanitization(string? value)
    {
        return !string.IsNullOrEmpty(value) && DangerousLeadingCharacters.Contains(value[0]);
    }
}