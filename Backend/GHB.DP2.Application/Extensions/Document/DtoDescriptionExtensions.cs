namespace GHB.DP2.Application.Extensions.Document;

using System.ComponentModel;
using System.Reflection;
using GHB.DP2.Application.Extensions.Document.ReplaceDocument;

public record ResponseDtoDescriptionSingle(string Key, string Description);

public record ResponseDtoDescriptionMultiple(string Key, string Description);

public record ResponseDtoDescriptionExtension(
    IEnumerable<ResponseDtoDescriptionSingle> Single,
    IEnumerable<ResponseDtoDescriptionMultiple> Multiple);

public static class DtoDescriptionExtensions
{
    public static ResponseDtoDescriptionExtension ToDictionary(Type dtoType)
    {
        var singleDescriptions = new List<ResponseDtoDescriptionSingle>();
        var multipleDescriptions = new List<ResponseDtoDescriptionMultiple>();
        var visitedTypes = new HashSet<Type>();
        var rootProperties = dtoType.GetProperties(BindingFlags.Public | BindingFlags.Instance);

        foreach (var prop in rootProperties)
        {
            ProcessProperty(prop, prop.Name, singleDescriptions, multipleDescriptions, visitedTypes);
        }

        return new ResponseDtoDescriptionExtension(singleDescriptions, multipleDescriptions);
    }

    private static void ProcessProperty(
        PropertyInfo property,
        string path,
        List<ResponseDtoDescriptionSingle> singleDescriptions,
        List<ResponseDtoDescriptionMultiple> multipleDescriptions,
        HashSet<Type> visitedTypes)
    {
        if (TypeInspector.IsEnumerable(property.PropertyType, out var itemType))
        {
            ProcessCollectionProperty(itemType, path, multipleDescriptions, visitedTypes);
        }
        else if (TypeInspector.IsDto(property.PropertyType))
        {
            ProcessDtoProperty(property.PropertyType, path, singleDescriptions, multipleDescriptions, visitedTypes);
        }
        else
        {
            ProcessScalarProperty(property, path, singleDescriptions);
        }
    }

    private static void ProcessCollectionProperty(
        Type itemType,
        string prefix,
        List<ResponseDtoDescriptionMultiple> multipleDescriptions,
        HashSet<Type> visitedTypes)
    {
        foreach (var subProp in itemType.GetProperties())
        {
            var path = $"{prefix}.{subProp.Name}";
            var description = GetDescription(subProp);

            if (TypeInspector.IsEnumerable(subProp.PropertyType, out var nestedItemType))
            {
                ProcessNestedCollection(nestedItemType, path, multipleDescriptions, visitedTypes);
            }
            else if (TypeInspector.IsDto(subProp.PropertyType))
            {
                ProcessCollectionItemDto(subProp, path, multipleDescriptions, itemType, visitedTypes);
            }
            else
            {
                multipleDescriptions.Add(new ResponseDtoDescriptionMultiple(path, description));
            }
        }
    }

    private static void ProcessCollectionItemDto(
        PropertyInfo property,
        string path,
        List<ResponseDtoDescriptionMultiple> multipleDescriptions,
        Type parentItemType,
        HashSet<Type> visitedTypes)
    {
        var actualType = Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;

        if (actualType == parentItemType)
        {
            ProcessSelfReferencingType(actualType, path, multipleDescriptions, visitedTypes);
        }
        else
        {
            ProcessNestedDtoProperties(actualType, path, multipleDescriptions, visitedTypes);
        }
    }

    private static void ProcessSelfReferencingType(
        Type actualType,
        string path,
        List<ResponseDtoDescriptionMultiple> multipleDescriptions,
        HashSet<Type> visitedTypes)
    {
        if (visitedTypes.Contains(actualType))
        {
            return;
        }

        foreach (var nestedProp in actualType.GetProperties())
        {
            var nestedPath = $"{path}.{nestedProp.Name}";
            var nestedDescription = GetDescription(nestedProp);
            var nestedActualType = Nullable.GetUnderlyingType(nestedProp.PropertyType) ?? nestedProp.PropertyType;

            if (nestedActualType != actualType && !visitedTypes.Contains(nestedActualType))
            {
                if (TypeInspector.IsDto(nestedActualType))
                {
                    ProcessNestedDtoProperties(nestedActualType, nestedPath, multipleDescriptions, visitedTypes);
                }
                else
                {
                    multipleDescriptions.Add(new ResponseDtoDescriptionMultiple(nestedPath, nestedDescription));
                }
            }
        }
    }

