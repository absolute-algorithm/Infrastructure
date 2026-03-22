using System.Collections.Concurrent;
using System.Reflection;

namespace FileFormula.Api.Infrastructure.Utilities;

/// <summary>
/// Provides helper methods for reading cached reflection metadata.
/// </summary>
public static class ReflectionUtility
{
    private static readonly ConcurrentDictionary<Type, PropertyInfo[]> PublicInstanceProperties = new();
    private static readonly ConcurrentDictionary<Type, PropertyInfo[]> PublicReadableProperties = new();
    private static readonly ConcurrentDictionary<Type, PropertyInfo[]> PublicWritableProperties = new();

    /// <summary>
    /// Returns the public instance properties for the specified type.
    /// </summary>
    /// <param name="type">The type to inspect.</param>
    /// <returns>The public instance properties.</returns>
    public static IReadOnlyList<PropertyInfo> GetPublicInstanceProperties(Type type)
    {
        ArgumentNullException.ThrowIfNull(type);

        return PublicInstanceProperties.GetOrAdd(
            type,
            static currentType => currentType.GetProperties(BindingFlags.Instance | BindingFlags.Public));
    }

    /// <summary>
    /// Returns the public readable instance properties for the specified type.
    /// </summary>
    /// <param name="type">The type to inspect.</param>
    /// <returns>The public readable instance properties.</returns>
    public static IReadOnlyList<PropertyInfo> GetReadableProperties(Type type)
    {
        ArgumentNullException.ThrowIfNull(type);

        return PublicReadableProperties.GetOrAdd(
            type,
            static currentType => currentType.GetProperties(BindingFlags.Instance | BindingFlags.Public).Where(property => property.CanRead).ToArray());
    }

    /// <summary>
    /// Returns the public writable instance properties for the specified type.
    /// </summary>
    /// <param name="type">The type to inspect.</param>
    /// <returns>The public writable instance properties.</returns>
    public static IReadOnlyList<PropertyInfo> GetWritableProperties(Type type)
    {
        ArgumentNullException.ThrowIfNull(type);

        return PublicWritableProperties.GetOrAdd(
            type,
            static currentType => currentType.GetProperties(BindingFlags.Instance | BindingFlags.Public).Where(property => property.CanWrite).ToArray());
    }

    /// <summary>
    /// Returns the public instance properties for the specified generic type.
    /// </summary>
    /// <typeparam name="T">The type to inspect.</typeparam>
    /// <returns>The public instance properties.</returns>
    public static IReadOnlyList<PropertyInfo> GetPublicInstanceProperties<T>()
    {
        return GetPublicInstanceProperties(typeof(T));
    }

    /// <summary>
    /// Returns the public readable instance properties for the specified generic type.
    /// </summary>
    /// <typeparam name="T">The type to inspect.</typeparam>
    /// <returns>The public readable instance properties.</returns>
    public static IReadOnlyList<PropertyInfo> GetReadableProperties<T>()
    {
        return GetReadableProperties(typeof(T));
    }

    /// <summary>
    /// Returns the public writable instance properties for the specified generic type.
    /// </summary>
    /// <typeparam name="T">The type to inspect.</typeparam>
    /// <returns>The public writable instance properties.</returns>
    public static IReadOnlyList<PropertyInfo> GetWritableProperties<T>()
    {
        return GetWritableProperties(typeof(T));
    }
}