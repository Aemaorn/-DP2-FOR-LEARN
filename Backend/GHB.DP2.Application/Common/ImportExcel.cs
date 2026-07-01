namespace GHB.DP2.Application.Common;

using System.Globalization;
using System.Reflection;
using System.Text.RegularExpressions;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;

[AttributeUsage(AttributeTargets.Property)]
public sealed class ExcelColumnAttribute : Attribute
{
    public string Header { get; init; }

    public int? Index { get; init; }

    public ExcelColumnAttribute()
    {
    }

    public ExcelColumnAttribute(string header) => this.Header = header;

    public ExcelColumnAttribute(int index) => this.Index = index;

    public ExcelColumnAttribute(string header, int index)
    {
        this.Header = header;
        this.Index = index;
    }
}

[AttributeUsage(AttributeTargets.Property)]
public sealed class ExcelDynamicColumnsAttribute : Attribute
{
    // e.g., ^\d{4}$ to map all year headers → dictionary keys
    public string MatchPattern { get; }

    public ExcelDynamicColumnsAttribute(string matchPattern) => this.MatchPattern = matchPattern;
}

public class ImportExcel<T>
    where T : new()
{
    public static List<T> FromStream(Stream stream, string sheetName, int headerRow = 1, int startDataRow = 2)
    {
        using var doc = SpreadsheetDocument.Open(stream, false);

        return Read(doc, sheetName, headerRow, startDataRow);
    }

    /// <summary>
    /// Converts Excel column reference (A1 notation) to zero-based column index.
    /// </summary>
    /// <param name="cellReference">The cell reference in A1 notation (e.g., "A1", "BC5")</param>
    /// <returns>Zero-based column index, or 0 if input is invalid</returns>
    private static int ColIndexFromA1(StringValue? cellReference)
    {
        if (cellReference?.Value is not { } referenceValue || string.IsNullOrWhiteSpace(referenceValue))
        {
            return 0;
        }

        // Extract column letters from the cell reference (e.g., "BC" from "BC5")
        var columnLetters = ExtractColumnLetters(referenceValue);

        if (string.IsNullOrEmpty(columnLetters))
        {
            return 0;
        }

        return ConvertColumnLettersToIndex(columnLetters);
    }

    /// <summary>
    /// Extracts column letters from a cell reference string.
    /// </summary>
    /// <param name="cellReference">Cell reference string (e.g., "A1", "BC5")</param>
    /// <returns>Column letters portion (e.g., "A", "BC")</returns>
    private static string ExtractColumnLetters(string cellReference)
    {
        var letterCount = 0;

        foreach (var character in cellReference)
        {
            if (!char.IsLetter(character))
            {
                break;
            }

            letterCount++;
        }

        return cellReference[..letterCount].ToUpperInvariant();
    }

    /// <summary>
    /// Converts column letters to zero-based column index using base-26 arithmetic.
    /// </summary>
    /// <param name="columnLetters">Column letters (e.g., "A", "BC")</param>
    /// <returns>Zero-based column index</returns>
    private static int ConvertColumnLettersToIndex(string columnLetters)
    {
        var columnIndex = 0;

        foreach (var letter in columnLetters)
        {
            columnIndex = (columnIndex * 26) + (letter - 'A' + 1);
        }

        return columnIndex - 1; // Convert to zero-based index
    }

    /// <summary>
    /// Converts text value to the specified type with proper type handling and nullable support.
    /// </summary>
    /// <param name="targetType">The target type to convert to</param>
    /// <param name="text">The text value to convert</param>
    /// <returns>Converted value or appropriate default/null value</returns>
    private static object? ConvertTo(Type targetType, string? text)
    {
        // Handle null/empty text early
        if (string.IsNullOrWhiteSpace(text))
        {
            return GetDefaultValue(targetType);
        }

        // Handle string type directly
        if (targetType == typeof(string))
        {
            return text;
        }

        // Handle nullable types
        var underlyingType = Nullable.GetUnderlyingType(targetType);

        if (underlyingType != null)
        {
            return ConvertTo(underlyingType, text);
        }

        var culture = CultureInfo.InvariantCulture;

        // Use direct type comparison for better performance and clarity
        if (targetType == typeof(int))
        {
            return TryParseInt(text, culture);
        }

        if (targetType == typeof(long))
        {
            return TryParseLong(text, culture);
        }

        if (targetType == typeof(decimal))
        {
            return TryParseDecimal(text, culture);
        }

        if (targetType == typeof(double))
        {
            return TryParseDouble(text, culture);
        }

        if (targetType == typeof(float))
        {
            return TryParseFloat(text, culture);
        }

        if (targetType == typeof(bool))
        {
            return TryParseBool(text);
        }

        if (targetType == typeof(DateTime))
        {
            return TryParseDateTime(text, culture);
        }

        // Fallback to Convert.ChangeType for other types
        return ConvertUsingChangeType(text, targetType, culture);
    }

    /// <summary>
    /// Gets the default value for a given type.
    /// </summary>
    /// <param name="type">The type to get default value for</param>
    /// <returns>Default value for the type</returns>
    private static object? GetDefaultValue(Type type)
    {
        if (type == typeof(string))
        {
            return string.Empty;
        }

        return type.IsValueType ? Activator.CreateInstance(type) : null;
    }

    /// <summary>
    /// Attempts to parse text as integer with fallback to zero.
    /// </summary>
    private static int TryParseInt(string text, CultureInfo culture)
    {
        return int.TryParse(text, NumberStyles.Any, culture, out var result) ? result : 0;
    }

    /// <summary>
    /// Attempts to parse text as long with fallback to zero.
    /// </summary>
    private static long TryParseLong(string text, CultureInfo culture)
    {
        return long.TryParse(text, NumberStyles.Any, culture, out var result) ? result : 0L;
    }

    /// <summary>
    /// Attempts to parse text as decimal with fallback to zero.
    /// </summary>
    private static decimal TryParseDecimal(string text, CultureInfo culture)
    {
        return decimal.TryParse(text, NumberStyles.Any, culture, out var result) ? result : 0m;
    }

    /// <summary>
    /// Attempts to parse text as double with fallback to zero.
    /// </summary>
    private static double TryParseDouble(string text, CultureInfo culture)
    {
        return double.TryParse(text, NumberStyles.Any, culture, out var result) ? result : 0d;
    }

    /// <summary>
    /// Attempts to parse text as float with fallback to zero.
    /// </summary>
    private static float TryParseFloat(string text, CultureInfo culture)
    {
        return float.TryParse(text, NumberStyles.Any, culture, out var result) ? result : 0f;
    }

    /// <summary>
    /// Attempts to parse text as boolean with support for multiple formats.
    /// </summary>
    private static bool TryParseBool(string text)
    {
        var normalizedText = text.Trim();

        return normalizedText switch
        {
            "1" or "true" or "TRUE" or "True" or "yes" or "YES" or "Yes" => true,
            "0" or "false" or "FALSE" or "False" or "no" or "NO" or "No" => false,
            _ => bool.TryParse(normalizedText, out var result) && result,
        };
    }

    /// <summary>
    /// Attempts to parse text as DateTime with support for Excel OADate and standard formats.
    /// </summary>
    private static DateTime TryParseDateTime(string text, CultureInfo culture)
    {
        // Try Excel OADate format first (numeric)
        if (double.TryParse(text, NumberStyles.Any, culture, out var oaDate))
        {
            try
            {
                return DateTime.FromOADate(oaDate);
            }
            catch (ArgumentException)
            {
                // Invalid OADate value, continue to try string parsing
            }
        }

        // Try standard DateTime parsing
        if (DateTime.TryParse(text, culture, DateTimeStyles.AssumeLocal, out var dateTime))
        {
            return dateTime;
        }

        // Return default DateTime if all parsings fail
        return default;
    }

    /// <summary>
    /// Uses Convert.ChangeType as a fallback for unsupported types.
    /// </summary>
    private static object? ConvertUsingChangeType(string text, Type targetType, CultureInfo culture)
    {
        try
        {
            return Convert.ChangeType(text, targetType, culture);
        }
        catch (Exception)
        {
            // Return default value if conversion fails
            return GetDefaultValue(targetType);
        }
    }

    private static string GetCellText(Cell? c, SharedStringTable? sst)
    {
        if (c == null)
        {
            return string.Empty;
        }

        if (c.DataType != null && c.DataType == CellValues.SharedString)
        {
            if (int.TryParse(c.CellValue?.InnerText, out int sIdx) && sst != null)
            {
                return sst.ElementAt(sIdx).InnerText;
            }

            return string.Empty;
        }

        if (c.InlineString != null)
        {
            return c.InlineString.Text?.Text ?? string.Empty;
        }

        return c.CellValue?.InnerText ?? string.Empty;
    }

    private static List<T> Read(SpreadsheetDocument doc, string sheetName, int headerRow, int startDataRow)
    {
        var excelData = ExtractExcelData(doc, sheetName);
        var headerMapping = BuildHeaderMapping(excelData.Rows, headerRow, excelData.SharedStringTable);
        var propertyMappings = BuildPropertyMappings(headerMapping.ColumnHeaders);

        return ProcessDataRows(excelData.Rows, excelData.SharedStringTable, headerMapping, propertyMappings, startDataRow);
    }

    /// <summary>
    /// Extracts basic Excel data components needed for processing.
    /// </summary>
    private static ExcelData ExtractExcelData(SpreadsheetDocument doc, string sheetName)
    {
        var workbookPart = doc.WorkbookPart
                           ?? throw new InvalidOperationException("Workbook part is missing from the Excel document.");

        var sheets = workbookPart.Workbook.Sheets
                     ?? throw new InvalidOperationException("No sheets found in the Excel workbook.");

        var sheet = FindSheetByName(sheets, sheetName);

        var worksheetPart = GetWorksheetPart(workbookPart, sheet);
        var sharedStringTable = workbookPart.SharedStringTablePart?.SharedStringTable;
        var rows = ExtractRowsFromWorksheet(worksheetPart);

        return new ExcelData(rows, sharedStringTable);
    }

    /// <summary>
    /// Finds a sheet by name with proper error handling.
    /// </summary>
    private static Sheet FindSheetByName(Sheets sheets, string sheetName)
    {
        var sheet = sheets.Elements<Sheet>()
                          .FirstOrDefault(s => sheetName.Contains(s.Name?.Value?.Trim() ?? string.Empty, StringComparison.OrdinalIgnoreCase));

        if (sheet == null)
        {
            var availableSheets = sheets.Elements<Sheet>()
                                        .Select(s => s.Name?.Value ?? "<unnamed>")
                                        .ToList();

            throw new InvalidOperationException(
                $"Sheet '{sheetName}' not found in the Excel workbook. Available sheets: {string.Join(", ", availableSheets)}");
        }

        return sheet;
    }

    /// <summary>
    /// Gets the worksheet part from the workbook with proper error handling.
    /// </summary>
    private static WorksheetPart GetWorksheetPart(WorkbookPart workbookPart, Sheet sheet)
    {
        if (sheet.Id?.Value == null)
        {
            throw new InvalidOperationException($"Sheet '{sheet.Name?.Value}' has no valid ID.");
        }

        var worksheetPart = workbookPart.GetPartById(sheet.Id.Value) as WorksheetPart;

        if (worksheetPart == null)
        {
            throw new InvalidOperationException($"Cannot find worksheet part for sheet '{sheet.Name?.Value}'.");
        }

        return worksheetPart;
    }

    /// <summary>
    /// Extracts rows from the worksheet with proper error handling.
    /// </summary>
    private static List<Row> ExtractRowsFromWorksheet(WorksheetPart worksheetPart)
    {
        var sheetData = worksheetPart.Worksheet.GetFirstChild<SheetData>();

        if (sheetData == null)
        {
            throw new InvalidOperationException("No sheet data found in the worksheet.");
        }

        return [.. sheetData.Elements<Row>()];
    }

    /// <summary>
    /// Builds mapping between column indices and header names.
    /// </summary>
    private static HeaderMapping BuildHeaderMapping(List<Row> rows, int headerRow, SharedStringTable? sharedStringTable)
    {
        var headerRowData = rows.FirstOrDefault(r => r.RowIndex?.Value == (uint)headerRow)
                            ?? rows[headerRow - 1];

        var columnHeaders = new Dictionary<int, string>();
        var maxColumn = 0;

        foreach (var cell in headerRowData.Elements<Cell>())
        {
            var columnIndex = ColIndexFromA1(cell.CellReference);
            maxColumn = Math.Max(maxColumn, columnIndex);
            columnHeaders[columnIndex] = GetCellText(cell, sharedStringTable);
        }

        return new HeaderMapping(columnHeaders, maxColumn);
    }

    /// <summary>
    /// Attempts to create a dynamic property mapping for dictionary properties.
    /// </summary>
    private static bool TryCreateDynamicMapping(PropertyInfo property, out DynamicPropertyMapping? mapping)
    {
        mapping = null;

        var dynamicAttribute = property.GetCustomAttribute<ExcelDynamicColumnsAttribute>();

        if (dynamicAttribute == null)
        {
            return false;
        }

        var dictionaryInterface = property.PropertyType.GetInterfaces()
                                          .FirstOrDefault(t => t.IsGenericType && t.GetGenericTypeDefinition() == typeof(IDictionary<,>));

        if (dictionaryInterface == null)
        {
            return false;
        }

        var genericArguments = dictionaryInterface.GetGenericArguments();
        mapping = new DynamicPropertyMapping(
            property,
            new Regex(dynamicAttribute.MatchPattern, RegexOptions.None, TimeSpan.FromMilliseconds(500)),
            genericArguments[0],
            genericArguments[1]);

        return true;
    }

    /// <summary>
    /// Attempts to create a direct property mapping for regular properties.
    /// </summary>
    private static bool TryCreateDirectMapping(PropertyInfo property, Dictionary<int, string> columnHeaders, out DirectPropertyMapping? mapping)
    {
        mapping = null;

        var columnAttribute = property.GetCustomAttribute<ExcelColumnAttribute>();

        if (columnAttribute == null)
        {
            return false;
        }

        // Try mapping by index first (convert 1-based to 0-based index)
        if (columnAttribute.Index is { } index and > 0)
        {
            var zeroBasedIndex = index - 1; // Convert 1-based to 0-based
            mapping = new DirectPropertyMapping(property, zeroBasedIndex);

            return true;
        }

        // Try mapping by header name
        if (!string.IsNullOrWhiteSpace(columnAttribute.Header))
        {
            var foundColumn = columnHeaders.FirstOrDefault(kv =>
                string.Equals(kv.Value, columnAttribute.Header, StringComparison.OrdinalIgnoreCase));

            // Changed from > 0 to >= 0 since column A is index 0
            if (foundColumn.Key >= 0)
            {
                mapping = new DirectPropertyMapping(property, foundColumn.Key);

                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Builds property mappings for both direct and dynamic column mappings.
    /// </summary>
    private static PropertyMappings BuildPropertyMappings(Dictionary<int, string> columnHeaders)
    {
        var properties = typeof(T).GetProperties(BindingFlags.Instance | BindingFlags.Public)
                                  .Where(p => p.CanWrite)
                                  .ToArray();

        var directMappings = new List<DirectPropertyMapping>();
        var dynamicMappings = new List<DynamicPropertyMapping>();

        foreach (var property in properties)
        {
            if (TryCreateDynamicMapping(property, out var dynamicMapping) && dynamicMapping != null)
            {
                dynamicMappings.Add(dynamicMapping);

                continue;
            }

            if (TryCreateDirectMapping(property, columnHeaders, out var directMapping) && directMapping != null)
            {
                directMappings.Add(directMapping);
            }
        }

        return new PropertyMappings(directMappings, dynamicMappings);
    }

    /// <summary>
    /// Processes all data rows and converts them to strongly-typed objects.
    /// </summary>
    private static List<T> ProcessDataRows(
        List<Row> rows,
        SharedStringTable? sharedStringTable,
        HeaderMapping headerMapping,
        PropertyMappings propertyMappings,
        int startDataRow)
    {
        var result = new List<T>();
        var maxRowIndex = rows.Max(x => (int)x.RowIndex!.Value);

        for (var rowIndex = startDataRow; rowIndex <= maxRowIndex; rowIndex++)
        {
            var row = rows.FirstOrDefault(x => x.RowIndex?.Value == (uint)rowIndex);
            var item = new T();
            var hasData = false;

            // Process direct property mappings
            hasData |= ProcessDirectMappings(row, sharedStringTable, item, propertyMappings.DirectMappings);

            // Process dynamic dictionary mappings
            hasData |= ProcessDynamicMappings(row, sharedStringTable, item, propertyMappings.DynamicMappings, headerMapping);

            if (hasData)
            {
                result.Add(item);
            }
        }

        return result;
    }

    /// <summary>
    /// Processes direct property mappings for a single row.
    /// </summary>
    private static bool ProcessDirectMappings(
        Row? row,
        SharedStringTable? sharedStringTable,
        T item,
        List<DirectPropertyMapping> directMappings)
    {
        var hasData = false;

        foreach (var mapping in directMappings)
        {
            var cell = row?.Elements<Cell>()
                          .FirstOrDefault(c => ColIndexFromA1(c.CellReference) == mapping.ColumnIndex);
            var cellText = GetCellText(cell, sharedStringTable);

            if (!string.IsNullOrWhiteSpace(cellText))
            {
                hasData = true;
            }

            var convertedValue = ConvertTo(mapping.Property.PropertyType, cellText);
            mapping.Property.SetValue(item, convertedValue);
        }

        return hasData;
    }

    /// <summary>
    /// Processes dynamic dictionary mappings for a single row.
    /// </summary>
    private static bool ProcessDynamicMappings(
        Row? row,
        SharedStringTable? sharedStringTable,
        T item,
        List<DynamicPropertyMapping> dynamicMappings,
        HeaderMapping headerMapping)
    {
        var hasData = false;

        foreach (var mapping in dynamicMappings)
        {
            var dictionary = GetOrCreateDictionary(item, mapping);

            for (var columnIndex = 1; columnIndex <= headerMapping.MaxColumn; columnIndex++)
            {
                if (!ShouldProcessColumn(headerMapping.ColumnHeaders, columnIndex, mapping.Regex, out var headerValue))
                {
                    continue;
                }

                var cell = row?.Elements<Cell>()
                              .FirstOrDefault(c => ColIndexFromA1(c.CellReference) == columnIndex);
                var cellText = GetCellText(cell, sharedStringTable);

                if (!string.IsNullOrWhiteSpace(cellText))
                {
                    hasData = true;
                }

                var key = CreateDictionaryKey(headerValue, mapping.KeyType);
                var value = ConvertTo(mapping.ValueType, cellText);
                dictionary[key] = value!;
            }
        }

        return hasData;
    }

    /// <summary>
    /// Gets or creates a dictionary instance for dynamic mapping.
    /// </summary>
    private static System.Collections.IDictionary GetOrCreateDictionary(T item, DynamicPropertyMapping mapping)
    {
        var dictionary = (System.Collections.IDictionary?)mapping.Property.GetValue(item);

        if (dictionary == null)
        {
            var dictionaryType = typeof(Dictionary<,>).MakeGenericType(mapping.KeyType, mapping.ValueType);
            dictionary = (System.Collections.IDictionary?)Activator.CreateInstance(dictionaryType);
            mapping.Property.SetValue(item, dictionary);
        }

        return dictionary!;
    }

    /// <summary>
    /// Determines if a column should be processed based on regex matching.
    /// </summary>
    private static bool ShouldProcessColumn(
        Dictionary<int, string> columnHeaders,
        int columnIndex,
        Regex regex,
        out string headerValue)
    {
        if (!columnHeaders.TryGetValue(columnIndex, out var tempHeaderValue))
        {
            headerValue = string.Empty;

            return false;
        }

        headerValue = tempHeaderValue;

        return regex.IsMatch(headerValue);
    }

    /// <summary>
    /// Creates a dictionary key of the appropriate type.
    /// </summary>
    private static object CreateDictionaryKey(string headerValue, Type keyType)
    {
        return keyType == typeof(int)
            ? int.Parse(headerValue, CultureInfo.InvariantCulture)
            : headerValue;
    }

    // Helper record types for better organization
    private record ExcelData(List<Row> Rows, SharedStringTable? SharedStringTable);

    private record HeaderMapping(Dictionary<int, string> ColumnHeaders, int MaxColumn);

    private record PropertyMappings(
        List<DirectPropertyMapping> DirectMappings,
        List<DynamicPropertyMapping> DynamicMappings);

    private record DirectPropertyMapping(PropertyInfo Property, int ColumnIndex);

    private record DynamicPropertyMapping(
        PropertyInfo Property,
        Regex Regex,
        Type KeyType,
        Type ValueType);
}