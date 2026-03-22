namespace FileFormula.Api.Infrastructure.Enums;

/// <summary>
/// Specifies the comparison applied by a filter descriptor.
/// </summary>
public enum FilterOperator : byte
{
    /// <summary>
    /// Matches values that are equal.
    /// </summary>
    Equals = 1,

    /// <summary>
    /// Matches values that are not equal.
    /// </summary>
    NotEquals,

    /// <summary>
    /// Matches values that contain the supplied text.
    /// </summary>
    Contains,

    /// <summary>
    /// Matches values that start with the supplied text.
    /// </summary>
    StartsWith,

    /// <summary>
    /// Matches values that end with the supplied text.
    /// </summary>
    EndsWith,

    /// <summary>
    /// Matches values greater than the supplied value.
    /// </summary>
    GreaterThan,

    /// <summary>
    /// Matches values greater than or equal to the supplied value.
    /// </summary>
    GreaterThanOrEqual,

    /// <summary>
    /// Matches values less than the supplied value.
    /// </summary>
    LessThan,

    /// <summary>
    /// Matches values less than or equal to the supplied value.
    /// </summary>
    LessThanOrEqual,

    /// <summary>
    /// Matches values included in the supplied set.
    /// </summary>
    In,

    /// <summary>
    /// Matches values that fall between the supplied bounds.
    /// </summary>
    Between
}