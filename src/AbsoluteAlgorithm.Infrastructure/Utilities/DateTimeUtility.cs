namespace AbsoluteAlgorithm.Infrastructure.Utilities;

/// <summary>
/// Provides helper methods for working with UTC timestamps and Unix time values.
/// </summary>
public static class DateTimeUtility
{
    /// <summary>
    /// Ensures that the supplied value is represented in UTC.
    /// </summary>
    /// <param name="value">The date and time value.</param>
    /// <returns>The UTC representation of the supplied value.</returns>
    public static DateTime EnsureUtc(DateTime value)
    {
        return value.Kind switch
        {
            DateTimeKind.Utc => value,
            DateTimeKind.Local => value.ToUniversalTime(),
            _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
        };
    }

    /// <summary>
    /// Returns the UTC start of day for the supplied value.
    /// </summary>
    /// <param name="value">The source date and time.</param>
    /// <returns>The UTC start of day.</returns>
    public static DateTime StartOfDayUtc(DateTime value)
    {
        return EnsureUtc(value).Date;
    }

    /// <summary>
    /// Returns the UTC end of day for the supplied value.
    /// </summary>
    /// <param name="value">The source date and time.</param>
    /// <returns>The UTC end of day.</returns>
    public static DateTime EndOfDayUtc(DateTime value)
    {
        return StartOfDayUtc(value).AddDays(1).AddTicks(-1);
    }

    /// <summary>
    /// Converts a date and time value to Unix time seconds.
    /// </summary>
    /// <param name="value">The source date and time.</param>
    /// <returns>The Unix time seconds.</returns>
    public static long ToUnixTimeSeconds(DateTime value)
    {
        return new DateTimeOffset(EnsureUtc(value)).ToUnixTimeSeconds();
    }

    /// <summary>
    /// Converts Unix time seconds to a UTC date and time value.
    /// </summary>
    /// <param name="unixTimeSeconds">The Unix time seconds.</param>
    /// <returns>The UTC date and time.</returns>
    public static DateTime FromUnixTimeSeconds(long unixTimeSeconds)
    {
        return DateTimeOffset.FromUnixTimeSeconds(unixTimeSeconds).UtcDateTime;
    }
}