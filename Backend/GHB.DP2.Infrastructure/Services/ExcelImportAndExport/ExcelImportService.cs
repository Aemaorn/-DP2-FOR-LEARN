namespace GHB.DP2.Infrastructure.Services.ExcelImportAndExport;

using System.Globalization;
using System.Reflection;
using ExcelDataReader;
using FastEndpoints;
using GHB.DP2.Infrastructure.Exceptions;
using Microsoft.Extensions.Logging;

/// <summary>
/// Service for importing Excel files and mapping data to strongly typed C# objects.
/// </summary>
[RegisterService<IExcelImportService>(LifeTime.Scoped)]
public class ExcelImportService : IExcelImportService
{
    private readonly ILogger<ExcelImportService> logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ExcelImportService"/> class.
    /// </summary>
    /// <param name="logger">The logger instance for structured logging.</param>
    public ExcelImportService(ILogger<ExcelImportService> logger)
    {
        this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task<List<T>> ImportExcelAsync<T>(Stream fileStream, bool skipHeader = true, int worksheetIndex = 0, CancellationToken cancellationToken = default)
        where T : class, new()
    {
        var options = new ExcelImportOptions
        {
            SkipHeader = skipHeader,
            WorksheetIndex = worksheetIndex,
        };

        return await this.ImportExcelAsync<T>(fileStream, options, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<List<T>> ImportExcelAsync<T>(Stream fileStream, ExcelImportOptions options, CancellationToken cancellationToken = default)
        where T : class, new()
    {
        ArgumentNullException.ThrowIfNull(fileStream);

        ArgumentNullException.ThrowIfNull(options);

        try
        {
            // Validate file size
            if (fileStream.CanSeek && fileStream.Length > options.MaxFileSizeBytes)
            {
                throw new InvalidFileFormatException($"File size ({fileStream.Length} bytes) exceeds maximum allowed size ({options.MaxFileSizeBytes} bytes).");
            }

            // Reset stream position if possible
            if (fileStream.CanSeek)
            {
                fileStream.Position = 0;
            }

            var result = await Task.Run(() => this.ProcessExcelFile<T>(fileStream, options, cancellationToken), cancellationToken);

            return result;
        }
        catch (Exception ex) when (ex is not (InvalidFileFormatException or EmptyFileException or WorksheetNotFoundException or DataMappingException))
        {
            throw new InvalidFileFormatException("An error occurred while processing the Excel file. Please ensure the file is not corrupted and is a valid Excel format.", ex);
        }
    }

    private List<T> ProcessExcelFile<T>(Stream fileStream, ExcelImportOptions options, CancellationToken cancellationToken)
        where T : class, new()
    {
        using var reader = ExcelReaderFactory.CreateReader(fileStream);

        if (reader == null)
        {
            throw new InvalidFileFormatException("Unable to create Excel reader. The file may not be a valid Excel format.");
        }

        // Navigate to the specified worksheet
        var currentWorksheetIndex = 0;

        do
        {
            if (currentWorksheetIndex == options.WorksheetIndex)
            {
                return this.ProcessWorksheet<T>(reader, options, cancellationToken);
            }

            currentWorksheetIndex++;
        }
        while (reader.NextResult());

        throw new WorksheetNotFoundException($"Worksheet at index {options.WorksheetIndex} was not found. The file contains {currentWorksheetIndex} worksheet(s).");
    }

    private List<T> ProcessWorksheet<T>(IExcelDataReader reader, ExcelImportOptions options, CancellationToken cancellationToken)
        where T : class, new()
    {
        var results = new List<T>();
        var propertyMap = BuildPropertyMap<T>(options);
        var headers = new Dictionary<string, int>();
        var rowNumber = 0;

        // Read the header row if needed
        if (options.SkipHeader && reader.Read())
        {
            rowNumber++;
            headers = ReadHeaderRow(reader);
            this.logger.LogDebug(
                "Read header row with {HeaderCount} columns: {Headers}",
                headers.Count,
                string.Join(", ", headers.Keys));
        }

        // Process data rows
        var batch = new List<T>();

        while (reader.Read())
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                var item = MapRowToObject<T>(reader, propertyMap, headers, rowNumber, options);

                if (item != null)
                {
                    batch.Add(item);

                    // Process in batches for memory efficiency
                    if (batch.Count >= options.BatchSize)
                    {
                        results.AddRange(batch);
                        batch.Clear();
                    }
                }
            }
            catch (DataMappingException ex)
            {
                if (!options.ContinueOnMappingError)
                {
                    throw;
                }

                this.logger.LogWarning(ex, "Skipping row {RowNumber} due to mapping error.", rowNumber);
            }

            rowNumber++;
        }

        // Add remaining items from the last batch
        if (batch.Count > 0)
        {
            results.AddRange(batch);
        }

        if (results.Count == 0)
        {
            throw new EmptyFileException("The Excel file contains no valid data rows.");
        }

        return results;
    }

    private static Dictionary<PropertyInfo, ColumnMapping> BuildPropertyMap<T>(ExcelImportOptions options)
        where T : class, new()
    {
        var propertyMap = new Dictionary<PropertyInfo, ColumnMapping>();
        var properties = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance)
                                  .Where(p => p.CanWrite);

        foreach (var property in properties)
        {
            var excelColumnAttr = property.GetCustomAttribute<ExcelColumnAttribute>();

            if (excelColumnAttr?.Ignore == true)
            {
                continue;
            }

            var mapping = new ColumnMapping
            {
                Property = property,
                Attribute = excelColumnAttr,
            };

            // Determine a column mapping source
            if (excelColumnAttr != null)
            {
                if (excelColumnAttr.UsesColumnIndex)
                {
                    mapping.ColumnIndex = excelColumnAttr.ColumnIndex;
                }
                else if (excelColumnAttr.UsesColumnName)
                {
                    mapping.ColumnName = excelColumnAttr.ColumnName;
                }
            }
            else
            {
                // Use property name as column name by default
                mapping.ColumnName = property.Name;
            }

            // Check for custom column mappings in options
            if (options.ColumnMappings.TryGetValue(property.Name, out var customColumnName))
            {
                mapping.ColumnName = customColumnName;
                mapping.ColumnIndex = null;
            }

            propertyMap[property] = mapping;
        }

        return propertyMap;
    }

