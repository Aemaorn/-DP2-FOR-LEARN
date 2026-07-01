namespace GHB.DP2.Application.Extensions.Document.ReplaceDocument;

using Codehard.OdtKit;
using Codehard.OdtKit.Abstractions;
using Codehard.OdtKit.Representations.Tables;
using Codehard.OdtKit.Representations.Texts;

/// <summary>
/// Handles processing of document templates with placeholder replacement
/// </summary>
public static class DocumentTemplateEngine
{
    /// <summary>
    /// Processes single value placeholders in the document
    /// </summary>
    /// <param name="wrapped">The wrapped document element</param>
    /// <param name="complexDto">The source data object</param>
    /// <param name="singleValuePaths">List of property paths for single values</param>
    public static void ProcessSingleValuePlaceholders(XElementWrapper wrapped, object complexDto, List<string> singleValuePaths)
    {
        if (singleValuePaths.Count == 0)
        {
            return;
        }

        var singleValuePathsSet = singleValuePaths.ToHashSet();
        var placeholdersByKey = wrapped.OfType<PlaceholderElement>()
            .Where(p => singleValuePathsSet.Contains(p.Key))
            .GroupBy(p => p.Key)
            .ToDictionary(g => g.Key, g => g.ToList());

        foreach (var propertyPath in singleValuePaths)
        {
            if (!placeholdersByKey.TryGetValue(propertyPath, out var placeholders))
            {
                continue;
            }

            var value = PropertyPathMapper.GetSingleValueByPath(complexDto, propertyPath);
            if (value != null)
            {
                placeholders.UpdatePlaceHolder(propertyPath, value.ToString()!);
            }
        }
    }

    /// <summary>
    /// Processes multiple value placeholders in the document (tables and lists)
    /// </summary>
    /// <param name="wrapped">The wrapped document element</param>
    /// <param name="complexDto">The source data object</param>
    /// <param name="multipleValuePaths">List of property paths for multiple values</param>
    public static void ProcessMultipleValuePlaceholders(
        XElementWrapper wrapped,
        object complexDto,
        List<string> multipleValuePaths)
    {
        if (multipleValuePaths.Count == 0)
        {
            return;
        }

        var multipleValuePathsSet = multipleValuePaths.ToHashSet();

        ProcessListsWithMultipleValues(wrapped, complexDto, multipleValuePathsSet);
        ProcessTablesWithMultipleValues(wrapped, complexDto, multipleValuePathsSet);
    }

    private static void ProcessTablesWithMultipleValues(
        XElementWrapper wrapped,
        object complexDto,
        HashSet<string> multipleValuePaths)
    {
        foreach (var table in wrapped.OfType<TableElement>())
        {
            ProcessTableWithMultipleValues(table, complexDto, multipleValuePaths);
        }
    }

    private static void ProcessTableWithMultipleValues(TableElement table, object complexDto, HashSet<string> multipleValuePaths)
    {
        var templateRows = table.Rows
            .Where(row => row.OfType<PlaceholderElement>()
                .Any(p => multipleValuePaths.Contains(p.Key)))
            .ToList();

        foreach (var templateRow in templateRows)
        {
            ProcessTemplateRow(templateRow, complexDto, multipleValuePaths);
        }
    }

    private static void ProcessTemplateRow(TableRowElement templateRow, object complexDto, HashSet<string> multipleValuePaths)
    {
        var multipleValuePlaceholders = templateRow.OfType<PlaceholderElement>()
            .Where(p => multipleValuePaths.Contains(p.Key))
            .ToArray();

        if (multipleValuePlaceholders.Length == 0)
        {
            return;
        }

        var firstPlaceholder = multipleValuePlaceholders[0];
        var valueCount = PropertyPathMapper.GetMultipleValuesCount(complexDto, firstPlaceholder.Key);

        if (valueCount > 0)
        {
            CreateRowsFromTemplate(templateRow, complexDto, multipleValuePaths, valueCount);
            templateRow.Remove();
        }
    }

