namespace GHB.DP2.Application.Extensions.Document.ReplaceDocument;

using System.Collections;
using System.Collections.Concurrent;
using System.ComponentModel;
using System.Reflection;

/// <summary>
/// Provides functionality for mapping object properties to path strings for document template replacement
/// </summary>
public static class PropertyPathMapper
{
    private static readonly ConcurrentDictionary<Type, PropertyInfo[]> PropertyCache = new();
    private static readonly ConcurrentDictionary<string, PropertyInfo?> PropertyPathCache = new();

    /// <summary>
    /// Gets a single value from an object using a dot-separated property path
    /// </summary>
    /// <param name="obj">The source object</param>
    /// <param name="path">The dot-separated property path (e.g., "Person.Name")</param>
    /// <returns>The property value or null if not found</returns>
    public static object? GetSingleValueByPath(object? obj, string path)
    {
        if (!IsValidInput(obj, path))
        {
            return null;
        }

        var parts = path.Split('.');

        return TraversePath(obj, parts);
    }

    private static bool IsValidInput(object? obj, string? path)
    {
        return obj != null && !string.IsNullOrWhiteSpace(path);
    }

    private static object? TraversePath(object? current, string[] parts)
    {
        foreach (var part in parts)
        {
            if (current == null)
            {
                return null;
            }

            var property = GetCachedProperty(current.GetType(), part);

            if (property == null)
            {
                return null;
            }

            current = property.GetValue(current);
        }

        return current;
    }

    /// <summary>
    /// Gets multiple values from collections using a dot-separated property path
    /// </summary>
    /// <param name="obj">The source object</param>
    /// <param name="path">The property path containing collections</param>
    /// <returns>List of values from the collection properties</returns>
    public static List<object?> GetMultipleValuesByPath(object? obj, string path)
    {
        var result = new List<object?>();

        if (!IsValidInputForMultipleValues(obj, path))
        {
            return result;
        }

        var parts = path.Split('.');

        return TraversePathForMultipleValues(obj!, parts, result);
    }

    private static bool IsValidInputForMultipleValues(object? obj, string? path)
    {
        if (!IsValidInput(obj, path))
        {
            return false;
        }

        var parts = path!.Split('.');

        return parts.Length >= 2;
    }

    /// <summary>
    /// Gets the count of items in a collection at the specified path
    /// </summary>
    /// <param name="obj">The source object</param>
    /// <param name="path">The property path to the collection</param>
    /// <returns>The count of items in the collection</returns>
    public static int GetMultipleValuesCount(object? obj, string path)
    {
        if (!IsValidInputForMultipleValues(obj, path))
        {
            return 0;
        }

        var parts = path.Split('.');

        return TraversePathForCount(obj!, parts, 0);
    }

    /// <summary>
    /// Gets a specific indexed value from collections using a property path
    /// </summary>
    /// <param name="obj">The source object</param>
    /// <param name="path">The property path containing collections</param>
    /// <param name="index">The index of the item to retrieve</param>
    /// <returns>The value at the specified index or null</returns>
    public static object? GetMultipleValueByPathWithIndex(object? obj, string path, int index)
    {
        if (!IsValidInputForMultipleValues(obj, path))
        {
            return null;
        }

        var parts = path.Split('.');

        return TraversePathForIndexedValue(obj!, parts, index, 0);
    }

    /// <summary>
    /// Maps an object's properties to categorized path lists (single value vs multiple value)
    /// </summary>
    /// <param name="dto">The object to map</param>
    /// <returns>Tuple containing single value paths and multiple value paths</returns>
    public static (List<string> SingleValue, List<string> MultipleValue) MapDtoPaths(this object dto)
    {
        var singleValue = new List<string>();
        var multipleValue = new List<string>();
        var visitedTypes = new HashSet<Type>();

        var type = dto.GetType();
        var rootProperties = GetCachedProperties(type);

        foreach (var prop in rootProperties)
        {
            ProcessPropertyForPaths(prop, prop.Name, singleValue, multipleValue, visitedTypes);
        }

        return (singleValue, multipleValue);
    }

    /// <summary>
    /// Maps object properties to a dictionary with descriptions from DescriptionAttribute
    /// </summary>
    /// <param name="dto">The object to map</param>
    /// <returns>Dictionary mapping property paths to their descriptions</returns>
    public static Dictionary<string, string> MapDtoPathsWithDescriptions(this object dto)
    {
        var pathDescriptions = new Dictionary<string, string>();
        var type = dto.GetType();
        var rootProperties = GetCachedProperties(type);

        foreach (var prop in rootProperties)
        {
            ProcessPropertyForDescriptions(prop, prop.Name, pathDescriptions);
        }

        return pathDescriptions;
    }