    private static Dictionary<string, int> ReadHeaderRow(IExcelDataReader reader)
    {
        var headers = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        for (var columnIndex = 0; columnIndex < reader.FieldCount; columnIndex++)
        {
            var headerValue = reader.GetValue(columnIndex).ToString()?.Trim();

            if (!string.IsNullOrEmpty(headerValue))
            {
                headers[headerValue] = columnIndex;
            }
        }

        return headers;
    }

    private static T? MapRowToObject<T>(
        IExcelDataReader reader,
        Dictionary<PropertyInfo, ColumnMapping> propertyMap,
        Dictionary<string, int> headers,
        int rowNumber,
        ExcelImportOptions options)
        where T : class, new()
    {
        var item = new T();
        var hasAnyValue = false;

        foreach (var (property, mapping) in propertyMap)
        {
            try
            {
                var cellValue = GetCellValue(reader, mapping, headers, rowNumber);
                var convertedValue = ConvertValue(cellValue, property.PropertyType, mapping, rowNumber, options);

                if (convertedValue == null && IsValueTypeOrNullable(property.PropertyType))
                {
                    continue;
                }

                property.SetValue(item, convertedValue);

                if (convertedValue != null)
                {
                    hasAnyValue = true;
                }
            }
            catch (Exception ex)
            {
                var columnInfo = mapping.ColumnName ?? mapping.ColumnIndex?.ToString() ?? property.Name;

                throw new DataMappingException(
                    $"Failed to map column '{columnInfo}' to property '{property.Name}' at row {rowNumber}. {ex.Message}", ex)
                {
                    PropertyName = property.Name,
                    ColumnName = columnInfo,
                    RowNumber = rowNumber,
                    CellValue = GetCellValue(reader, mapping, headers, rowNumber),
                    TargetType = property.PropertyType,
                };
            }
        }

        return hasAnyValue ? item : null;
    }

    private static object? GetCellValue(IExcelDataReader reader, ColumnMapping mapping, Dictionary<string, int> headers, int rowNumber)
    {
        int columnIndex;

        if (mapping.ColumnIndex.HasValue)
        {
            columnIndex = mapping.ColumnIndex.Value;
        }
        else if (!string.IsNullOrEmpty(mapping.ColumnName) && headers.TryGetValue(mapping.ColumnName, out var headerIndex))
        {
            columnIndex = headerIndex;
        }
        else
        {
            if (mapping.Attribute?.IsRequired == true)
            {
                throw new DataMappingException($"Required column '{mapping.ColumnName ?? mapping.Property.Name}' not found at row {rowNumber}.");
            }

            return mapping.Attribute?.DefaultValue;
        }

        if (columnIndex < reader.FieldCount)
        {
            return reader.GetValue(columnIndex);
        }

        if (mapping.Attribute?.IsRequired == true)
        {
            throw new DataMappingException($"Required column at index {columnIndex} is beyond the available columns ({reader.FieldCount}) at row {rowNumber}.");
        }

        return mapping.Attribute?.DefaultValue;
    }

    private static object? ConvertValue(object? cellValue, Type targetType, ColumnMapping mapping, int rowNumber, ExcelImportOptions options)
    {
        if (cellValue == null || cellValue == DBNull.Value || (cellValue is string str && string.IsNullOrWhiteSpace(str)))
        {
            if (mapping.Attribute?.IsRequired == true)
            {
                throw new DataMappingException($"Required value is missing at row {rowNumber}.");
            }

            return mapping.Attribute?.DefaultValue ?? GetDefaultValue(targetType);
        }