    private static void CreateRowsFromTemplate(
        TableRowElement templateRow,
        object complexDto,
        HashSet<string> multipleValuePaths,
        int rowCount)
    {
        for (var rowIndex = 0; rowIndex < rowCount; rowIndex++)
        {
            var clonedRow = templateRow.Clone();
            var placeholders = clonedRow.OfType<PlaceholderElement>()
                .Where(p => multipleValuePaths.Contains(p.Key));

            foreach (var placeholder in placeholders)
            {
                var value = PropertyPathMapper.GetMultipleValueByPathWithIndex(complexDto, placeholder.Key, rowIndex);
                if (value != null)
                {
                    var replacedValue = value.ToString() ?? string.Empty;
                    var textElement = PredefinedTextElement.Create(replacedValue);
                    placeholder.ReplaceWith(textElement);
                }
            }

            templateRow.AddBeforeSelf(clonedRow);
        }
    }

    private static void ProcessListsWithMultipleValues(
        XElementWrapper wrapped,
        object complexDto,
        HashSet<string> multipleValuePaths)
    {
        foreach (var list in wrapped.OfType<ListElement>())
        {
            ProcessListWithMultipleValues(list, complexDto, multipleValuePaths);
        }
    }

    private static void ProcessListWithMultipleValues(ListElement list, object complexDto, HashSet<string> multipleValuePaths)
    {
        var templateItems = list.OfType<ListItemElement>()
            .Where(item => item.Descendants<PlaceholderElement>()
                .Any(p => multipleValuePaths.Contains(p.Key)))
            .ToList();

        foreach (var templateItem in templateItems)
        {
            ProcessTemplateListItem(templateItem, complexDto, multipleValuePaths);
        }
    }

    private static void ProcessTemplateListItem(ListItemElement templateItem, object complexDto, HashSet<string> multipleValuePaths)
    {
        var multipleValuePlaceholders = templateItem.OfType<PlaceholderElement>()
            .Where(p => multipleValuePaths.Contains(p.Key))
            .ToArray();

        if (multipleValuePlaceholders.Length == 0)
        {
            return;
        }

        var firstPlaceholder = multipleValuePlaceholders[0];
        var valueCount = PropertyPathMapper.GetMultipleValuesCount(complexDto, firstPlaceholder.Key);

        if (valueCount > 0)
        {
            CreateListItemsFromTemplate(templateItem, complexDto, multipleValuePaths, valueCount);
            templateItem.Remove();
        }
    }

    private static void CreateListItemsFromTemplate(
        ListItemElement templateItem,
        object complexDto,
        HashSet<string> multipleValuePaths,
        int itemCount)
    {
        for (var itemIndex = 0; itemIndex < itemCount; itemIndex++)
        {
            var clonedXElement = new System.Xml.Linq.XElement(templateItem.Raw);
            var clonedItem = clonedXElement.Wrap();

            if (clonedItem is ListItemElement listItem)
            {
                var placeholders = listItem.Descendants<PlaceholderElement>()
                    .Where(p => multipleValuePaths.Contains(p.Key));

                foreach (var placeholder in placeholders)
                {
                    var value = PropertyPathMapper.GetMultipleValueByPathWithIndex(complexDto, placeholder.Key, itemIndex);
                    if (value != null)
                    {
                        var replacedValue = value.ToString() ?? string.Empty;
                        var textElement = PredefinedTextElement.Create(replacedValue);
                        placeholder.ReplaceWith(textElement);
                    }
                }

                templateItem.AddBeforeSelf(listItem.Raw);
            }
        }
    }

    /// <summary>
    /// Extension method to update placeholders with a value
    /// </summary>
    /// <param name="placeholders">Collection of placeholder elements</param>
    /// <param name="key">The placeholder key</param>
    /// <param name="value">The replacement value</param>
    private static void UpdatePlaceHolder(
        this IEnumerable<PlaceholderElement> placeholders,
        string key,
        string value)
    {
        foreach (var placeholder in placeholders)
        {
            if (value.Contains('\n'))
            {
                var textElement = PredefinedTextElement.Create(value);
                placeholder.ReplaceWith(textElement);
            }
            else
            {
                placeholder.FillPlaceholders(key, value);
            }
        }
    }
}