    private static PropertyInfo? GetCachedProperty(Type type, string propertyName)
    {
        var cacheKey = $"{type.FullName}.{propertyName}";

        return PropertyPathCache.GetOrAdd(cacheKey, _ => type.GetProperty(propertyName));
    }

    private static PropertyInfo[] GetCachedProperties(Type type)
    {
        return PropertyCache.GetOrAdd(type, t => t.GetProperties(BindingFlags.Public | BindingFlags.Instance));
    }

    private static List<object?> TraversePathForMultipleValues(object current, string[] parts, List<object?> result)
    {
        for (int i = 0; i < parts.Length - 1; i++)
        {
            if (current == null)
            {
                return result;
            }

            var property = GetCachedProperty(current.GetType(), parts[i]);

            if (property == null)
            {
                return result;
            }

            var propValue = property.GetValue(current);

            if (propValue is IEnumerable enumerable and not string)
            {
                return ProcessEnumerableForMultipleValues(enumerable, parts, i, result);
            }

            current = propValue!;
        }

        return result;
    }

    private static List<object?> ProcessEnumerableForMultipleValues(
        IEnumerable enumerable,
        string[] parts,
        int currentIndex,
        List<object?> result)
    {
        var remainingPath = string.Join('.', parts.Skip(currentIndex + 1));
        var enumerableList = enumerable.Cast<object>().ToList();

        if (remainingPath.Contains('.') && HasCollectionInPath(enumerableList, remainingPath))
        {
            return ProcessNestedCollections(enumerableList, remainingPath, result);
        }

        return ProcessSimpleCollection(enumerableList, remainingPath, result);
    }

    private static List<object?> ProcessNestedCollections(
        List<object> enumerableList,
        string remainingPath,
        List<object?> result)
    {
        foreach (var item in enumerableList)
        {
            var nestedValues = GetMultipleValuesByPath(item, remainingPath);
            result.AddRange(nestedValues);
        }

        return result;
    }

    private static List<object?> ProcessSimpleCollection(
        List<object> enumerableList,
        string remainingPath,
        List<object?> result)
    {
        foreach (var item in enumerableList)
        {
            var value = GetSingleValueByPath(item, remainingPath);
            result.Add(value);
        }

        return result;
    }

    private static int TraversePathForCount(object current, string[] parts, int currentIndex)
    {
        if (IsAtEndOfPath(currentIndex, parts.Length))
        {
            return 0;
        }

        var property = GetCachedProperty(current.GetType(), parts[currentIndex]);
        if (property == null)
        {
            return 0;
        }

        var propValue = property.GetValue(current);
        if (propValue is IEnumerable enumerable and not string)
        {
            return ProcessEnumerableForCount(enumerable, parts, currentIndex);
        }

        return propValue != null ? TraversePathForCount(propValue, parts, currentIndex + 1) : 0;
    }

    private static bool IsAtEndOfPath(int currentIndex, int totalParts)
    {
        return currentIndex >= totalParts - 1;
    }

    private static int ProcessEnumerableForCount(IEnumerable enumerable, string[] parts, int currentIndex)
    {
        if (currentIndex == parts.Length - 2)
        {
            return TypeInspector.GetCount(enumerable);
        }

        return GetMaxCountFromItems(enumerable, parts, currentIndex);
    }

    private static int GetMaxCountFromItems(IEnumerable enumerable, string[] parts, int currentIndex)
    {
        var maxCount = 0;
        foreach (var item in enumerable)
        {
            if (item != null)
            {
                var itemCount = TraversePathForCount(item, parts, currentIndex + 1);
                maxCount = Math.Max(maxCount, itemCount);
            }
        }

        return maxCount;
    }

    private static object? TraversePathForIndexedValue(object current, string[] parts, int targetIndex, int currentPartIndex)
    {
        if (IsAtEndOfPath(currentPartIndex, parts.Length))
        {
            return null;
        }

        var property = GetCachedProperty(current.GetType(), parts[currentPartIndex]);
        if (property == null)
        {
            return null;
        }

        var propValue = property.GetValue(current);
        if (propValue is IEnumerable enumerable and not string)
        {
            return ProcessEnumerableForIndexedValue(enumerable, parts, targetIndex, currentPartIndex);
        }

        return propValue != null ? TraversePathForIndexedValue(propValue, parts, targetIndex, currentPartIndex + 1) : null;
    }

    private static object? ProcessEnumerableForIndexedValue(
        IEnumerable enumerable,
        string[] parts,
        int targetIndex,
        int currentPartIndex)
    {
        var remainingPath = string.Join('.', parts.Skip(currentPartIndex + 1));
        var enumerableList = enumerable.Cast<object>().ToList();

        if (remainingPath.Contains('.') && HasCollectionInPath(enumerableList, remainingPath))
        {
            return GetIndexedValueFromNestedCollections(enumerableList, remainingPath, targetIndex);
        }

        return GetIndexedValueFromSimpleCollection(enumerableList, remainingPath, targetIndex);
    }