        // Handle nullable types
        var underlyingType = Nullable.GetUnderlyingType(targetType) ?? targetType;

        // Convert string values
        if (cellValue is string stringValue)
        {
            if (options.TrimStrings)
            {
                stringValue = stringValue.Trim();
            }

            return string.IsNullOrEmpty(stringValue)
                ? GetDefaultValue(targetType)
                : ConvertFromString(stringValue, underlyingType, mapping, options);
        }

        // Direct type conversion
        try
        {
            if (underlyingType == typeof(string))
            {
                return cellValue.ToString();
            }

            return underlyingType.IsEnum
                ? Enum.Parse(underlyingType, cellValue.ToString()!, true)
                : Convert.ChangeType(cellValue, underlyingType, options.CultureInfo ?? CultureInfo.CurrentCulture);
        }
        catch (Exception ex)
        {
            throw new DataMappingException($"Cannot convert value '{cellValue}' to type '{targetType.Name}'.", ex);
        }
    }

    private static object ConvertFromString(string value, Type targetType, ColumnMapping mapping, ExcelImportOptions options)
    {
        var culture = options.CultureInfo ?? CultureInfo.CurrentCulture;

        try
        {
            return targetType switch
            {
                var t when t == typeof(string) => value,
                var t when t == typeof(bool) => ConvertToBoolean(value),
                var t when t == typeof(DateTime) => ConvertToDateTime(value, mapping, culture),
                var t when t == typeof(DateOnly) => ConvertToDateOnly(value, mapping, culture),
                var t when t == typeof(TimeOnly) => TimeOnly.Parse(value, culture),
                var t when t.IsEnum => Enum.Parse(targetType, value, true),
                var t when t == typeof(Guid) => Guid.Parse(value),
                var t when IsNumericType(t) => ConvertToNumeric(value, targetType, mapping, culture),
                _ => Convert.ChangeType(value, targetType, culture),
            };
        }
        catch (Exception ex)
        {
            throw new DataMappingException($"Cannot convert string value '{value}' to type '{targetType.Name}'.", ex);
        }
    }

    private static bool ConvertToBoolean(string value)
    {
        return value.ToLowerInvariant() switch
        {
            "true" or "1" or "yes" or "y" or "on" => true,
            "false" or "0" or "no" or "n" or "off" => false,
            _ => bool.Parse(value),
        };
    }

    private static DateTime ConvertToDateTime(string value, ColumnMapping mapping, CultureInfo culture)
    {
        return !string.IsNullOrEmpty(mapping.Attribute?.DateFormat)
            ? DateTime.ParseExact(value, mapping.Attribute.DateFormat, culture)
            : DateTime.Parse(value, culture);
    }

    private static DateOnly ConvertToDateOnly(string value, ColumnMapping mapping, CultureInfo culture)
    {
        return !string.IsNullOrEmpty(mapping.Attribute?.DateFormat)
            ? DateOnly.ParseExact(value, mapping.Attribute.DateFormat, culture)
            : DateOnly.Parse(value, culture);
    }

    private static object ConvertToNumeric(string value, Type targetType, ColumnMapping mapping, CultureInfo culture)
    {
        return !string.IsNullOrEmpty(mapping.Attribute?.NumberFormat)
            ? ConvertNumericWithFormat(value, targetType, culture)
            : Convert.ChangeType(value, targetType, culture);
    }

    private static object ConvertNumericWithFormat(string value, Type targetType, CultureInfo culture)
    {
        if (targetType == typeof(decimal))
        {
            return decimal.Parse(value, NumberStyles.Any, culture);
        }

        if (targetType == typeof(double))
        {
            return double.Parse(value, NumberStyles.Any, culture);
        }

        if (targetType == typeof(float))
        {
            return float.Parse(value, NumberStyles.Any, culture);
        }

        if (targetType == typeof(int))
        {
            return int.Parse(value, NumberStyles.Integer, culture);
        }

        return targetType == typeof(long)
            ? long.Parse(value, NumberStyles.Integer, culture)
            : Convert.ChangeType(value, targetType, culture);
    }

    private static bool IsNumericType(Type type)
    {
        return type == typeof(byte) || type == typeof(sbyte) ||
               type == typeof(short) || type == typeof(ushort) ||
               type == typeof(int) || type == typeof(uint) ||
               type == typeof(long) || type == typeof(ulong) ||
               type == typeof(float) || type == typeof(double) ||
               type == typeof(decimal);
    }

    private static bool IsValueTypeOrNullable(Type type)
    {
        return type.IsValueType || Nullable.GetUnderlyingType(type) != null;
    }

    private static object? GetDefaultValue(Type type)
    {
        return type.IsValueType ? Activator.CreateInstance(type) : null;
    }

    private class ColumnMapping
    {
        public PropertyInfo Property { get; init; } = null!;

        public ExcelColumnAttribute? Attribute { get; init; }

        public string? ColumnName { get; set; }

        public int? ColumnIndex { get; set; }
    }
}