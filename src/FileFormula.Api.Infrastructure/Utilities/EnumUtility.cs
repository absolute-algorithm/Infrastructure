namespace FileFormula.Api.Infrastructure.Utilities;

/// <summary>
/// Provides helper methods for working with enumeration values.
/// </summary>
public static class EnumUtility
{
    /// <summary>
    /// Parses an enumeration value.
    /// </summary>
    /// <typeparam name="TEnum">The enumeration type.</typeparam>
    /// <param name="value">The source string.</param>
    /// <param name="ignoreCase"><see langword="true"/> to ignore case during parsing; otherwise, <see langword="false"/>.</param>
    /// <returns>The parsed enumeration value.</returns>
    public static TEnum Parse<TEnum>(string value, bool ignoreCase = true) where TEnum : struct, Enum
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);

        return Enum.Parse<TEnum>(value, ignoreCase);
    }

    /// <summary>
    /// Attempts to parse an enumeration value.
    /// </summary>
    /// <typeparam name="TEnum">The enumeration type.</typeparam>
    /// <param name="value">The source string.</param>
    /// <param name="result">The parsed enumeration value when parsing succeeds.</param>
    /// <param name="ignoreCase"><see langword="true"/> to ignore case during parsing; otherwise, <see langword="false"/>.</param>
    /// <returns><see langword="true"/> when parsing succeeds; otherwise, <see langword="false"/>.</returns>
    public static bool TryParse<TEnum>(string? value, out TEnum result, bool ignoreCase = true) where TEnum : struct, Enum
    {
        return Enum.TryParse(value, ignoreCase, out result);
    }

    /// <summary>
    /// Returns the names defined for an enumeration type.
    /// </summary>
    /// <typeparam name="TEnum">The enumeration type.</typeparam>
    /// <returns>The enumeration names.</returns>
    public static IReadOnlyList<string> GetNames<TEnum>() where TEnum : struct, Enum
    {
        return Enum.GetNames<TEnum>();
    }

    /// <summary>
    /// Returns the values defined for an enumeration type.
    /// </summary>
    /// <typeparam name="TEnum">The enumeration type.</typeparam>
    /// <returns>The enumeration values.</returns>
    public static IReadOnlyList<TEnum> GetValues<TEnum>() where TEnum : struct, Enum
    {
        return Enum.GetValues<TEnum>();
    }

    /// <summary>
    /// Determines whether the supplied enumeration value is defined.
    /// </summary>
    /// <typeparam name="TEnum">The enumeration type.</typeparam>
    /// <param name="value">The enumeration value.</param>
    /// <returns><see langword="true"/> when the value is defined; otherwise, <see langword="false"/>.</returns>
    public static bool IsDefined<TEnum>(TEnum value) where TEnum : struct, Enum
    {
        return Enum.IsDefined(value);
    }
}