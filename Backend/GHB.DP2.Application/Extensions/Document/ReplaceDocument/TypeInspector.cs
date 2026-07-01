namespace GHB.DP2.Application.Extensions.Document.ReplaceDocument;

using System.Collections;
using System.Collections.Concurrent;

/// <summary>
/// Provides utilities for inspecting and categorizing .NET types
/// </summary>
public static class TypeInspector
{
    private static readonly ConcurrentDictionary<Type, bool> SimpleTypeCache = new();

    /// <summary>
    /// Determines if a type implements IEnumerable&lt;T&gt; and extracts the item type
    /// </summary>
    /// <param name="type">The type to inspect</param>
    /// <param name="itemType">The generic item type if enumerable</param>
    /// <returns>True if the type is enumerable (excluding string)</returns>
    public static bool IsEnumerable(Type type, out Type itemType)
    {
        itemType = null!;

        // Skip string type (it's IEnumerable but we don't want to treat it as a collection)
        if (type == typeof(string))
        {
            return false;
        }

        // Check if type implements IEnumerable<T>
        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IEnumerable<>))
        {
            itemType = type.GetGenericArguments()[0];

            return true;
        }

        // Check if type implements any IEnumerable<T> interface
        var enumerableInterface = type.GetInterfaces()
                                      .FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEnumerable<>));

        if (enumerableInterface != null)
        {
            itemType = enumerableInterface.GetGenericArguments()[0];

            return true;
        }

        return false;
    }

    /// <summary>
    /// Determines if a type is a Data Transfer Object (complex class type)
    /// </summary>
    /// <param name="type">The type to inspect</param>
    /// <returns>True if the type is a class and not string</returns>
    public static bool IsDto(Type type)
    {
        return type.IsClass && type != typeof(string);
    }

    /// <summary>
    /// Determines if a type is a simple value type or primitive
    /// </summary>
    /// <param name="type">The type to inspect</param>
    /// <returns>True if the type is considered simple</returns>
    public static bool IsSimple(Type type)
    {
        return SimpleTypeCache.GetOrAdd(type, t =>
            t.IsPrimitive ||
            t.IsEnum ||
            t == typeof(string) ||
            t == typeof(Guid) ||
            t == typeof(DateTime) ||
            t == typeof(DateTimeOffset) ||
            t == typeof(decimal) ||
            t == typeof(bool) ||
            (Nullable.GetUnderlyingType(t) is Type underlyingType && IsSimple(underlyingType)));
    }

    /// <summary>
    /// Gets the count of items in an enumerable collection
    /// </summary>
    /// <param name="enumerable">The enumerable to count</param>
    /// <returns>The count of items</returns>
    public static int GetCount(IEnumerable enumerable)
    {
        return enumerable is ICollection collection
            ? collection.Count
            : enumerable.Cast<object>().Count();
    }
}