    private static object? GetIndexedValueFromNestedCollections(
        List<object> enumerableList,
        string remainingPath,
        int targetIndex)
    {
        var allValues = new List<object?>();
        foreach (var item in enumerableList)
        {
            var nestedValues = GetMultipleValuesByPath(item, remainingPath);
            allValues.AddRange(nestedValues);
        }

        return targetIndex >= 0 && targetIndex < allValues.Count ? allValues[targetIndex] : null;
    }

    private static object? GetIndexedValueFromSimpleCollection(
        List<object> enumerableList,
        string remainingPath,
        int targetIndex)
    {
        if (targetIndex >= 0 && targetIndex < enumerableList.Count)
        {
            return GetSingleValueByPath(enumerableList[targetIndex], remainingPath);
        }

        return null;
    }

    private static bool HasCollectionInPath(IList<object> enumerableList, string remainingPath)
    {
        var parts = remainingPath.Split('.');

        if (parts.Length < 2)
        {
            return false;
        }

        foreach (var item in enumerableList)
        {
            if (item == null)
            {
                continue;
            }

            object? current = item;

            for (int i = 0; i < parts.Length - 1; i++)
            {
                var prop = current?.GetType().GetProperty(parts[i]);

                if (prop == null)
                {
                    break;
                }

                var propValue = prop.GetValue(current);

                if (propValue is IEnumerable and not string)
                {
                    return true;
                }

                current = propValue;
            }
        }

        return false;
    }

    private static void ProcessPropertyForPaths(
        PropertyInfo property,
        string path,
        List<string> singleValue,
        List<string> multipleValue,
        HashSet<Type> visitedTypes)
    {
        if (TypeInspector.IsEnumerable(property.PropertyType, out var itemType))
        {
            ProcessCollectionForPaths(itemType, path, multipleValue, visitedTypes);
            return;
        }

        if (TypeInspector.IsDto(property.PropertyType))
        {
            ProcessDtoForPaths(property.PropertyType, path, singleValue, multipleValue, visitedTypes);
            return;
        }

        if (TypeInspector.IsSimple(property.PropertyType))
        {
            singleValue.Add(path);
        }
    }

    private static void ProcessCollectionForPaths(
        Type itemType,
        string prefix,
        List<string> multipleValue,
        HashSet<Type> visitedTypes)
    {
        var itemProperties = GetCachedProperties(itemType);

        foreach (var subProp in itemProperties)
        {
            var path = $"{prefix}.{subProp.Name}";
            ProcessCollectionProperty(subProp, path, multipleValue, visitedTypes);
        }
    }

    private static void ProcessCollectionProperty(
        PropertyInfo subProp,
        string path,
        List<string> multipleValue,
        HashSet<Type> visitedTypes)
    {
        if (TypeInspector.IsEnumerable(subProp.PropertyType, out var nestedItemType))
        {
            ProcessCollectionForPaths(nestedItemType, path, multipleValue, visitedTypes);
            return;
        }

        if (TypeInspector.IsSimple(subProp.PropertyType))
        {
            multipleValue.Add(path);
            return;
        }

        if (TypeInspector.IsDto(subProp.PropertyType))
        {
            var actualType = GetActualType(subProp.PropertyType);
            ProcessDtoPropertiesForMultipleValues(actualType, path, multipleValue, visitedTypes);
        }
    }

    private static Type GetActualType(Type propertyType)
    {
        return Nullable.GetUnderlyingType(propertyType) ?? propertyType;
    }

    private static void ProcessDtoForPaths(
        Type dtoType,
        string prefix,
        List<string> singleValue,
        List<string> multipleValue,
        HashSet<Type> visitedTypes)
    {
        if (visitedTypes.Contains(dtoType))
        {
            return;
        }

        visitedTypes.Add(dtoType);

        var nestedProperties = GetCachedProperties(dtoType);

        foreach (var subProp in nestedProperties)
        {
            var path = $"{prefix}.{subProp.Name}";
            ProcessPropertyForPaths(subProp, path, singleValue, multipleValue, visitedTypes);
        }

        visitedTypes.Remove(dtoType);
    }

    private static void ProcessDtoPropertiesForMultipleValues(
        Type dtoType,
        string path,
        List<string> multipleValue,
        HashSet<Type> visitedTypes)
    {
        if (visitedTypes.Contains(dtoType))
        {
            return;
        }

        var properties = GetCachedProperties(dtoType);
        foreach (var prop in properties)
        {
            var nestedPath = $"{path}.{prop.Name}";
            ProcessDtoPropertyForMultipleValues(prop, nestedPath, multipleValue, visitedTypes);
        }
    }