    private static void ProcessDtoProperty(
        Type dtoType,
        string prefix,
        List<ResponseDtoDescriptionSingle> singleDescriptions,
        List<ResponseDtoDescriptionMultiple> multipleDescriptions,
        HashSet<Type> visitedTypes)
    {
        if (visitedTypes.Contains(dtoType))
        {
            return;
        }

        visitedTypes.Add(dtoType);

        foreach (var subProp in dtoType.GetProperties())
        {
            var path = $"{prefix}.{subProp.Name}";
            var description = GetDescription(subProp);

            if (TypeInspector.IsEnumerable(subProp.PropertyType, out var nestedItemType))
            {
                ProcessNestedCollection(nestedItemType, path, multipleDescriptions, visitedTypes);
            }
            else if (TypeInspector.IsDto(subProp.PropertyType))
            {
                ProcessDtoProperty(subProp.PropertyType, path, singleDescriptions, multipleDescriptions, visitedTypes);
            }
            else
            {
                singleDescriptions.Add(new ResponseDtoDescriptionSingle(path, description));
            }
        }

        visitedTypes.Remove(dtoType);
    }

    private static void ProcessNestedCollection(
        Type itemType,
        string path,
        List<ResponseDtoDescriptionMultiple> multipleDescriptions,
        HashSet<Type> visitedTypes)
    {
        foreach (var nestedSubProp in itemType.GetProperties())
        {
            var nestedPath = $"{path}.{nestedSubProp.Name}";
            var nestedDescription = GetDescription(nestedSubProp);

            if (TypeInspector.IsEnumerable(nestedSubProp.PropertyType, out var deeperItemType))
            {
                ProcessNestedCollection(deeperItemType, nestedPath, multipleDescriptions, visitedTypes);
            }
            else if (TypeInspector.IsDto(nestedSubProp.PropertyType))
            {
                var nestedActualType = Nullable.GetUnderlyingType(nestedSubProp.PropertyType) ?? nestedSubProp.PropertyType;
                ProcessNestedDtoProperties(nestedActualType, nestedPath, multipleDescriptions, visitedTypes);
            }
            else
            {
                multipleDescriptions.Add(new ResponseDtoDescriptionMultiple(nestedPath, nestedDescription));
            }
        }
    }

    private static void ProcessNestedDtoProperties(
        Type dtoType,
        string path,
        List<ResponseDtoDescriptionMultiple> multipleDescriptions,
        HashSet<Type> visitedTypes)
    {
        if (visitedTypes.Contains(dtoType))
        {
            return;
        }

        foreach (var deepNestedProp in dtoType.GetProperties())
        {
            var deepNestedPath = $"{path}.{deepNestedProp.Name}";
            var deepNestedDescription = GetDescription(deepNestedProp);

            if (TypeInspector.IsDto(deepNestedProp.PropertyType))
            {
                var actualType = Nullable.GetUnderlyingType(deepNestedProp.PropertyType) ?? deepNestedProp.PropertyType;

                if (!visitedTypes.Contains(actualType))
                {
                    ProcessNestedDtoProperties(actualType, deepNestedPath, multipleDescriptions, visitedTypes);
                }
            }
            else
            {
                multipleDescriptions.Add(new ResponseDtoDescriptionMultiple(deepNestedPath, deepNestedDescription));
            }
        }
    }

    private static void ProcessScalarProperty(
        PropertyInfo property,
        string path,
        List<ResponseDtoDescriptionSingle> singleDescriptions)
    {
        var description = GetDescription(property);
        singleDescriptions.Add(new ResponseDtoDescriptionSingle(path, description));
    }

    private static string GetDescription(PropertyInfo property)
    {
        return property.GetCustomAttribute<DescriptionAttribute>()?.Description ?? property.Name;
    }
}