    private static void ProcessDtoPropertyForMultipleValues(
        PropertyInfo prop,
        string nestedPath,
        List<string> multipleValue,
        HashSet<Type> visitedTypes)
    {
        if (TypeInspector.IsEnumerable(prop.PropertyType, out var itemType))
        {
            ProcessCollectionForPaths(itemType, nestedPath, multipleValue, visitedTypes);
            return;
        }

        if (TypeInspector.IsSimple(prop.PropertyType))
        {
            multipleValue.Add(nestedPath);
            return;
        }

        if (TypeInspector.IsDto(prop.PropertyType))
        {
            var nestedActualType = GetActualType(prop.PropertyType);
            if (!visitedTypes.Contains(nestedActualType))
            {
                ProcessDtoPropertiesForMultipleValues(nestedActualType, nestedPath, multipleValue, visitedTypes);
            }
        }
    }

    private static void ProcessPropertyForDescriptions(
        PropertyInfo property,
        string path,
        Dictionary<string, string> pathDescriptions)
    {
        if (TypeInspector.IsEnumerable(property.PropertyType, out var itemType))
        {
            ProcessCollectionForDescriptions(itemType, path, pathDescriptions);
            return;
        }

        if (TypeInspector.IsDto(property.PropertyType))
        {
            ProcessDtoForDescriptions(property.PropertyType, path, pathDescriptions);
            return;
        }

        if (TypeInspector.IsSimple(property.PropertyType))
        {
            var description = GetPropertyDescription(property);
            pathDescriptions[path] = description;
        }
    }

    private static string GetPropertyDescription(PropertyInfo property)
    {
        return property.GetCustomAttribute<DescriptionAttribute>()?.Description ?? property.Name;
    }

    private static void ProcessCollectionForDescriptions(
        Type itemType,
        string prefix,
        Dictionary<string, string> pathDescriptions)
    {
        var itemProperties = GetCachedProperties(itemType);

        foreach (var subProp in itemProperties)
        {
            var path = $"{prefix}.{subProp.Name}";
            ProcessCollectionPropertyForDescriptions(subProp, path, pathDescriptions);
        }
    }

    private static void ProcessCollectionPropertyForDescriptions(
        PropertyInfo subProp,
        string path,
        Dictionary<string, string> pathDescriptions)
    {
        if (TypeInspector.IsEnumerable(subProp.PropertyType, out var nestedItemType))
        {
            ProcessCollectionForDescriptions(nestedItemType, path, pathDescriptions);
            return;
        }

        if (TypeInspector.IsSimple(subProp.PropertyType))
        {
            var description = GetPropertyDescription(subProp);
            pathDescriptions[path] = description;
            return;
        }

        if (TypeInspector.IsDto(subProp.PropertyType))
        {
            var actualType = GetActualType(subProp.PropertyType);
            ProcessDtoPropertiesForDescriptions(actualType, path, pathDescriptions);
        }
    }

    private static void ProcessDtoForDescriptions(
        Type dtoType,
        string prefix,
        Dictionary<string, string> pathDescriptions)
    {
        var properties = GetCachedProperties(dtoType);

        foreach (var prop in properties)
        {
            var path = $"{prefix}.{prop.Name}";
            ProcessPropertyForDescriptions(prop, path, pathDescriptions);
        }
    }

    private static void ProcessDtoPropertiesForDescriptions(
        Type dtoType,
        string path,
        Dictionary<string, string> pathDescriptions)
    {
        var properties = GetCachedProperties(dtoType);

        foreach (var prop in properties)
        {
            var nestedPath = $"{path}.{prop.Name}";
            ProcessDtoPropertyForDescriptions(prop, nestedPath, pathDescriptions);
        }
    }

    private static void ProcessDtoPropertyForDescriptions(
        PropertyInfo prop,
        string nestedPath,
        Dictionary<string, string> pathDescriptions)
    {
        if (TypeInspector.IsEnumerable(prop.PropertyType, out var itemType))
        {
            ProcessCollectionForDescriptions(itemType, nestedPath, pathDescriptions);
            return;
        }

        if (TypeInspector.IsSimple(prop.PropertyType))
        {
            var description = GetPropertyDescription(prop);
            pathDescriptions[nestedPath] = description;
            return;
        }

        if (TypeInspector.IsDto(prop.PropertyType))
        {
            var nestedActualType = GetActualType(prop.PropertyType);
            ProcessDtoPropertiesForDescriptions(nestedActualType, nestedPath, pathDescriptions);
        }
    }
}